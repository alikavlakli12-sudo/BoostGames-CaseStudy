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
        private GameObject markerRoot;
        private bool exposed;
        private bool interactionEnabled;
        private float targetScale = 1f;

        public string BoxId { get; private set; } = string.Empty;

        public string ColorId { get; private set; } = string.Empty;

        public void Configure(string boxId, string colorId, Material material)
        {
            BoxId = boxId;
            ColorId = MarblePalette.Normalize(colorId);
            name = $"Top Box - {BoxId}";

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

            markerRoot = new GameObject("Nine Marble Markers");
            markerRoot.transform.SetParent(transform, false);

            for (int index = 0; index < marbleMarkers.Length; index++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Marker {index + 1:00}";
                marker.transform.SetParent(markerRoot.transform, false);
                marker.transform.localPosition = MarbleReleasePattern.GetLocalPosition(index);
                marker.transform.localScale = Vector3.one * 0.18f;
                marker.GetComponent<Renderer>().sharedMaterial =
                    PresentationMaterialLibrary.GetHighlight(material);

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
            markerRoot.SetActive(exposed);
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
            float speed = targetScale > transform.localScale.x ? 12f : 8f;
            float next = Mathf.MoveTowards(
                transform.localScale.x,
                targetScale,
                speed * Time.deltaTime);
            transform.localScale = Vector3.one * next;

            if (!interactionEnabled && targetScale > 1.05f && next >= targetScale - 0.001f)
            {
                targetScale = 1f;
            }
        }
    }
}
