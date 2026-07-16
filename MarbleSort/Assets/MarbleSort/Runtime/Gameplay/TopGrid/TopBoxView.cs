using System;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    [DisallowMultipleComponent]
    public sealed class TopBoxView : MonoBehaviour
    {
        private readonly Transform[] marbleMarkers = new Transform[MarbleReleasePattern.MarbleCount];
        private readonly Renderer[] closedRenderers = new Renderer[4];
        private Collider inputCollider;
        private GameObject trayContentRoot;
        private GameObject markerRoot;
        private Material ballMaterial;
        private bool exposed;
        private bool interactionEnabled;
        private float targetScale = 1f;
        private float currentScale = 1f;
        private float pulsePhase;

        private const float TraySpacing = 0.278f;
        private const float TrayBallHeight = 0.215f;

        public string BoxId { get; private set; } = string.Empty;

        public string ColorId { get; private set; } = string.Empty;

        public bool TrayVisible => trayContentRoot != null && trayContentRoot.activeSelf;

        public Material BallMaterial => ballMaterial;

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
            pulsePhase = Mathf.Abs(Animator.StringToHash(BoxId) % 628) * 0.01f;
            ballMaterial = PresentationMaterialLibrary.GetGlossyBall(material);

            GameObject shadow = PresentationMeshFactory.CreateRoundedBox(
                "Soft Shadow",
                transform,
                0.98f,
                0.98f,
                0.24f,
                0.2f,
                PresentationMaterialLibrary.GetSoftShadow());
            shadow.transform.localPosition = new Vector3(0.035f, -0.06f, 0.16f);
            closedRenderers[0] = shadow.GetComponent<Renderer>();

            GameObject outline = PresentationMeshFactory.CreateRoundedBox(
                "Color Outline",
                transform,
                0.96f,
                0.96f,
                0.32f,
                0.2f,
                PresentationMaterialLibrary.GetDarkened(material));
            outline.transform.localPosition = new Vector3(0f, -0.015f, 0.06f);
            closedRenderers[1] = outline.GetComponent<Renderer>();

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
            closedRenderers[2] = shell.GetComponent<Renderer>();

            GameObject highlight = PresentationMeshFactory.CreateRoundedBox(
                "Top Highlight",
                transform,
                0.62f,
                0.075f,
                0.025f,
                0.035f,
                PresentationMaterialLibrary.GetHighlight(material));
            highlight.transform.localPosition = new Vector3(-0.03f, 0.33f, -0.19f);
            closedRenderers[3] = highlight.GetComponent<Renderer>();

            trayContentRoot = new GameObject("Exposed Nine-Cup Tray");
            trayContentRoot.transform.SetParent(transform, false);

            if (!TopTrayArtworkLibrary.TryGet(ColorId, out TopTrayArtwork artwork))
            {
                throw new InvalidOperationException($"Top-tray artwork is unavailable for color '{ColorId}'.");
            }

            CreateSpriteVisual(
                "Hyper Realistic 3x3 Tray",
                trayContentRoot.transform,
                artwork.Tray,
                new Vector3(0f, 0f, -0.31f),
                0.98f,
                30);

            markerRoot = new GameObject("Nine Marble Markers");
            markerRoot.transform.SetParent(trayContentRoot.transform, false);

            for (int index = 0; index < marbleMarkers.Length; index++)
            {
                Vector3 markerPosition = MarbleReleasePattern.GetLocalPosition(index, TraySpacing, -0.36f);
                GameObject marker = CreateSpriteVisual(
                    $"Marker {index + 1:00}",
                    markerRoot.transform,
                    artwork.Ball,
                    markerPosition,
                    TrayBallHeight,
                    31,
                    fitByHeight: true);

                marbleMarkers[index] = marker.transform;
            }

            SetExposed(false);
            SetInteractionEnabled(false);
        }

        public void SetExposed(bool isExposed)
        {
            exposed = isExposed;
            trayContentRoot.SetActive(exposed);
            for (int index = 0; index < closedRenderers.Length; index++)
            {
                if (closedRenderers[index] != null)
                {
                    closedRenderers[index].enabled = !exposed;
                }
            }

            targetScale = exposed ? 1.025f : 0.965f;
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
            return transform.TransformPoint(MarbleReleasePattern.GetLocalPosition(index));
        }

        public void ConsumeMarker(int index)
        {
            marbleMarkers[index].gameObject.SetActive(false);
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

        private void RefreshCollider()
        {
            if (inputCollider != null)
            {
                inputCollider.enabled = exposed && interactionEnabled;
            }
        }

        private void Update()
        {
            float speed = targetScale > currentScale ? 12f : 8f;
            currentScale = Mathf.MoveTowards(
                currentScale,
                targetScale,
                speed * Time.deltaTime);

            float pulse = exposed && interactionEnabled
                ? (Mathf.Sin((Time.unscaledTime * 3.2f) + pulsePhase) + 1f) * 0.009f
                : 0f;
            transform.localScale = Vector3.one * (currentScale + pulse);

            if (!interactionEnabled && targetScale > 1.05f && currentScale >= targetScale - 0.001f)
            {
                targetScale = 1f;
            }
        }
    }
}
