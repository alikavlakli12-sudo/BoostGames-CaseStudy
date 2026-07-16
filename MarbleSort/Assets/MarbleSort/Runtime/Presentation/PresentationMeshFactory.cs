using System;
using System.Collections.Generic;
using MarbleSort.Gameplay.Conveyor;
using UnityEngine;

namespace MarbleSort.Presentation
{
    public static class PresentationMeshFactory
    {
        private static readonly Dictionary<RoundedMeshKey, Mesh> RoundedMeshes =
            new Dictionary<RoundedMeshKey, Mesh>();

        private static readonly Dictionary<StadiumMeshKey, Mesh> StadiumMeshes =
            new Dictionary<StadiumMeshKey, Mesh>();

        public static int CachedMeshCount => RoundedMeshes.Count + StadiumMeshes.Count;

        public static GameObject CreateRoundedBox(
            string objectName,
            Transform parent,
            float width,
            float height,
            float depth,
            float cornerRadius,
            Material material,
            bool addBoxCollider = false,
            int cornerSegments = 5)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);

            gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            PresentationMeshView meshView = gameObject.AddComponent<PresentationMeshView>();
            meshView.ConfigureRounded(width, height, depth, cornerRadius, cornerSegments);

            if (addBoxCollider)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(width, height, depth);
            }

            return gameObject;
        }

        public static GameObject CreateStadiumRibbon(
            string objectName,
            Transform parent,
            float straightLength,
            float turnRadius,
            float halfWidth,
            Material material,
            int samples = 96)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);

            gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            PresentationMeshView meshView = gameObject.AddComponent<PresentationMeshView>();
            meshView.ConfigureStadium(straightLength, turnRadius, halfWidth, samples);
            return gameObject;
        }

        public static Mesh GetRoundedBoxMesh(
            float width,
            float height,
            float depth,
            float cornerRadius,
            int cornerSegments = 5)
        {
            width = Mathf.Max(0.01f, width);
            height = Mathf.Max(0.01f, height);
            depth = Mathf.Max(0.01f, depth);
            cornerRadius = Mathf.Clamp(cornerRadius, 0.001f, Mathf.Min(width, height) * 0.5f);
            cornerSegments = Mathf.Clamp(cornerSegments, 2, 12);

            RoundedMeshKey key = new RoundedMeshKey(
                width,
                height,
                depth,
                cornerRadius,
                cornerSegments);
            if (RoundedMeshes.TryGetValue(key, out Mesh cached))
            {
                return cached;
            }

            Mesh mesh = BuildRoundedBoxMesh(width, height, depth, cornerRadius, cornerSegments);
            mesh.name = $"Rounded Box {width:0.###}x{height:0.###}x{depth:0.###}";
            mesh.hideFlags = HideFlags.DontSave;
            RoundedMeshes.Add(key, mesh);
            return mesh;
        }

        public static Mesh GetStadiumRibbonMesh(
            float straightLength,
            float turnRadius,
            float halfWidth,
            int samples = 96)
        {
            straightLength = Mathf.Max(0.01f, straightLength);
            turnRadius = Mathf.Max(0.01f, turnRadius);
            halfWidth = Mathf.Clamp(halfWidth, 0.01f, turnRadius * 0.9f);
            samples = Mathf.Clamp(samples, 24, 192);

            StadiumMeshKey key = new StadiumMeshKey(straightLength, turnRadius, halfWidth, samples);
            if (StadiumMeshes.TryGetValue(key, out Mesh cached))
            {
                return cached;
            }

            Mesh mesh = BuildStadiumRibbonMesh(straightLength, turnRadius, halfWidth, samples);
            mesh.name = $"Stadium Ribbon {straightLength:0.###}-{turnRadius:0.###}-{halfWidth:0.###}";
            mesh.hideFlags = HideFlags.DontSave;
            StadiumMeshes.Add(key, mesh);
            return mesh;
        }

        private static Mesh BuildRoundedBoxMesh(
            float width,
            float height,
            float depth,
            float radius,
            int cornerSegments)
        {
            List<Vector2> outline = BuildRoundedOutline(width, height, radius, cornerSegments);
            int outlineCount = outline.Count;
            List<Vector3> vertices = new List<Vector3>((outlineCount * 4) + 2);
            List<Vector2> uvs = new List<Vector2>((outlineCount * 4) + 2);
            List<int> triangles = new List<int>(outlineCount * 12);
            float frontZ = -depth * 0.5f;
            float backZ = depth * 0.5f;

            int frontCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, frontZ));
            uvs.Add(new Vector2(0.5f, 0.5f));
            int frontOutline = vertices.Count;
            AddOutlineVertices(vertices, uvs, outline, frontZ, width, height);

            int backCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, backZ));
            uvs.Add(new Vector2(0.5f, 0.5f));
            int backOutline = vertices.Count;
            AddOutlineVertices(vertices, uvs, outline, backZ, width, height);

            int sideFront = vertices.Count;
            AddOutlineVertices(vertices, uvs, outline, frontZ, width, height);
            int sideBack = vertices.Count;
            AddOutlineVertices(vertices, uvs, outline, backZ, width, height);

            for (int index = 0; index < outlineCount; index++)
            {
                int next = (index + 1) % outlineCount;
                triangles.Add(frontCenter);
                triangles.Add(frontOutline + next);
                triangles.Add(frontOutline + index);

                triangles.Add(backCenter);
                triangles.Add(backOutline + index);
                triangles.Add(backOutline + next);

                triangles.Add(sideFront + index);
                triangles.Add(sideFront + next);
                triangles.Add(sideBack + next);
                triangles.Add(sideFront + index);
                triangles.Add(sideBack + next);
                triangles.Add(sideBack + index);
            }

            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<Vector2> BuildRoundedOutline(
            float width,
            float height,
            float radius,
            int segments)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            Vector2[] centers =
            {
                new Vector2(halfWidth - radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, -halfHeight + radius),
                new Vector2(halfWidth - radius, -halfHeight + radius)
            };
            float[] startAngles = { 0f, 90f, 180f, 270f };
            List<Vector2> outline = new List<Vector2>(4 * (segments + 1));
            for (int corner = 0; corner < centers.Length; corner++)
            {
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = (startAngles[corner] + ((90f * segment) / segments)) * Mathf.Deg2Rad;
                    outline.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }

            return outline;
        }

        private static void AddOutlineVertices(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector2> outline,
            float z,
            float width,
            float height)
        {
            for (int index = 0; index < outline.Count; index++)
            {
                Vector2 point = outline[index];
                vertices.Add(new Vector3(point.x, point.y, z));
                uvs.Add(new Vector2((point.x / width) + 0.5f, (point.y / height) + 0.5f));
            }
        }

        private static Mesh BuildStadiumRibbonMesh(
            float straightLength,
            float turnRadius,
            float halfWidth,
            int samples)
        {
            Vector3[] vertices = new Vector3[samples * 2];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[samples * 6];

            for (int index = 0; index < samples; index++)
            {
                float normalized = index / (float)samples;
                StadiumPose pose = StadiumPath.Evaluate(normalized, straightLength, turnRadius);
                Vector3 normal = new Vector3(-pose.Tangent.y, pose.Tangent.x, 0f).normalized;
                vertices[index * 2] = pose.Position + (normal * halfWidth);
                vertices[(index * 2) + 1] = pose.Position - (normal * halfWidth);
                normals[index * 2] = Vector3.back;
                normals[(index * 2) + 1] = Vector3.back;
                uvs[index * 2] = new Vector2(normalized, 1f);
                uvs[(index * 2) + 1] = new Vector2(normalized, 0f);

                int next = (index + 1) % samples;
                int triangleIndex = index * 6;
                triangles[triangleIndex] = index * 2;
                triangles[triangleIndex + 1] = next * 2;
                triangles[triangleIndex + 2] = (next * 2) + 1;
                triangles[triangleIndex + 3] = index * 2;
                triangles[triangleIndex + 4] = (next * 2) + 1;
                triangles[triangleIndex + 5] = (index * 2) + 1;
            }

            Mesh mesh = new Mesh
            {
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private readonly struct RoundedMeshKey : IEquatable<RoundedMeshKey>
        {
            private readonly int width;
            private readonly int height;
            private readonly int depth;
            private readonly int radius;
            private readonly int segments;

            public RoundedMeshKey(float width, float height, float depth, float radius, int segments)
            {
                this.width = Quantize(width);
                this.height = Quantize(height);
                this.depth = Quantize(depth);
                this.radius = Quantize(radius);
                this.segments = segments;
            }

            public bool Equals(RoundedMeshKey other)
            {
                return width == other.width && height == other.height && depth == other.depth &&
                       radius == other.radius && segments == other.segments;
            }

            public override bool Equals(object obj)
            {
                return obj is RoundedMeshKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = width;
                    hashCode = (hashCode * 397) ^ height;
                    hashCode = (hashCode * 397) ^ depth;
                    hashCode = (hashCode * 397) ^ radius;
                    return (hashCode * 397) ^ segments;
                }
            }
        }

        private readonly struct StadiumMeshKey : IEquatable<StadiumMeshKey>
        {
            private readonly int straightLength;
            private readonly int radius;
            private readonly int width;
            private readonly int samples;

            public StadiumMeshKey(float straightLength, float radius, float width, int samples)
            {
                this.straightLength = Quantize(straightLength);
                this.radius = Quantize(radius);
                this.width = Quantize(width);
                this.samples = samples;
            }

            public bool Equals(StadiumMeshKey other)
            {
                return straightLength == other.straightLength && radius == other.radius &&
                       width == other.width && samples == other.samples;
            }

            public override bool Equals(object obj)
            {
                return obj is StadiumMeshKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = straightLength;
                    hashCode = (hashCode * 397) ^ radius;
                    hashCode = (hashCode * 397) ^ width;
                    return (hashCode * 397) ^ samples;
                }
            }
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * 1000f);
        }
    }
}
