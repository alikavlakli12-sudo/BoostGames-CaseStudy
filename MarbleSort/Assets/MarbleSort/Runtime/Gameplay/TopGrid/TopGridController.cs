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

        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private MarblePool marblePool;
        [SerializeField] private MarblePalette palette;
        [SerializeField] private Camera inputCamera;
        [SerializeField, Min(0f)] private float releaseInterval = 0.035f;
        [SerializeField, Min(0f)] private float collapseDuration = 0.24f;

        private readonly Dictionary<string, TopBoxView> views =
            new Dictionary<string, TopBoxView>(StringComparer.OrdinalIgnoreCase);

        private TopGridData currentGrid;
        private TopGridState state;
        private bool inputLocked;

        public event Action<string, string, int> MarblesReleased;

        public event Action<string> BoxRemoved;

        public event Action<string, string, Vector3> BoxSelected;

        public TopGridState State => state;

        public bool InputLocked => inputLocked;

        public int GeneratedBoxCount => views.Count;

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
            currentGrid = level.topGrid;
            state = new TopGridState(currentGrid);

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
            StartCoroutine(ReleaseAndCollapse(view));
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
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return;
            }

            TopBoxView view = hit.collider.GetComponentInParent<TopBoxView>();
            if (view != null)
            {
                TrySelectBox(view.BoxId);
            }
        }

        private IEnumerator ReleaseAndCollapse(TopBoxView view)
        {
            view.BeginRelease();
            WaitForSeconds releaseWait = releaseInterval > 0f ? new WaitForSeconds(releaseInterval) : null;

            for (int index = 0; index < MarblesPerBox; index++)
            {
                Vector3 localOffset = MarbleReleasePattern.GetLocalPosition(index);
                Vector3 velocity = new Vector3(localOffset.x * 0.8f, -0.2f - ((index / 3) * 0.04f), 0f);
                marblePool.Rent(view.ColorId, view.GetReleaseWorldPosition(index), velocity);
                view.ConsumeMarker(index);

                if (releaseWait != null)
                {
                    yield return releaseWait;
                }
                else
                {
                    yield return null;
                }
            }

            if (!state.TryRemoveExposed(view.BoxId, out TopBoxRemovalResult result))
            {
                Debug.LogError($"Top box '{view.BoxId}' became invalid during release.", this);
                inputLocked = false;
                RefreshExposure();
                yield break;
            }

            MarblesReleased?.Invoke(view.BoxId, view.ColorId, MarblesPerBox);
            views.Remove(view.BoxId);
            Destroy(view.gameObject);

            yield return AnimateCollapse(result.Moves);

            BoxRemoved?.Invoke(result.RemovedBox.Id);
            inputLocked = false;
            RefreshExposure();
        }

        private IEnumerator AnimateCollapse(IReadOnlyList<TopBoxMove> moves)
        {
            if (moves.Count == 0)
            {
                yield break;
            }

            TopBoxView[] movingViews = new TopBoxView[moves.Count];
            Vector3[] starts = new Vector3[moves.Count];
            Vector3[] targets = new Vector3[moves.Count];
            for (int index = 0; index < moves.Count; index++)
            {
                TopBoxMove move = moves[index];
                movingViews[index] = views[move.BoxId];
                starts[index] = movingViews[index].transform.localPosition;
                TopBoxState box = state.GetBox(move.BoxId);
                targets[index] = GetLocalPosition(box.Column, box.CurrentRow);
            }

            if (collapseDuration <= 0f)
            {
                for (int index = 0; index < movingViews.Length; index++)
                {
                    movingViews[index].transform.localPosition = targets[index];
                }

                yield break;
            }

            float elapsed = 0f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / collapseDuration);
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                for (int index = 0; index < movingViews.Length; index++)
                {
                    movingViews[index].transform.localPosition = Vector3.LerpUnclamped(starts[index], targets[index], eased);
                }

                yield return null;
            }

            for (int index = 0; index < movingViews.Length; index++)
            {
                movingViews[index].transform.localPosition = targets[index];
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
            collapseDuration = Mathf.Max(0f, collapseDuration);
        }
    }
}
