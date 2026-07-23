using System;
using System.Collections;
using System.Collections.Generic;
using MarbleSort.Core;
using MarbleSort.Data;
using MarbleSort.Gameplay.Marbles;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    [DisallowMultipleComponent]
    public sealed class TopGridController : MonoBehaviour
    {
        public const int MarblesPerBox = MarbleReleasePattern.MarbleCount;
        public const int LooseBoardMarbleCapacity = 36;

        // Marker rows are indexed top-to-bottom. Empty the tray from the chute-facing
        // bottom row upward so the release reads naturally and never looks random.
        private static readonly int[] ReleaseOrder = { 6, 7, 8, 3, 4, 5, 0, 1, 2 };
        private const float ReleaseDownwardSpeed = 4.75f;

        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private MarblePool marblePool;
        [SerializeField] private MarblePalette palette;
        [SerializeField] private Camera inputCamera;
        [SerializeField, Min(0f)] private float releaseInterval = 0.01f;
        [SerializeField, Min(0f)] private float disappearDuration = 0.14f;

        private readonly Dictionary<string, TopBoxView> views =
            new Dictionary<string, TopBoxView>(StringComparer.OrdinalIgnoreCase);

        private TopGridData currentGrid;
        private TopGridState state;
        private TrayFormationBackplate formationBackplate;
        private ClearedTraySpotLayer traySpotLayer;
        private int activeReleaseCount;
        private int reservedLooseMarbleCount;

        public event Action<string, string, int> MarblesReleased;

        public event Action<string, int> MarbleReleased;

        public event Action<string> BoxRemoved;

        public event Action<string, string, Vector3> BoxSelected;

        public event Action AllTraysSelected;

        public TopGridState State => state;

        // Kept for compatibility with existing diagnostics. Tray input is no
        // longer globally locked while another tray is releasing.
        public bool InputLocked => false;

        public int ActiveReleaseCount => activeReleaseCount;

        public int ReservedLooseMarbleCount => reservedLooseMarbleCount;

        public int ProjectedLooseMarbleCount =>
            (marblePool == null ? 0 : marblePool.LooseMarbleCount) +
            reservedLooseMarbleCount;

        public bool HasCapacityForTrayRelease =>
            ProjectedLooseMarbleCount + MarblesPerBox <= LooseBoardMarbleCapacity;

        public int BoardFullRejectionCount { get; private set; }

        public int GeneratedBoxCount => views.Count;

        public TrayFormationBackplate FormationBackplate => formationBackplate;

        public ClearedTraySpotLayer TraySpotLayer => traySpotLayer;

        public void Configure(
            GameBootstrap gameBootstrap,
            MarblePool pool,
            MarblePalette marblePalette,
            Camera camera)
        {
            bootstrap = gameBootstrap;
            marblePool = pool;
            palette = marblePalette;
            inputCamera = camera;
        }

        public bool BuildLevel(LevelData level)
        {
            if (level == null || level.topGrid == null)
            {
                Debug.LogError("Cannot build a null Marble Sort top grid.", this);
                return false;
            }

            ClearViews();
            ClearFormationBackplate();
            ClearTraySpotLayer();
            currentGrid = level.topGrid;
            state = new TopGridState(currentGrid);

            GameObject formationObject = new GameObject("Static Tray Formation Surround");
            formationObject.transform.SetParent(transform, false);
            formationBackplate = formationObject.AddComponent<TrayFormationBackplate>();
            formationBackplate.Build(currentGrid);

            GameObject spotLayerObject = new GameObject("Static Cleared Tray Spots");
            spotLayerObject.transform.SetParent(transform, false);
            traySpotLayer = spotLayerObject.AddComponent<ClearedTraySpotLayer>();
            traySpotLayer.Build(currentGrid);

            for (int index = 0; index < state.Boxes.Count; index++)
            {
                TopBoxState box = state.Boxes[index];
                GameObject viewObject = new GameObject();
                viewObject.transform.SetParent(transform, false);
                viewObject.transform.localPosition = GetLocalPosition(box.Column, box.CurrentRow);

                TopBoxView view = viewObject.AddComponent<TopBoxView>();
                Material material = palette == null ? null : palette.GetMaterial(box.ColorId);
                view.Configure(box.Id, box.ColorId, material, box.IsMystery);
                views.Add(box.Id, view);
            }

            activeReleaseCount = 0;
            reservedLooseMarbleCount = 0;
            BoardFullRejectionCount = 0;
            RefreshExposure();
            return true;
        }

        public bool TrySelectBox(string boxId)
        {
            if (state == null || marblePool == null || !state.CanSelect(boxId))
            {
                return false;
            }

            if (!views.TryGetValue(boxId, out TopBoxView view))
            {
                return false;
            }

            // Reserve all nine positions before changing tray state. Concurrent
            // release coroutines therefore cannot collectively exceed the loose
            // board budget, even before their first marble has spawned.
            if (!HasCapacityForTrayRelease)
            {
                BoardFullRejectionCount++;
                view.ShowBoardFullFeedback();
                return false;
            }

            reservedLooseMarbleCount += MarblesPerBox;

            // Claim the tray immediately. Its balls and disappearance continue
            // asynchronously, but the board can expose and accept the next tray
            // on this same input beat. State removal also prevents a second tap
            // from starting another release for this tray.
            if (!state.TryRemoveExposed(boxId, out TopBoxRemovalResult removal))
            {
                reservedLooseMarbleCount = Mathf.Max(
                    0,
                    reservedLooseMarbleCount - MarblesPerBox);
                return false;
            }

            view.BeginRelease();
            activeReleaseCount++;
            BoxSelected?.Invoke(view.BoxId, view.ColorId, view.transform.position);
            RefreshExposure();
            StartCoroutine(ReleaseAndReveal(view, removal));

            if (state.ActiveCount == 0)
            {
                AllTraysSelected?.Invoke();
            }

            return true;
        }

        private void Start()
        {
            if (bootstrap == null || bootstrap.Catalog == null || bootstrap.Session == null)
            {
                Debug.LogError("Top grid requires an initialized GameBootstrap.", this);
                enabled = false;
                return;
            }

            LevelData level = bootstrap.Catalog.levels[bootstrap.Session.CurrentLevelIndex];
            if (!BuildLevel(level))
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (inputCamera == null)
            {
                return;
            }

            for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
            {
                Touch touch = Input.GetTouch(touchIndex);
                if (touch.phase == TouchPhase.Began)
                {
                    TrySelectAtScreenPosition(touch.position);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                TrySelectAtScreenPosition(Input.mousePosition);
            }
        }

        private void TrySelectAtScreenPosition(Vector2 screenPosition)
        {
            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    100f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                return;
            }

            TopBoxView view = hit.collider.GetComponentInParent<TopBoxView>();
            if (view != null)
            {
                TrySelectBox(view.BoxId);
            }
        }

        private IEnumerator ReleaseAndReveal(TopBoxView view, TopBoxRemovalResult removal)
        {
            WaitForSeconds releaseWait = releaseInterval > 0f ? new WaitForSeconds(releaseInterval) : null;

            for (int releaseIndex = 0; releaseIndex < MarblesPerBox; releaseIndex++)
            {
                int markerIndex = ReleaseOrder[releaseIndex];
                Vector3 releasePosition = view.GetReleaseWorldPosition(markerIndex);
                releasePosition.z = MarblePool.TransitDepth;
                yield return WaitForReleaseClearance(releasePosition);

                Vector3 localOffset = MarbleReleasePattern.GetLocalPosition(markerIndex);
                Vector3 velocity = new Vector3(
                    localOffset.x * 0.8f,
                    -ReleaseDownwardSpeed,
                    0f);
                marblePool.Rent(view.ColorId, releasePosition, velocity);
                reservedLooseMarbleCount = Mathf.Max(0, reservedLooseMarbleCount - 1);
                view.ConsumeMarker(markerIndex);
                MarbleReleased?.Invoke(view.BoxId, markerIndex);

                if (releaseWait != null)
                {
                    yield return releaseWait;
                }
                else
                {
                    yield return null;
                }
            }

            yield return view.AnimateDisappearance(disappearDuration);

            MarblesReleased?.Invoke(view.BoxId, view.ColorId, MarblesPerBox);
            traySpotLayer?.RevealSpot(removal.RemovedBox.Id);
            views.Remove(view.BoxId);
            Destroy(view.gameObject);

            BoxRemoved?.Invoke(removal.RemovedBox.Id);
            activeReleaseCount = Mathf.Max(0, activeReleaseCount - 1);
            RefreshExposure();
        }

        private IEnumerator WaitForReleaseClearance(Vector3 releasePosition)
        {
            while (!marblePool.HasClearance(
                       releasePosition,
                       MarblePool.TransitCollisionDiameter))
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private Vector3 GetLocalPosition(int column, int row)
        {
            float center = (currentGrid.columns - 1) * 0.5f;
            return new Vector3(
                (column - center) * currentGrid.cellSpacing,
                row * currentGrid.cellSpacing,
                0f);
        }

        private void RefreshExposure()
        {
            if (state == null)
            {
                return;
            }

            for (int index = 0; index < state.Boxes.Count; index++)
            {
                TopBoxState box = state.Boxes[index];
                if (box.IsRemoved || !views.TryGetValue(box.Id, out TopBoxView view))
                {
                    continue;
                }

                view.SetExposed(state.IsExposed(box.Id));
                view.SetInteractionEnabled(true);
            }
        }

        private void ClearViews()
        {
            StopAllCoroutines();
            activeReleaseCount = 0;
            reservedLooseMarbleCount = 0;

            foreach (TopBoxView view in views.Values)
            {
                if (view == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }
                else
                {
                    DestroyImmediate(view.gameObject);
                }
            }

            views.Clear();
        }

        private void ClearFormationBackplate()
        {
            if (formationBackplate == null)
            {
                return;
            }

            GameObject backplateObject = formationBackplate.gameObject;
            formationBackplate = null;
            if (Application.isPlaying)
            {
                backplateObject.SetActive(false);
                Destroy(backplateObject);
            }
            else
            {
                DestroyImmediate(backplateObject);
            }
        }

        private void ClearTraySpotLayer()
        {
            if (traySpotLayer == null)
            {
                return;
            }

            GameObject spotLayerObject = traySpotLayer.gameObject;
            traySpotLayer = null;
            if (Application.isPlaying)
            {
                spotLayerObject.SetActive(false);
                Destroy(spotLayerObject);
            }
            else
            {
                DestroyImmediate(spotLayerObject);
            }
        }

        private void OnValidate()
        {
            releaseInterval = Mathf.Max(0f, releaseInterval);
            disappearDuration = Mathf.Max(0f, disappearDuration);
        }
    }
}
