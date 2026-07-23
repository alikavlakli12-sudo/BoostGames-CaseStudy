using UnityEngine;

namespace MarbleSort.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ResponsiveCameraController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float referenceOrthographicSize = 9.5f;
        [SerializeField, Min(0.01f)] private float minimumHalfWidth = 5.15f;
        [SerializeField] private Transform background;
        [SerializeField, Min(0.1f)] private float backgroundTextureAspect = 2f / 3f;
        [SerializeField, Min(0f)] private float backgroundOverscan = 0.4f;

        private Camera targetCamera;
        private int previousWidth;
        private int previousHeight;

        public float CurrentAspect { get; private set; }

        public float CurrentOrthographicSize => targetCamera == null
            ? 0f
            : targetCamera.orthographicSize;

        public void Configure(
            float orthographicSize,
            float halfWidth,
            Transform backgroundTransform,
            float textureAspect)
        {
            referenceOrthographicSize = Mathf.Max(0.01f, orthographicSize);
            minimumHalfWidth = Mathf.Max(0.01f, halfWidth);
            background = backgroundTransform;
            backgroundTextureAspect = Mathf.Max(0.1f, textureAspect);
            backgroundOverscan = 0f;
            ApplyLayout(true);
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ApplyLayout(true);
        }

        private void LateUpdate()
        {
            ApplyLayout(false);
        }

        private void ApplyLayout(bool force)
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (!force && width == previousWidth && height == previousHeight)
            {
                return;
            }

            previousWidth = width;
            previousHeight = height;
            CurrentAspect = width / (float)height;
            float widthConstrainedSize = minimumHalfWidth / CurrentAspect;
            targetCamera.orthographicSize = Mathf.Max(referenceOrthographicSize, widthConstrainedSize);

            if (background != null)
            {
                float visibleHeight = (targetCamera.orthographicSize * 2f) + backgroundOverscan;
                float visibleWidth = (targetCamera.orthographicSize * 2f * CurrentAspect) + backgroundOverscan;
                float backgroundHeight = Mathf.Max(visibleHeight, visibleWidth / backgroundTextureAspect);
                float backgroundWidth = backgroundHeight * backgroundTextureAspect;
                background.localScale = new Vector3(backgroundWidth, backgroundHeight, 1f);
            }
        }

        private void OnValidate()
        {
            referenceOrthographicSize = Mathf.Max(0.01f, referenceOrthographicSize);
            minimumHalfWidth = Mathf.Max(0.01f, minimumHalfWidth);
            backgroundTextureAspect = Mathf.Max(0.1f, backgroundTextureAspect);
            backgroundOverscan = Mathf.Max(0f, backgroundOverscan);
            ApplyLayout(true);
        }
    }
}
