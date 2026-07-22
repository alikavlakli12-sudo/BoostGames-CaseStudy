using System;
using System.Collections;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    [DisallowMultipleComponent]
    public sealed class TopBoxView : MonoBehaviour
    {
        public const float ExposedRestingScale = 0.95f;
        public const float ClosedRestingScale = 0.965f;

        private readonly Transform[] marbleMarkers = new Transform[MarbleReleasePattern.MarbleCount];
        private Collider inputCollider;
        private GameObject hiddenTrayRoot;
        private GameObject hiddenShadowRoot;
        private GameObject trayContentRoot;
        private GameObject markerRoot;
        private SpriteRenderer bakedTrayRenderer;
        private Sprite[] bakedOccupancyFrames;
        private Material ballMaterial;
        private bool exposed;
        private bool interactionEnabled;
        private float targetScale = 1f;
        private float currentScale = 1f;
        private bool disappearing;

        // The top-grid root is presented at 1.18 scale. Cancel that parent scale so the
        // resting balls keep their exact intended world-space diameter.
        private const float TrayBallHeight = MarblePool.RestingMarbleDiameter / 1.18f;
        private static readonly float[] ApprovedBallColumns = { -0.264f, -0.002f, 0.260f };
        private static readonly float[] ApprovedBallRows = { 0.276f, 0.038f, -0.195f };

        public string BoxId { get; private set; } = string.Empty;

        public string ColorId { get; private set; } = string.Empty;

        public bool TrayVisible => trayContentRoot != null && trayContentRoot.activeSelf;

        public bool HiddenTrayVisible => hiddenTrayRoot != null && hiddenTrayRoot.activeSelf;

        public bool HiddenArtworkLoaded =>
            hiddenTrayRoot != null && hiddenTrayRoot.GetComponent<SpriteRenderer>()?.sprite != null;

        public string HiddenArtworkName => hiddenTrayRoot == null
            ? string.Empty
            : hiddenTrayRoot.GetComponent<SpriteRenderer>()?.sprite?.name ?? string.Empty;

        public Material BallMaterial => ballMaterial;

        public int CurrentBakedRemainingCount { get; private set; } =
            MarbleReleasePattern.MarbleCount;

        public int VisibleMarkerCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < marbleMarkers.Length; index++)
                {
                    if (marbleMarkers[index] != null && marbleMarkers[index].gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Configure(string boxId, string colorId, Material material)
        {
            BoxId = boxId;
            ColorId = MarblePalette.Normalize(colorId);
            name = $"Top Box - {BoxId}";
            ballMaterial = PresentationMaterialLibrary.GetGlossyBall(material);

            // Keep one invisible input volume for the exposed tray. The hidden
            // visual itself is the approved baked sprite, never procedural mesh art.
            GameObject shell = PresentationMeshFactory.CreateRoundedBox(
                "Box Shell",
                transform,
                0.88f,
                0.88f,
                0.34f,
                0.17f,
                material,
                true);
            inputCollider = shell.GetComponent<Collider>();
            // This volume exists only so the tray can be selected. Keeping it as
            // a trigger prevents released marbles from landing on or wedging
            // between trays while preserving pointer raycasts.
            inputCollider.isTrigger = true;
            shell.GetComponent<Renderer>().enabled = false;

            if (!HiddenTopTrayArtworkLibrary.TryGet(ColorId, out Sprite hiddenArtwork))
            {
                throw new InvalidOperationException(
                    $"Hidden top-tray artwork is unavailable for color '{ColorId}'.");
            }

            hiddenTrayRoot = CreateSpriteVisual(
                "Approved Thin Hidden Tray",
                transform,
                hiddenArtwork,
                new Vector3(0f, 0f, -0.31f),
                0.965f,
                30);
            hiddenShadowRoot = CreateLightCastShadow(
                "Hidden Tray Light Shadow",
                transform,
                hiddenArtwork,
                new Vector3(0f, 0f, -0.30f),
                0.965f,
                29);

            trayContentRoot = new GameObject("Exposed Nine-Cup Tray");
            trayContentRoot.transform.SetParent(transform, false);

            if (!TopTrayArtworkLibrary.TryGet(ColorId, out TopTrayArtwork artwork))
            {
                throw new InvalidOperationException($"Top-tray artwork is unavailable for color '{ColorId}'.");
            }

            bakedOccupancyFrames = artwork.OccupancyFrames;
            CreateLightCastShadow(
                "Exposed Tray Light Shadow",
                trayContentRoot.transform,
                artwork.GetFrame(MarbleReleasePattern.MarbleCount),
                new Vector3(0f, 0f, -0.30f),
                0.98f,
                29);
            GameObject bakedTray = CreateSpriteVisual(
                "Hyper Realistic 3x3 Tray",
                trayContentRoot.transform,
                artwork.GetFrame(MarbleReleasePattern.MarbleCount),
                new Vector3(0f, 0f, -0.31f),
                0.98f,
                30);
            bakedTrayRenderer = bakedTray.GetComponent<SpriteRenderer>();

            markerRoot = new GameObject("Nine Marble Markers");
            markerRoot.transform.SetParent(trayContentRoot.transform, false);

            for (int index = 0; index < marbleMarkers.Length; index++)
            {
                int column = index % 3;
                int row = index / 3;
                Vector3 markerPosition = new Vector3(
                    ApprovedBallColumns[column],
                    ApprovedBallRows[row],
                    -0.36f);
                GameObject marker = CreateSpriteVisual(
                    $"Marker {index + 1:00}",
                    markerRoot.transform,
                    artwork.Ball,
                    markerPosition,
                    TrayBallHeight,
                    31,
                    fitByHeight: true);

                marbleMarkers[index] = marker.transform;
                marker.GetComponent<SpriteRenderer>().enabled = false;
            }

            SetExposed(false);
            SetInteractionEnabled(false);
        }

        public void SetExposed(bool isExposed)
        {
            exposed = isExposed;
            trayContentRoot.SetActive(exposed);
            hiddenTrayRoot.SetActive(!exposed);
            hiddenShadowRoot.SetActive(!exposed);

            targetScale = exposed ? ExposedRestingScale : ClosedRestingScale;
            currentScale = targetScale;
            ApplyPresentationScale(currentScale);
            RefreshCollider();
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            interactionEnabled = isEnabled;
            RefreshCollider();
        }

        public void BeginRelease()
        {
            interactionEnabled = false;
            targetScale = 1.1f;
            RefreshCollider();
        }

        public Vector3 GetReleaseWorldPosition(int index)
        {
            if (index < 0 || index >= marbleMarkers.Length || marbleMarkers[index] == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return marbleMarkers[index].position;
        }

        public void ConsumeMarker(int index)
        {
            marbleMarkers[index].gameObject.SetActive(false);
            CurrentBakedRemainingCount = VisibleMarkerCount;
            if (bakedTrayRenderer != null && bakedOccupancyFrames != null &&
                bakedOccupancyFrames.Length > 0)
            {
                int frameIndex = Mathf.Clamp(
                    CurrentBakedRemainingCount,
                    0,
                    bakedOccupancyFrames.Length - 1);
                bakedTrayRenderer.sprite = bakedOccupancyFrames[frameIndex];
            }
        }

        public IEnumerator AnimateDisappearance(float duration)
        {
            interactionEnabled = false;
            disappearing = true;
            RefreshCollider();

            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
            Color[] startColors = new Color[sprites.Length];
            for (int index = 0; index < sprites.Length; index++)
            {
                startColors[index] = sprites[index].color;
            }

            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            Vector3 startScale = transform.localScale;
            if (duration <= Mathf.Epsilon)
            {
                gameObject.SetActive(false);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = normalized * normalized * (3f - (2f * normalized));

                float scaleMultiplier;
                if (normalized < 0.28f)
                {
                    float popProgress = normalized / 0.28f;
                    scaleMultiplier = Mathf.LerpUnclamped(1f, 1.075f, popProgress);
                }
                else
                {
                    float shrinkProgress = (normalized - 0.28f) / 0.72f;
                    scaleMultiplier = Mathf.LerpUnclamped(
                        1.075f,
                        0.58f,
                        shrinkProgress * shrinkProgress);
                }

                transform.localScale = startScale * scaleMultiplier;
                transform.localPosition = startPosition + new Vector3(0f, 0.12f * eased, 0f);
                transform.localRotation = startRotation *
                                          Quaternion.Euler(0f, 0f, -4f * eased);

                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, normalized));
                for (int index = 0; index < sprites.Length; index++)
                {
                    Color color = startColors[index];
                    color.a *= alpha;
                    sprites[index].color = color;
                }

                yield return null;
            }

            gameObject.SetActive(false);
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

        private static GameObject CreateLightCastShadow(
            string objectName,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            float targetSize,
            int sortingOrder)
        {
            Vector2 offset = PresentationMaterialLibrary.LightCastShadowOffset;
            GameObject shadow = CreateSpriteVisual(
                objectName,
                parent,
                sprite,
                localPosition + new Vector3(offset.x, offset.y, 0f),
                targetSize,
                sortingOrder);
            shadow.GetComponent<SpriteRenderer>().color =
                PresentationMaterialLibrary.LightCastShadowColor;
            return shadow;
        }

        private void RefreshCollider()
        {
            if (inputCollider != null)
            {
                inputCollider.enabled = exposed && interactionEnabled;
            }
        }

        private void Update()
        {
            if (disappearing)
            {
                return;
            }

            float speed = targetScale > currentScale ? 12f : 8f;
            currentScale = Mathf.MoveTowards(
                currentScale,
                targetScale,
                speed * Time.deltaTime);

            float presentationScale = currentScale;
            ApplyPresentationScale(presentationScale);

            if (!interactionEnabled && targetScale > 1.05f && currentScale >= targetScale - 0.001f)
            {
                targetScale = 1f;
            }
        }

        private void ApplyPresentationScale(float presentationScale)
        {
            transform.localScale = Vector3.one * presentationScale;
            if (markerRoot != null)
            {
                // Tray presentation scale never changes the resting ball size. This keeps
                // the balls matched to the receiver cups while giving adjacent trays a gap.
                markerRoot.transform.localScale =
                    Vector3.one / Mathf.Max(0.01f, presentationScale);
            }
        }
    }
}
