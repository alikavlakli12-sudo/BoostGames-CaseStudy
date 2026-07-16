using MarbleSort.Gameplay.Marbles;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    [DisallowMultipleComponent]
    public sealed class TopBoxView : MonoBehaviour
    {
        private readonly Transform[] marbleMarkers = new Transform[MarbleReleasePattern.MarbleCount];
        private Collider inputCollider;
        private GameObject trayContentRoot;
        private GameObject markerRoot;
        private Material ballMaterial;
        private bool exposed;
        private bool interactionEnabled;
        private float targetScale = 1f;
        private float currentScale = 1f;
        private float pulsePhase;

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

            GameObject outline = PresentationMeshFactory.CreateRoundedBox(
                "Color Outline",
                transform,
                0.96f,
                0.96f,
                0.32f,
                0.2f,
                PresentationMaterialLibrary.GetDarkened(material));
            outline.transform.localPosition = new Vector3(0f, -0.015f, 0.06f);

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

            GameObject highlight = PresentationMeshFactory.CreateRoundedBox(
                "Top Highlight",
                transform,
                0.62f,
                0.075f,
                0.025f,
                0.035f,
                PresentationMaterialLibrary.GetHighlight(material));
            highlight.transform.localPosition = new Vector3(-0.03f, 0.33f, -0.19f);

            trayContentRoot = new GameObject("Exposed Nine-Cup Tray");
            trayContentRoot.transform.SetParent(transform, false);

            GameObject traySurface = PresentationMeshFactory.CreateNineCupTraySurface(
                "Nine-Cup Tray Surface",
                trayContentRoot.transform,
                0.24f,
                0.12f,
                0.39f,
                0.1f,
                0.082f,
                0.022f,
                material,
                PresentationMaterialLibrary.GetDarkened(material),
                PresentationMaterialLibrary.GetCup(material),
                16);
            traySurface.transform.localPosition = new Vector3(0f, 0f, -0.195f);

            markerRoot = new GameObject("Nine Marble Markers");
            markerRoot.transform.SetParent(trayContentRoot.transform, false);

            for (int index = 0; index < marbleMarkers.Length; index++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Marker {index + 1:00}";
                marker.transform.SetParent(markerRoot.transform, false);
                marker.transform.localPosition = MarbleReleasePattern.GetLocalPosition(index);
                marker.transform.localScale = Vector3.one * 0.17f;
                Renderer markerRenderer = marker.GetComponent<Renderer>();
                markerRenderer.sharedMaterial = ballMaterial;
                markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                markerRenderer.receiveShadows = false;

                Collider markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    markerCollider.enabled = false;
                    Destroy(markerCollider);
                }

                marbleMarkers[index] = marker.transform;
            }

            SetExposed(false);
            SetInteractionEnabled(false);
        }

        public void SetExposed(bool isExposed)
        {
            exposed = isExposed;
            trayContentRoot.SetActive(exposed);
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
            return marbleMarkers[index].position;
        }

        public void ConsumeMarker(int index)
        {
            marbleMarkers[index].gameObject.SetActive(false);
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
