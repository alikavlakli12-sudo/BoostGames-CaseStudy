using UnityEngine;

namespace MarbleSort.Presentation
{
    public enum PresentationMeshType
    {
        RoundedBox,
        StadiumRibbon
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PresentationMeshView : MonoBehaviour
    {
        [SerializeField] private PresentationMeshType meshType;
        [SerializeField] private float width = 1f;
        [SerializeField] private float height = 1f;
        [SerializeField] private float depth = 0.2f;
        [SerializeField] private float radius = 0.15f;
        [SerializeField] private int detail = 5;

        public PresentationMeshType MeshType => meshType;

        public void ConfigureRounded(float newWidth, float newHeight, float newDepth, float newRadius, int segments)
        {
            meshType = PresentationMeshType.RoundedBox;
            width = newWidth;
            height = newHeight;
            depth = newDepth;
            radius = newRadius;
            detail = segments;
            ApplyMesh();
        }

        public void ConfigureStadium(float straightLength, float turnRadius, float halfWidth, int samples)
        {
            meshType = PresentationMeshType.StadiumRibbon;
            width = straightLength;
            height = turnRadius;
            radius = halfWidth;
            detail = samples;
            ApplyMesh();
        }

        private void Awake()
        {
            ApplyMesh();
        }

        private void OnValidate()
        {
            ApplyMesh();
        }

        private void ApplyMesh()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null)
            {
                return;
            }

            filter.sharedMesh = meshType == PresentationMeshType.RoundedBox
                ? PresentationMeshFactory.GetRoundedBoxMesh(width, height, depth, radius, detail)
                : PresentationMeshFactory.GetStadiumRibbonMesh(width, height, radius, detail);
        }
    }
}
