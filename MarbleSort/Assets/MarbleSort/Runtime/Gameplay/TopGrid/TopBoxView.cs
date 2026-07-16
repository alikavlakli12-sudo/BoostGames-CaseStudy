using MarbleSort.Gameplay.Marbles;
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

        public string BoxId { get; private set; } = string.Empty;

        public string ColorId { get; private set; } = string.Empty;

        public void Configure(string boxId, string colorId, Material material)
        {
            BoxId = boxId;
            ColorId = MarblePalette.Normalize(colorId);
            name = $"Top Box - {BoxId}";

            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Box Shell";
            shell.transform.SetParent(transform, false);
            shell.transform.localScale = new Vector3(0.9f, 0.9f, 0.42f);
            shell.GetComponent<Renderer>().sharedMaterial = material;
            inputCollider = shell.GetComponent<Collider>();

            markerRoot = new GameObject("Nine Marble Markers");
            markerRoot.transform.SetParent(transform, false);

            for (int index = 0; index < marbleMarkers.Length; index++)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Marker {index + 1:00}";
                marker.transform.SetParent(markerRoot.transform, false);
                marker.transform.localPosition = MarbleReleasePattern.GetLocalPosition(index);
                marker.transform.localScale = Vector3.one * 0.18f;
                marker.GetComponent<Renderer>().sharedMaterial = material;

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
    }
}
