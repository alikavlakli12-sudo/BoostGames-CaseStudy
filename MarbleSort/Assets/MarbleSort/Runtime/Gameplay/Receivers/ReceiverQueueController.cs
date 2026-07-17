using System;
using System.Collections;
using System.Collections.Generic;
using MarbleSort.Core;
using MarbleSort.Data;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.Receivers
{
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    public sealed class ReceiverQueueController : MonoBehaviour
    {
        private const float ReceiverWidth = 1.7f;
        private const float ReceiverMarkerY = 0.19f;

        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private StadiumConveyorController conveyor;
        [SerializeField] private MarblePool marblePool;
        [SerializeField] private MarblePalette palette;
        [SerializeField] private Material emptyCapacityMaterial;
        [SerializeField] private float collectionY = -4f;
        [SerializeField, Min(0.01f)] private float collectionRadius = 0.18f;
        [SerializeField, Min(0f)] private float transferDuration = 0.18f;
        [SerializeField, Min(0f)] private float transferArcHeight = 0.12f;
        [SerializeField, Min(0.01f)] private float lidOpenDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float lidCloseDuration = 0.13f;
        [SerializeField, Min(0.01f)] private float queueAdvanceDuration = 0.16f;

        private readonly Dictionary<string, ReceiverBoxRuntimeView> boxViews =
            new Dictionary<string, ReceiverBoxRuntimeView>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GameObject> laneRoots = new List<GameObject>(4);
        private readonly List<MarbleActor> transferringMarbles = new List<MarbleActor>(4);

        private LevelData currentLevel;
        private ReceiverQueueState state;
        private bool[] laneTransfers = Array.Empty<bool>();
        private bool collectionEnabled = true;

        public event Action<ReceiverAcceptanceResult> MarbleCollected;

        public event Action<int, string, string> ReceiverCompleted;

        public event Action StateChanged;

        public ReceiverQueueState State => state;

        public int PendingTransferCount { get; private set; }

        public int GeneratedBoxCount => state == null ? 0 : state.RemainingBoxCount;

        public bool CollectionEnabled => collectionEnabled;

        public int GeneratedLidCount => boxViews.Count;

        public int OpenLidCount
        {
            get
            {
                int count = 0;
                foreach (ReceiverBoxRuntimeView view in boxViews.Values)
                {
                    if (view.IsLidOpen)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ClosedLidCount => Mathf.Max(0, GeneratedLidCount - OpenLidCount);

        public void Configure(
            GameBootstrap gameBootstrap,
            StadiumConveyorController conveyorController,
            MarblePool pool,
            MarblePalette marblePalette,
            Material capacityMaterial,
            float laneCollectionY,
            float laneCollectionRadius,
            float marbleTransferDuration,
            float marbleTransferArcHeight)
        {
            bootstrap = gameBootstrap;
            conveyor = conveyorController;
            marblePool = pool;
            palette = marblePalette;
            emptyCapacityMaterial = capacityMaterial;
            collectionY = laneCollectionY;
            collectionRadius = Mathf.Max(0.01f, laneCollectionRadius);
            transferDuration = Mathf.Max(0f, marbleTransferDuration);
            transferArcHeight = Mathf.Max(0f, marbleTransferArcHeight);
        }

        public bool BuildLevel(LevelData level)
        {
            if (level == null || level.receiverLanes == null)
            {
                Debug.LogError("Cannot build null Marble Sort receiver lanes.", this);
                return false;
            }

            CancelPendingTransfers();
            ClearViews();

            currentLevel = level;
            state = new ReceiverQueueState(level.receiverLanes);
            laneTransfers = new bool[state.Lanes.Count];
            ReceiverBoxRuntimeView[] activeViews = new ReceiverBoxRuntimeView[state.Lanes.Count];
            for (int laneIndex = 0; laneIndex < state.Lanes.Count; laneIndex++)
            {
                CreateLaneRoot(laneIndex);
                activeViews[laneIndex] = RebuildLaneView(laneIndex);
            }

            collectionEnabled = true;
            for (int laneIndex = 0; laneIndex < activeViews.Length; laneIndex++)
            {
                ReceiverBoxRuntimeView activeView = activeViews[laneIndex];
                if (activeView == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    laneTransfers[laneIndex] = true;
                    StartCoroutine(AnimateInitialLidOpen(laneIndex, activeView));
                }
                else
                {
                    activeView.SetLidOpenAmount(1f);
                }
            }

            StateChanged?.Invoke();
            return true;
        }

        public void SetCollectionEnabled(bool value)
        {
            collectionEnabled = value;
        }

        public bool IsLaneReadyForCollection(int laneIndex)
        {
            if (state == null || laneIndex < 0 || laneIndex >= state.Lanes.Count ||
                laneIndex >= laneTransfers.Length || laneTransfers[laneIndex])
            {
                return false;
            }

            ReceiverBoxState activeBox = state.Lanes[laneIndex].ActiveBox;
            return activeBox != null &&
                   boxViews.TryGetValue(activeBox.Id, out ReceiverBoxRuntimeView view) &&
                   view.IsLidOpen;
        }

        public Vector3 GetCollectionWorldPosition(int laneIndex)
        {
            if (currentLevel == null || laneIndex < 0 || laneIndex >= currentLevel.receiverLanes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(laneIndex));
            }

            ReceiverLaneData lane = currentLevel.receiverLanes[laneIndex];
            return transform.TransformPoint(new Vector3(lane.position.x, collectionY, -0.16f));
        }

        public bool TryCollectMatchingSlot(int laneIndex, int slotIndex)
        {
            if (!collectionEnabled || state == null || conveyor == null || marblePool == null ||
                laneIndex < 0 || laneIndex >= state.Lanes.Count || laneTransfers[laneIndex])
            {
                return false;
            }

            ConveyorSlotState slot = conveyor.State?.GetSlot(slotIndex);
            if (slot == null || slot.Status != ConveyorSlotStatus.Occupied ||
                !state.CanAccept(laneIndex, slot.ColorId))
            {
                return false;
            }

            ReceiverBoxState activeBox = state.Lanes[laneIndex].ActiveBox;
            if (activeBox == null || !boxViews.TryGetValue(activeBox.Id, out ReceiverBoxRuntimeView boxView) ||
                !boxView.IsLidOpen)
            {
                return false;
            }

            string colorId = slot.ColorId;

            if (!conveyor.TryClearSlot(slotIndex, out MarbleActor marble))
            {
                return false;
            }

            if (!marble.BeginReceiverTransfer())
            {
                marblePool.Return(marble);
                Debug.LogError($"Marble in conveyor slot {slotIndex} could not begin receiver transfer.", this);
                return false;
            }

            if (!state.TryAccept(laneIndex, colorId, out ReceiverAcceptanceResult result))
            {
                marblePool.Return(marble);
                Debug.LogError($"Receiver lane {laneIndex} rejected a previously validated marble.", this);
                return false;
            }

            laneTransfers[laneIndex] = true;
            PendingTransferCount++;
            transferringMarbles.Add(marble);
            Vector3 target = boxView.GetCapacityWorldPosition(result.FillCount - 1);
            StartCoroutine(AnimateTransfer(marble, target, boxView, result));
            return true;
        }

        private void Start()
        {
            if (bootstrap == null || bootstrap.Catalog == null || bootstrap.Session == null ||
                conveyor == null || marblePool == null)
            {
                Debug.LogError("Receiver queues require bootstrap, conveyor, and marble-pool references.", this);
                enabled = false;
                return;
            }

            LevelData level = bootstrap.Catalog.levels[bootstrap.Session.CurrentLevelIndex];
            if (!BuildLevel(level))
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (!collectionEnabled || state == null || conveyor == null)
            {
                return;
            }

            for (int laneIndex = 0; laneIndex < state.Lanes.Count; laneIndex++)
            {
                ReceiverBoxState activeBox = state.Lanes[laneIndex].ActiveBox;
                if (activeBox == null || laneTransfers[laneIndex])
                {
                    continue;
                }

                if (conveyor.TryFindClosestOccupiedSlot(
                        GetCollectionWorldPosition(laneIndex),
                        activeBox.ColorId,
                        collectionRadius,
                        out int slotIndex))
                {
                    TryCollectMatchingSlot(laneIndex, slotIndex);
                }
            }
        }

        private IEnumerator AnimateInitialLidOpen(int laneIndex, ReceiverBoxRuntimeView activeView)
        {
            float stagger = laneIndex * 0.035f;
            if (stagger > 0f)
            {
                yield return new WaitForSeconds(stagger);
            }

            if (!IsCurrentActiveView(laneIndex, activeView))
            {
                yield break;
            }

            yield return AnimateLid(activeView, true, lidOpenDuration);
            if (IsCurrentActiveView(laneIndex, activeView))
            {
                laneTransfers[laneIndex] = false;
            }
        }

        private IEnumerator AnimateTransfer(
            MarbleActor marble,
            Vector3 target,
            ReceiverBoxRuntimeView boxView,
            ReceiverAcceptanceResult result)
        {
            Vector3 start = marble.transform.position;
            float elapsed = 0f;
            while (elapsed < transferDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = transferDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / transferDuration);
                float eased = normalized * normalized * (3f - (2f * normalized));
                Vector3 position = Vector3.LerpUnclamped(start, target, eased);
                position.y += Mathf.Sin(normalized * Mathf.PI) * transferArcHeight;
                marble.SetReceiverTransferPosition(position);
                yield return null;
            }

            marble.SetReceiverTransferPosition(target);
            marblePool.Return(marble);
            transferringMarbles.Remove(marble);

            if (result.BoxCompleted)
            {
                boxView.SetFilledCount(result.FillCount);
                yield return AnimateLid(boxView, false, lidCloseDuration);
                yield return AnimateBoxExit(boxView.Root.transform);

                List<ReceiverBoxRuntimeView> promotedViews = new List<ReceiverBoxRuntimeView>();
                float spacing = currentLevel.receiverLanes[result.LaneIndex].verticalSpacing;
                ReceiverBoxRuntimeView nextActive = RebuildLaneView(
                    result.LaneIndex,
                    -spacing,
                    promotedViews);
                ReceiverCompleted?.Invoke(result.LaneIndex, result.BoxId, result.ColorId);

                if (promotedViews.Count > 0)
                {
                    yield return AnimateLaneAdvance(promotedViews, spacing, queueAdvanceDuration);
                }

                if (nextActive != null)
                {
                    yield return AnimateLid(nextActive, true, lidOpenDuration);
                }

                laneTransfers[result.LaneIndex] = false;
            }
            else
            {
                boxView.SetFilledCount(result.FillCount);
                // The receiver can accept its next marble as soon as this marble reaches
                // its capacity spot; the decorative pulse must never force a full loop.
                laneTransfers[result.LaneIndex] = false;
                yield return AnimateBoxPulse(boxView.Root.transform);
            }

            PendingTransferCount--;
            MarbleCollected?.Invoke(result);
            StateChanged?.Invoke();
        }

        private bool IsCurrentActiveView(int laneIndex, ReceiverBoxRuntimeView view)
        {
            if (view == null || state == null || laneIndex < 0 || laneIndex >= state.Lanes.Count)
            {
                return false;
            }

            ReceiverBoxState activeBox = state.Lanes[laneIndex].ActiveBox;
            return activeBox != null &&
                   boxViews.TryGetValue(activeBox.Id, out ReceiverBoxRuntimeView currentView) &&
                   ReferenceEquals(currentView, view);
        }

        private void CreateLaneRoot(int laneIndex)
        {
            ReceiverLaneData laneData = currentLevel.receiverLanes[laneIndex];
            GameObject laneRoot = new GameObject($"Receiver Lane {laneIndex + 1:00} - {laneData.id}");
            laneRoot.transform.SetParent(transform, false);
            laneRoot.transform.localPosition = laneData.position.ToVector3();
            laneRoots.Add(laneRoot);
        }

        private ReceiverBoxRuntimeView RebuildLaneView(
            int laneIndex,
            float spawnOffsetY = 0f,
            List<ReceiverBoxRuntimeView> rebuiltViews = null)
        {
            GameObject laneRoot = laneRoots[laneIndex];
            ClearLaneChildren(laneRoot.transform);

            ReceiverLaneData laneData = currentLevel.receiverLanes[laneIndex];
            ReceiverLaneState laneState = state.Lanes[laneIndex];
            ReceiverBoxRuntimeView activeView = null;
            int visibleIndex = 0;
            for (int boxIndex = laneState.ActiveBoxIndex; boxIndex < laneState.Boxes.Count; boxIndex++)
            {
                ReceiverBoxState box = laneState.Boxes[boxIndex];
                bool isActive = boxIndex == laneState.ActiveBoxIndex;
                ReceiverBoxRuntimeView view = CreateBoxView(
                    laneRoot.transform,
                    box,
                    new Vector3(0f, -(visibleIndex * laneData.verticalSpacing) + spawnOffsetY, 0f),
                    isActive,
                    visibleIndex);
                boxViews[box.Id] = view;
                rebuiltViews?.Add(view);
                if (isActive)
                {
                    activeView = view;
                }

                visibleIndex++;
            }

            return activeView;
        }

        private ReceiverBoxRuntimeView CreateBoxView(
            Transform parent,
            ReceiverBoxState box,
            Vector3 localPosition,
            bool active,
            int queueIndex)
        {
            GameObject root = new GameObject($"Receiver - {box.Id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localScale = Vector3.one;

            if (!ReceiverArtworkLibrary.TryGet(box.ColorId, out ReceiverArtwork artwork))
            {
                DestroyObject(root);
                throw new InvalidOperationException($"Receiver artwork is unavailable for color '{box.ColorId}'.");
            }

            // Each waiting box sits slightly in front of the box above it, matching the
            // physical stack shown in the reference game instead of disappearing beneath it.
            int sortingBase = 20 + (Mathf.Clamp(queueIndex, 0, 6) * 3);
            GameObject body = CreateSpriteVisual(
                "Hyper Realistic Receiver Body",
                root.transform,
                artwork.Box,
                new Vector3(0f, 0f, -0.2f),
                ReceiverWidth,
                sortingBase);
            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();

            GameObject lidLiftObject = new GameObject("Receiver Lid Lift");
            lidLiftObject.transform.SetParent(root.transform, false);
            lidLiftObject.transform.localPosition = new Vector3(0f, 0.13f, -0.27f);

            GameObject lid = CreateSpriteVisual(
                "Hyper Realistic Receiver Lid",
                lidLiftObject.transform,
                artwork.Cap,
                Vector3.zero,
                ReceiverWidth,
                sortingBase + 2);
            SpriteRenderer lidRenderer = lid.GetComponent<SpriteRenderer>();

            GameObject[] markers = Array.Empty<GameObject>();
            Transform[] markerTransforms = Array.Empty<Transform>();
            if (active)
            {
                markers = new GameObject[ReceiverBoxState.Capacity];
                markerTransforms = new Transform[ReceiverBoxState.Capacity];
                for (int index = 0; index < ReceiverBoxState.Capacity; index++)
                {
                    GameObject marker = CreateSpriteVisual(
                        $"Glossy Receiver Ball {index + 1}",
                        root.transform,
                        artwork.Ball,
                        new Vector3(-0.46f + (index * 0.46f), ReceiverMarkerY, -0.24f),
                        MarblePool.ActiveMarbleDiameter,
                        sortingBase + 1,
                        fitByHeight: true);
                    markers[index] = marker;
                    markerTransforms[index] = marker.transform;
                }
            }

            ReceiverBoxRuntimeView view = new ReceiverBoxRuntimeView(
                root,
                body,
                bodyRenderer,
                lidLiftObject.transform,
                lidRenderer,
                markers,
                markerTransforms);
            view.SetLidOpenAmount(0f);
            view.SetFilledCount(box.FillCount);
            return view;
        }

        private static GameObject CreateSpriteVisual(
            string objectName,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            float targetSize,
            int sortingOrder,
            bool fitByHeight = false)
        {
            GameObject visual = new GameObject(objectName);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;

            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;

            Vector2 spriteSize = sprite.bounds.size;
            float sourceSize = fitByHeight ? spriteSize.y : spriteSize.x;
            float scale = sourceSize <= Mathf.Epsilon ? 1f : targetSize / sourceSize;
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            return visual;
        }

        private static IEnumerator AnimateLid(
            ReceiverBoxRuntimeView view,
            bool opening,
            float duration)
        {
            if (view == null)
            {
                yield break;
            }

            float start = opening ? 0f : 1f;
            float end = opening ? 1f : 0f;
            view.SetLidOpenAmount(start);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = normalized * normalized * (3f - (2f * normalized));
                view.SetLidOpenAmount(Mathf.LerpUnclamped(start, end, eased));
                yield return null;
            }

            view.SetLidOpenAmount(end);
        }

        private static IEnumerator AnimateLaneAdvance(
            IReadOnlyList<ReceiverBoxRuntimeView> views,
            float distance,
            float duration)
        {
            if (views == null || views.Count == 0)
            {
                yield break;
            }

            Vector3[] starts = new Vector3[views.Count];
            for (int index = 0; index < views.Count; index++)
            {
                starts[index] = views[index].Root.transform.localPosition;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                for (int index = 0; index < views.Count; index++)
                {
                    Vector3 target = starts[index] + (Vector3.up * distance);
                    views[index].Root.transform.localPosition =
                        Vector3.LerpUnclamped(starts[index], target, eased);
                }

                yield return null;
            }

            for (int index = 0; index < views.Count; index++)
            {
                views[index].Root.transform.localPosition = starts[index] + (Vector3.up * distance);
            }
        }

        private static IEnumerator AnimateBoxPulse(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 originalScale = target.localScale;
            Vector3 peakScale = originalScale * 1.11f;
            const float riseDuration = 0.08f;
            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / riseDuration);
                target.localScale = Vector3.LerpUnclamped(originalScale, peakScale, normalized);
                yield return null;
            }

            Vector3 endScale = originalScale;
            const float settleDuration = 0.1f;
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / settleDuration);
                float eased = normalized * normalized * (3f - (2f * normalized));
                target.localScale = Vector3.LerpUnclamped(peakScale, endScale, eased);
                yield return null;
            }

            target.localScale = endScale;
        }

        private static IEnumerator AnimateBoxExit(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 originalScale = target.localScale;
            Vector3 originalPosition = target.localPosition;
            Vector3 peakScale = originalScale * 1.06f;
            const float anticipationDuration = 0.045f;
            float elapsed = 0f;
            while (elapsed < anticipationDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / anticipationDuration);
                target.localScale = Vector3.LerpUnclamped(originalScale, peakScale, normalized);
                yield return null;
            }

            const float exitDuration = 0.14f;
            Vector3 endScale = originalScale * 0.06f;
            Vector3 endPosition = originalPosition + new Vector3(0f, 0.12f, 0f);
            elapsed = 0f;
            while (elapsed < exitDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / exitDuration);
                float eased = normalized * normalized * normalized;
                target.localScale = Vector3.LerpUnclamped(peakScale, endScale, eased);
                target.localPosition = Vector3.LerpUnclamped(originalPosition, endPosition, eased);
                yield return null;
            }

            target.localScale = endScale;
            target.localPosition = endPosition;
        }

        private void CancelPendingTransfers()
        {
            StopAllCoroutines();
            for (int index = transferringMarbles.Count - 1; index >= 0; index--)
            {
                MarbleActor marble = transferringMarbles[index];
                if (marble != null && marble.IsRented && marblePool != null)
                {
                    marblePool.Return(marble);
                }
            }

            transferringMarbles.Clear();
            PendingTransferCount = 0;
            Array.Clear(laneTransfers, 0, laneTransfers.Length);
        }

        private void ClearViews()
        {
            boxViews.Clear();
            for (int index = laneRoots.Count - 1; index >= 0; index--)
            {
                DestroyObject(laneRoots[index]);
            }

            laneRoots.Clear();
        }

        private void ClearLaneChildren(Transform laneRoot)
        {
            for (int index = laneRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = laneRoot.GetChild(index);
                boxViews.Remove(GetBoxIdFromRoot(child.gameObject));
                DestroyObject(child.gameObject);
            }
        }

        private static string GetBoxIdFromRoot(GameObject root)
        {
            const string prefix = "Receiver - ";
            return root.name.StartsWith(prefix, StringComparison.Ordinal)
                ? root.name.Substring(prefix.Length)
                : root.name;
        }

        private static void DestroyObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void OnDestroy()
        {
            CancelPendingTransfers();
        }

        private void OnValidate()
        {
            collectionRadius = Mathf.Max(0.01f, collectionRadius);
            transferDuration = Mathf.Max(0f, transferDuration);
            transferArcHeight = Mathf.Max(0f, transferArcHeight);
            lidOpenDuration = Mathf.Max(0.01f, lidOpenDuration);
            lidCloseDuration = Mathf.Max(0.01f, lidCloseDuration);
            queueAdvanceDuration = Mathf.Max(0.01f, queueAdvanceDuration);
        }

        private sealed class ReceiverBoxRuntimeView
        {
            private const float OpenLiftDistance = 0.38f;

            private readonly SpriteRenderer bodyRenderer;
            private readonly Transform lidLift;
            private readonly Vector3 lidClosedLocalPosition;
            private readonly SpriteRenderer lidRenderer;
            private readonly GameObject[] capacityMarkers;
            private readonly Transform[] capacityTransforms;

            public ReceiverBoxRuntimeView(
                GameObject root,
                GameObject body,
                SpriteRenderer receiverBodyRenderer,
                Transform receiverLidLift,
                SpriteRenderer receiverLidRenderer,
                GameObject[] markers,
                Transform[] markerTransforms)
            {
                Root = root;
                Body = body;
                bodyRenderer = receiverBodyRenderer;
                lidLift = receiverLidLift;
                lidClosedLocalPosition = receiverLidLift == null
                    ? Vector3.zero
                    : receiverLidLift.localPosition;
                lidRenderer = receiverLidRenderer;
                capacityMarkers = markers;
                capacityTransforms = markerTransforms;
            }

            public GameObject Root { get; }

            public GameObject Body { get; }

            public bool IsLidOpen { get; private set; }

            public Vector3 GetCapacityWorldPosition(int index)
            {
                if (index < 0 || index >= capacityTransforms.Length)
                {
                    return Root.transform.position;
                }

                return capacityTransforms[index].position;
            }

            public void SetLidOpenAmount(float value)
            {
                float openAmount = Mathf.Clamp01(value);
                IsLidOpen = openAmount >= 0.999f;
                if (lidLift == null || lidRenderer == null)
                {
                    return;
                }

                float liftAmount = 1f - Mathf.Pow(1f - openAmount, 3f);
                lidLift.localPosition = lidClosedLocalPosition + (Vector3.up * (OpenLiftDistance * liftAmount));
                lidLift.localRotation = Quaternion.identity;

                Color lidColor = lidRenderer.color;
                lidColor.a = 1f - Mathf.InverseLerp(0.62f, 0.98f, openAmount);
                lidRenderer.color = lidColor;
                lidRenderer.enabled = openAmount < 0.995f;

                if (bodyRenderer != null)
                {
                    Color bodyColor = bodyRenderer.color;
                    bodyColor.a = 1f;
                    bodyRenderer.color = bodyColor;
                }
            }

            public void SetFilledCount(int fillCount)
            {
                int count = Mathf.Clamp(fillCount, 0, capacityMarkers.Length);
                for (int index = 0; index < capacityMarkers.Length; index++)
                {
                    capacityMarkers[index].SetActive(index < count);
                }
            }
        }
    }
}
