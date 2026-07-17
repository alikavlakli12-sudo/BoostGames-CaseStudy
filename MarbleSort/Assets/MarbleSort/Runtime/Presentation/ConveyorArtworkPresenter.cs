using MarbleSort.Gameplay.Conveyor;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Replaces only the legacy procedural conveyor meshes with the approved baked artwork.
    /// Slot transforms and marble actors remain live so gameplay movement is unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConveyorArtworkPresenter : MonoBehaviour
    {
        // Includes the sprite's transparent safety padding. The visible chassis remains
        // 8.5 world units wide after the conveyor root's 0.9 presentation scale.
        [SerializeField, Min(0.01f)] private float targetWidth = 10.03f;
        [SerializeField, Min(0.01f)] private float movingSocketHeight = 0.64f;
        [SerializeField, Min(0.01f)] private float centerRailWidth = 7.3f;
        [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.02f, 0.24f);
        [SerializeField] private int baseSortingOrder = 10;
        [SerializeField] private int socketSortingOrder = 11;

        private static Material liveBeltMaterial;
        private static Texture2D liveBeltTexture;
        private MeshRenderer[] legacyRenderers;
        private Transform[] movingSocketVisuals;
        private SpriteRenderer artworkRenderer;
        private SpriteRenderer centerRailRenderer;
        private MeshRenderer beltSurfaceRenderer;

        public bool IsUsingArtwork =>
            artworkRenderer != null && artworkRenderer.enabled && artworkRenderer.sprite != null;

        public SpriteRenderer ArtworkRenderer => artworkRenderer;

        public MeshRenderer BeltSurfaceRenderer => beltSurfaceRenderer;

        public SpriteRenderer CenterRailRenderer => centerRailRenderer;

        public int LegacyRendererCount => legacyRenderers?.Length ?? 0;

        public int MovingSocketCount => movingSocketVisuals?.Length ?? 0;

        public Vector3 GetMovingSocketWorldPosition(int index)
        {
            if (movingSocketVisuals == null || index < 0 || index >= movingSocketVisuals.Length ||
                movingSocketVisuals[index] == null)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index));
            }

            return movingSocketVisuals[index].position;
        }

        private void Awake()
        {
            ApplyArtwork();
        }

        private void ApplyArtwork()
        {
            legacyRenderers = GetComponentsInChildren<MeshRenderer>(true);
            StadiumConveyorController conveyor = GetComponent<StadiumConveyorController>();
            if (conveyor == null ||
                !ConveyorArtworkLibrary.TryGet(out Sprite artwork) ||
                !ConveyorArtworkLibrary.TryGetSlot(out Sprite socketArtwork) ||
                !ConveyorArtworkLibrary.TryGetRail(out Sprite railArtwork))
            {
                SetLegacyRenderersEnabled(true);
                Debug.LogError(
                    "The animated hyper-realistic conveyor could not be built; keeping the procedural fallback visible.",
                    this);
                return;
            }

            GameObject visual = new GameObject("Hyper Realistic Conveyor Artwork");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;

            artworkRenderer = visual.AddComponent<SpriteRenderer>();
            artworkRenderer.sprite = artwork;
            artworkRenderer.color = Color.white;
            artworkRenderer.sortingOrder = baseSortingOrder;

            float sourceWidth = artwork.bounds.size.x;
            float scale = sourceWidth <= Mathf.Epsilon ? 1f : targetWidth / sourceWidth;
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            SetLegacyRenderersEnabled(false);
            ConfigureLiveBeltSurface();
            ConfigureCenterRail(railArtwork);
            ConfigureMovingSockets(conveyor, socketArtwork);
        }

        private void ConfigureCenterRail(Sprite railArtwork)
        {
            GameObject rail = new GameObject("Approved Preview Center Rail");
            rail.transform.SetParent(transform, false);
            rail.transform.localPosition = new Vector3(
                0f,
                localPosition.y,
                -0.025f);

            centerRailRenderer = rail.AddComponent<SpriteRenderer>();
            centerRailRenderer.sprite = railArtwork;
            centerRailRenderer.color = Color.white;
            centerRailRenderer.sortingOrder = socketSortingOrder;

            float sourceWidth = railArtwork.bounds.size.x;
            float scale = sourceWidth <= Mathf.Epsilon ? 1f : centerRailWidth / sourceWidth;
            rail.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ConfigureLiveBeltSurface()
        {
            Transform surface = transform.Find("Track Surface");
            beltSurfaceRenderer = surface != null ? surface.GetComponent<MeshRenderer>() : null;
            if (beltSurfaceRenderer == null)
            {
                Debug.LogError("The conveyor Track Surface mesh is missing.", this);
                return;
            }

            beltSurfaceRenderer.sharedMaterial = GetLiveBeltMaterial();
            beltSurfaceRenderer.enabled = true;
        }

        private void ConfigureMovingSockets(StadiumConveyorController conveyor, Sprite socketArtwork)
        {
            int count = conveyor.ConfiguredSlotViewCount;
            movingSocketVisuals = new Transform[count];
            for (int index = 0; index < count; index++)
            {
                Transform slotRoot = conveyor.GetSlotView(index);
                if (slotRoot == null)
                {
                    continue;
                }

                GameObject visual = new GameObject($"Moving Socket Artwork {index + 1:00}");
                visual.transform.SetParent(slotRoot, false);
                visual.transform.localPosition = new Vector3(0f, 0f, -0.055f);

                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = socketArtwork;
                renderer.color = Color.white;
                renderer.sortingOrder = socketSortingOrder;

                float sourceHeight = socketArtwork.bounds.size.y;
                float scale = sourceHeight <= Mathf.Epsilon ? 1f : movingSocketHeight / sourceHeight;
                Vector3 parentScale = slotRoot.localScale;
                float inverseParentX = Mathf.Abs(parentScale.x) <= Mathf.Epsilon ? 1f : 1f / parentScale.x;
                float inverseParentY = Mathf.Abs(parentScale.y) <= Mathf.Epsilon ? 1f : 1f / parentScale.y;
                visual.transform.localScale = new Vector3(
                    scale * inverseParentX,
                    scale * inverseParentY,
                    1f);
                movingSocketVisuals[index] = visual.transform;
            }
        }

        private static Material GetLiveBeltMaterial()
        {
            if (liveBeltMaterial != null)
            {
                return liveBeltMaterial;
            }

            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            liveBeltTexture = new Texture2D(8, 64, TextureFormat.RGBA32, false)
            {
                name = "Live Conveyor Belt Gradient",
                filterMode = FilterMode.Bilinear,
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color edge = new Color32(138, 136, 159, 255);
            Color middle = new Color32(150, 148, 171, 255);
            Color highlight = new Color32(166, 165, 188, 255);
            for (int y = 0; y < liveBeltTexture.height; y++)
            {
                float normalized = y / (float)(liveBeltTexture.height - 1);
                float centerDistance = 1f - Mathf.Abs((normalized * 2f) - 1f);
                float eased = centerDistance * centerDistance * (3f - (2f * centerDistance));
                Color color = Color.Lerp(edge, middle, eased);
                color = Color.Lerp(color, highlight, Mathf.Pow(centerDistance, 5f) * 0.2f);
                for (int x = 0; x < liveBeltTexture.width; x++)
                {
                    liveBeltTexture.SetPixel(x, y, color);
                }
            }

            liveBeltTexture.Apply(false, true);
            liveBeltMaterial = new Material(shader)
            {
                name = "Live Hyper Realistic Conveyor Belt",
                mainTexture = liveBeltTexture,
                hideFlags = HideFlags.HideAndDontSave
            };
            return liveBeltMaterial;
        }

        private void SetLegacyRenderersEnabled(bool isEnabled)
        {
            if (legacyRenderers == null)
            {
                return;
            }

            for (int index = 0; index < legacyRenderers.Length; index++)
            {
                if (legacyRenderers[index] != null)
                {
                    legacyRenderers[index].enabled = isEnabled;
                }
            }
        }

        private void OnValidate()
        {
            targetWidth = Mathf.Max(0.01f, targetWidth);
            movingSocketHeight = Mathf.Max(0.01f, movingSocketHeight);
            centerRailWidth = Mathf.Max(0.01f, centerRailWidth);
        }
    }
}
