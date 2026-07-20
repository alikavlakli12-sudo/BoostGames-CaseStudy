using MarbleSort.Gameplay.Conveyor;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Displays the exact approved conveyor through one pre-rendered sprite sequence.
    /// The existing StadiumConveyorController remains the sole owner of movement,
    /// slot occupancy, marble anchors, and gameplay timing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConveyorArtworkPresenter : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float targetWidth = 7.09f;
        [SerializeField, Min(0.01f)] private float targetHeight = 1.84f;
        [SerializeField, Min(0.01f)] private float movingSocketWidth = 0.41f;
        [SerializeField, Min(0.01f)] private float movingSocketHeight = 0.49f;
        [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.02f, 0.24f);
        [SerializeField] private int sortingOrder = 13;

        private StadiumConveyorController conveyor;
        private Transform[] slotRoots;
        private Sprite[] animationFrames;
        private SpriteRenderer artworkRenderer;
        private int currentAnimationFrameIndex = -1;
        private int darkSocketCount;
        private int lightSocketCount;

        public bool IsUsingArtwork =>
            artworkRenderer != null && artworkRenderer.enabled && artworkRenderer.sprite != null;

        public SpriteRenderer ArtworkRenderer => artworkRenderer;

        public Vector2 BeltTextureOffset => conveyor != null
            ? new Vector2(
                -conveyor.Phase + (0.5f / Mathf.Max(1, conveyor.ConfiguredSlotViewCount)),
                0f)
            : Vector2.zero;

        public int AnimationFrameCount => animationFrames?.Length ?? 0;

        public int CurrentAnimationFrameIndex => currentAnimationFrameIndex;

        public int MovingSocketCount => slotRoots?.Length ?? 0;

        public int DarkSocketCount => darkSocketCount;

        public int LightSocketCount => lightSocketCount;

        public Vector3 GetMovingSocketWorldPosition(int index)
        {
            ValidateSocketIndex(index);
            return slotRoots[index].position;
        }

        public Bounds GetMovingSocketWorldBounds(int index)
        {
            ValidateSocketIndex(index);

            Vector3 tangent = slotRoots[index].right.normalized;
            Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);
            float extentX = (Mathf.Abs(tangent.x) * movingSocketWidth * 0.5f) +
                            (Mathf.Abs(normal.x) * movingSocketHeight * 0.5f);
            float extentY = (Mathf.Abs(tangent.y) * movingSocketWidth * 0.5f) +
                            (Mathf.Abs(normal.y) * movingSocketHeight * 0.5f);
            return new Bounds(
                slotRoots[index].position,
                new Vector3(extentX * 2f, extentY * 2f, 0.01f));
        }

        public float GetMovingSocketTurnAmount(int index)
        {
            ValidateSocketIndex(index);
            Vector3 tangent = slotRoots[index].localRotation * Vector3.right;
            return Mathf.Abs(tangent.y);
        }

        private void Awake()
        {
            ApplyArtwork();
        }

        private void ApplyArtwork()
        {
            RemoveObsoleteVisualComponents();
            conveyor = GetComponent<StadiumConveyorController>();
            if (conveyor == null ||
                !ConveyorArtworkLibrary.TryGetAnimation(out animationFrames))
            {
                Debug.LogError(
                    "The clean approved conveyor animation could not be loaded.",
                    this);
                return;
            }

            GameObject visual = new GameObject("Exact Approved Pre-Rendered Conveyor");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;

            artworkRenderer = visual.AddComponent<SpriteRenderer>();
            artworkRenderer.sprite = animationFrames[0];
            artworkRenderer.color = Color.white;
            artworkRenderer.sortingOrder = sortingOrder;
            ScaleLayerUniformlyToWidth(
                visual.transform,
                animationFrames[0],
                targetWidth);

            CacheMechanicalSlots();
            UpdateAnimationFrame();
        }

        private void RemoveObsoleteVisualComponents()
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int index = 0; index < meshRenderers.Length; index++)
            {
                meshRenderers[index].enabled = false;
                Destroy(meshRenderers[index]);
            }

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            for (int index = 0; index < meshFilters.Length; index++)
            {
                Destroy(meshFilters[index]);
            }
        }

        private void CacheMechanicalSlots()
        {
            int count = conveyor.ConfiguredSlotViewCount;
            slotRoots = new Transform[count];
            darkSocketCount = 0;
            lightSocketCount = 0;
            for (int index = 0; index < count; index++)
            {
                slotRoots[index] = conveyor.GetSlotView(index);
                if (IsLightSocket(index))
                {
                    lightSocketCount++;
                }
                else
                {
                    darkSocketCount++;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateAnimationFrame();
        }

        private void UpdateAnimationFrame()
        {
            if (artworkRenderer == null || animationFrames == null ||
                animationFrames.Length == 0 || conveyor == null)
            {
                return;
            }

            float normalizedFrame = Mathf.Repeat(
                conveyor.Phase,
                ConveyorArtworkLibrary.AnimationPhasePeriod) /
                ConveyorArtworkLibrary.AnimationPhasePeriod;
            int frameIndex = Mathf.FloorToInt(normalizedFrame * animationFrames.Length) %
                             animationFrames.Length;
            if (frameIndex == currentAnimationFrameIndex)
            {
                return;
            }

            currentAnimationFrameIndex = frameIndex;
            artworkRenderer.sprite = animationFrames[frameIndex];
        }

        private void ValidateSocketIndex(int index)
        {
            if (slotRoots == null || index < 0 || index >= slotRoots.Length || slotRoots[index] == null)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static void ScaleLayerUniformlyToWidth(
            Transform layer,
            Sprite artwork,
            float width)
        {
            float sourceWidth = artwork.bounds.size.x;
            float uniformScale = sourceWidth <= Mathf.Epsilon
                ? 1f
                : width / sourceWidth;
            layer.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        private static bool IsLightSocket(int index)
        {
            int sequenceIndex = Mathf.Abs(index) % 24;
            return sequenceIndex == 0 ||
                   sequenceIndex == 4 || sequenceIndex == 5 || sequenceIndex == 6 ||
                   sequenceIndex == 10 || sequenceIndex == 12 ||
                   sequenceIndex == 16 || sequenceIndex == 17 || sequenceIndex == 18 ||
                   sequenceIndex == 22;
        }

        private void OnValidate()
        {
            targetWidth = Mathf.Max(0.01f, targetWidth);
            targetHeight = Mathf.Max(0.01f, targetHeight);
            movingSocketWidth = Mathf.Max(0.01f, movingSocketWidth);
            movingSocketHeight = Mathf.Max(0.01f, movingSocketHeight);
        }
    }
}
