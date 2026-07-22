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
        private bool inputLocked;

        public event Action<string, string, int> MarblesReleased;

        public event Action<string, int> MarbleReleased;

        public event Action<string> BoxRemoved;

        public event Action<string, string, Vector3> BoxSelected;

        public TopGridState State => state;

        public bool InputLocked => inputLocked;

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
                view.Configure(box.Id, box.ColorId, material);
                views.Add(box.Id, view);
            }

            inputLocked = false;
            RefreshExposure();
            return true;
        }

        public bool TrySelectBox(string boxId)
        {
            if (inputLocked || state == null || marblePool == null || !state.CanSelect(boxId))
            {
                return false;
            }

            if (!views.TryGetValue(boxId, out TopBoxView view))
            {
                return false;
            }

            inputLocked = true;
            RefreshExposure();
            BoxSelected?.Invoke(view.BoxId, view.ColorId, view.transform.position);
            StartCoroutine(ReleaseAndReveal(view));
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
            if (inputLocked || inputCamera == null || !TryGetPointerDown(out Vector2 screenPosition))
            {
                return;
            }

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

        private IEnumerator ReleaseAndReveal(TopBoxView view)
        {
            view.BeginRelease();
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

            if (!state.TryRemoveExposed(view.BoxId, out TopBoxRemovalResult result))
            {
                Debug.LogError($"Top box '{view.BoxId}' became invalid during release.", this);
                inputLocked = false;
                RefreshExposure();
                yield break;
            }

            MarblesReleased?.Invoke(view.BoxId, view.ColorId, MarblesPerBox);
            traySpotLayer?.RevealSpot(result.RemovedBox.Id);
            views.Remove(view.BoxId);
            Destroy(view.gameObject);

            BoxRemoved?.Invoke(result.RemovedBox.Id);
            inputLocked = false;
            RefreshExposure();
        }

        private IEnumerator WaitForReleaseClearance(Vector3 releasePosition)
        {
            while (!marblePool.HasClearance(
                       releasePosition,
                       MarblePool.TransitMarbleDiameter))
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
                view.SetInteractionEnabled(!inputLocked);
            }
        }

        private void ClearViews()
        {
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

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
        }

        private void OnValidate()
        {
            releaseInterval = Mathf.Max(0f, releaseInterval);
            disappearDuration = Mathf.Max(0f, disappearDuration);
        }
    }
}
