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

        private const float TraySpacing = 0.24f;
        private const float TrayBallSize = 0.18f;

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
            trayContentRoot.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);

            GameObject trayShadow = PresentationMeshFactory.CreateRoundedBox(
                "Preview Tray Shadow",
                trayContentRoot.transform,
                0.98f,
                0.94f,
                0.05f,
                0.14f,
                PresentationMaterialLibrary.GetSoftShadow());
            trayShadow.transform.localPosition = new Vector3(0.035f, -0.085f, -0.08f);

            GameObject trayBacking = PresentationMeshFactory.CreateRoundedBox(
                "Molded Tray Lower Side",
                trayContentRoot.transform,
                0.96f,
                0.91f,
                0.16f,
                0.14f,
                PresentationMaterialLibrary.GetDarkened(material));
            trayBacking.transform.localPosition = new Vector3(0f, -0.045f, -0.13f);

            GameObject trayRim = PresentationMeshFactory.CreateRoundedBox(
                "Molded Tray Highlight Rim",
                trayContentRoot.transform,
                0.94f,
                0.89f,
                0.12f,
                0.135f,
                PresentationMaterialLibrary.GetHighlight(material));
            trayRim.transform.localPosition = new Vector3(0f, 0f, -0.24f);

            GameObject trayFace = PresentationMeshFactory.CreateRoundedBox(
                "Molded Tray Face",
                trayContentRoot.transform,
                0.86f,
                0.81f,
                0.08f,
                0.105f,
                material,
                false,
                8);
            trayFace.transform.localPosition = new Vector3(0f, 0f, -0.31f);

            markerRoot = new GameObject("Nine Marble Markers");
            markerRoot.transform.SetParent(trayContentRoot.transform, false);

            for (int index = 0; index < marbleMarkers.Length; index++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Marker {index + 1:00}";
                marker.transform.SetParent(markerRoot.transform, false);
                Vector3 markerPosition = MarbleReleasePattern.GetLocalPosition(index, TraySpacing, -0.4f);

                CreateCupLayer(
                    $"Cup Ring {index + 1:00}",
                    trayContentRoot.transform,
                    new Vector3(markerPosition.x, markerPosition.y, -0.345f),
                    0.225f,
                    0.024f,
                    PresentationMaterialLibrary.GetDarkened(material));
                CreateCupLayer(
                    $"Cup Interior {index + 1:00}",
                    trayContentRoot.transform,
                    new Vector3(markerPosition.x, markerPosition.y, -0.366f),
                    0.176f,
                    0.018f,
                    PresentationMaterialLibrary.GetCup(material));

                marker.transform.localPosition = markerPosition;
                marker.transform.localScale = Vector3.one * TrayBallSize;
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

        private static void CreateCupLayer(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            float diameter,
            float depth,
            Material material)
        {
            GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = objectName;
            cup.transform.SetParent(parent, false);
            cup.transform.localPosition = localPosition;
            cup.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cup.transform.localScale = new Vector3(diameter, depth * 0.5f, diameter);

            Renderer renderer = cup.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Collider collider = cup.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
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
