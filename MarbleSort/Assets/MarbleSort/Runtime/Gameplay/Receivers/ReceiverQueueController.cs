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
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private StadiumConveyorController conveyor;
        [SerializeField] private MarblePool marblePool;
        [SerializeField] private MarblePalette palette;
        [SerializeField] private Material emptyCapacityMaterial;
        [SerializeField] private float collectionY = -4f;
        [SerializeField, Min(0.01f)] private float collectionRadius = 0.18f;
        [SerializeField, Min(0f)] private float transferDuration = 0.18f;
        [SerializeField, Min(0f)] private float transferArcHeight = 0.12f;

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
            for (int laneIndex = 0; laneIndex < state.Lanes.Count; laneIndex++)
            {
                CreateLaneRoot(laneIndex);
                RebuildLaneView(laneIndex);
            }

            collectionEnabled = true;
            StateChanged?.Invoke();
            return true;
        }

        public void SetCollectionEnabled(bool value)
        {
            collectionEnabled = value;
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
            if (activeBox == null || !boxViews.TryGetValue(activeBox.Id, out ReceiverBoxRuntimeView boxView))
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
                yield return AnimateBoxPulse(boxView.Root.transform, true);
                RebuildLaneView(result.LaneIndex);
                ReceiverCompleted?.Invoke(result.LaneIndex, result.BoxId, result.ColorId);
            }
            else
            {
                boxView.SetFilledCount(
                    result.FillCount,
                    PresentationMaterialLibrary.GetGlossyBall(palette?.GetMaterial(result.ColorId)));
                yield return AnimateBoxPulse(boxView.Root.transform, false);
            }

            PendingTransferCount--;
            laneTransfers[result.LaneIndex] = false;
            MarbleCollected?.Invoke(result);
            StateChanged?.Invoke();
        }

        private void CreateLaneRoot(int laneIndex)
        {
            ReceiverLaneData laneData = currentLevel.receiverLanes[laneIndex];
            GameObject laneRoot = new GameObject($"Receiver Lane {laneIndex + 1:00} - {laneData.id}");
            laneRoot.transform.SetParent(transform, false);
            laneRoot.transform.localPosition = laneData.position.ToVector3();
            laneRoots.Add(laneRoot);
        }

        private void RebuildLaneView(int laneIndex)
        {
            GameObject laneRoot = laneRoots[laneIndex];
            ClearLaneChildren(laneRoot.transform);

            ReceiverLaneData laneData = currentLevel.receiverLanes[laneIndex];
            ReceiverLaneState laneState = state.Lanes[laneIndex];
            int visibleIndex = 0;
            for (int boxIndex = laneState.ActiveBoxIndex; boxIndex < laneState.Boxes.Count; boxIndex++)
            {
                ReceiverBoxState box = laneState.Boxes[boxIndex];
                bool isActive = boxIndex == laneState.ActiveBoxIndex;
                ReceiverBoxRuntimeView view = CreateBoxView(
                    laneRoot.transform,
                    box,
                    new Vector3(0f, -(visibleIndex * laneData.verticalSpacing), 0f),
                    isActive);
                boxViews[box.Id] = view;
                visibleIndex++;
            }
        }

        private ReceiverBoxRuntimeView CreateBoxView(
            Transform parent,
            ReceiverBoxState box,
            Vector3 localPosition,
            bool active)
        {
            GameObject root = new GameObject($"Receiver - {box.Id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localScale = Vector3.one * (active ? 1f : 0.94f);

            GameObject shadow = PresentationMeshFactory.CreateRoundedBox(
                "Soft Shadow",
                root.transform,
                1.54f,
                0.64f,
                0.2f,
                0.16f,
                PresentationMaterialLibrary.GetSoftShadow());
            shadow.transform.localPosition = new Vector3(0.04f, -0.045f, 0.15f);

            Material colorMaterial = palette?.GetMaterial(box.ColorId);
            GameObject outline = PresentationMeshFactory.CreateRoundedBox(
                "Color Outline",
                root.transform,
                1.5f,
                0.62f,
                0.28f,
                0.16f,
                PresentationMaterialLibrary.GetDarkened(colorMaterial));
            outline.transform.localPosition = new Vector3(0f, -0.015f, 0.06f);

            GameObject body = PresentationMeshFactory.CreateRoundedBox(
                "Box",
                root.transform,
                1.42f,
                0.54f,
                0.3f,
                0.14f,
                colorMaterial);

            GameObject highlight = PresentationMeshFactory.CreateRoundedBox(
                "Top Highlight",
                root.transform,
                1.08f,
                0.055f,
                0.02f,
                0.025f,
                PresentationMaterialLibrary.GetHighlight(colorMaterial));
            highlight.transform.localPosition = new Vector3(-0.04f, 0.19f, -0.17f);

            Renderer[] markers = Array.Empty<Renderer>();
            Transform[] markerTransforms = Array.Empty<Transform>();
            if (active)
            {
                markers = new Renderer[ReceiverBoxState.Capacity];
                markerTransforms = new Transform[ReceiverBoxState.Capacity];
                for (int index = 0; index < ReceiverBoxState.Capacity; index++)
                {
                    GameObject marker = CreatePrimitive(
                        $"Capacity {index + 1}",
                        PrimitiveType.Sphere,
                        root.transform,
                        new Vector3(-0.38f + (index * 0.38f), -0.015f, -0.2f),
                        new Vector3(0.16f, 0.16f, 0.075f),
                        emptyCapacityMaterial);
                    markers[index] = marker.GetComponent<Renderer>();
                    markerTransforms[index] = marker.transform;
                }
            }

            ReceiverBoxRuntimeView view = new ReceiverBoxRuntimeView(root, body, markers, markerTransforms);
            view.SetFilledCount(
                box.FillCount,
                PresentationMaterialLibrary.GetGlossyBall(palette?.GetMaterial(box.ColorId)));
            return view;
        }

        private static IEnumerator AnimateBoxPulse(Transform target, bool completing)
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

            Vector3 endScale = completing ? originalScale * 0.15f : originalScale;
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

        private static GameObject CreatePrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return gameObject;
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
        }

        private sealed class ReceiverBoxRuntimeView
        {
            private readonly Renderer[] capacityMarkers;
            private readonly Transform[] capacityTransforms;

            public ReceiverBoxRuntimeView(
                GameObject root,
                GameObject body,
                Renderer[] markers,
                Transform[] markerTransforms)
            {
                Root = root;
                Body = body;
                capacityMarkers = markers;
                capacityTransforms = markerTransforms;
            }

            public GameObject Root { get; }

            public GameObject Body { get; }

            public Vector3 GetCapacityWorldPosition(int index)
            {
                if (index < 0 || index >= capacityTransforms.Length)
                {
                    return Root.transform.position;
                }

                return capacityTransforms[index].position;
            }

            public void SetFilledCount(int fillCount, Material filledMaterial)
            {
                int count = Mathf.Min(fillCount, capacityMarkers.Length);
                for (int index = 0; index < count; index++)
                {
                    capacityMarkers[index].sharedMaterial = filledMaterial;
                }
            }
        }
    }
}
