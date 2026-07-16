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

        private static readonly Dictionary<NineCupMeshKey, Mesh> NineCupMeshes =
            new Dictionary<NineCupMeshKey, Mesh>();

        public static int CachedMeshCount => RoundedMeshes.Count + StadiumMeshes.Count + NineCupMeshes.Count;

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

        public static GameObject CreateNineCupTraySurface(
            string objectName,
            Transform parent,
            float spacing,
            float cellHalfSize,
            float outerHalfSize,
            float holeRadius,
            float innerRadius,
            float cupDepth,
            Material faceMaterial,
            Material wallMaterial,
            Material bottomMaterial,
            int segments = 16)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = GetNineCupTrayMesh(
                spacing,
                cellHalfSize,
                outerHalfSize,
                holeRadius,
                innerRadius,
                cupDepth,
                segments);

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { faceMaterial, wallMaterial, bottomMaterial };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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

        public static Mesh GetNineCupTrayMesh(
            float spacing,
            float cellHalfSize,
            float outerHalfSize,
            float holeRadius,
            float innerRadius,
            float cupDepth,
            int segments = 16)
        {
            spacing = Mathf.Max(0.05f, spacing);
            cellHalfSize = Mathf.Max(spacing * 0.5f, cellHalfSize);
            outerHalfSize = Mathf.Max((spacing + cellHalfSize) + 0.01f, outerHalfSize);
            holeRadius = Mathf.Clamp(holeRadius, 0.01f, cellHalfSize * 0.92f);
            innerRadius = Mathf.Clamp(innerRadius, 0.005f, holeRadius - 0.005f);
            cupDepth = Mathf.Max(0.001f, cupDepth);
            segments = Mathf.Clamp(segments, 8, 32);

            NineCupMeshKey key = new NineCupMeshKey(
                spacing,
                cellHalfSize,
                outerHalfSize,
                holeRadius,
                innerRadius,
                cupDepth,
                segments);
            if (NineCupMeshes.TryGetValue(key, out Mesh cached))
            {
                return cached;
            }

            Mesh mesh = BuildNineCupTrayMesh(
                spacing,
                cellHalfSize,
                outerHalfSize,
                holeRadius,
                innerRadius,
                cupDepth,
                segments);
            mesh.name = $"Nine Cup Tray {spacing:0.###}-{holeRadius:0.###}-{cupDepth:0.###}";
            mesh.hideFlags = HideFlags.DontSave;
            NineCupMeshes.Add(key, mesh);
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

        private static Mesh BuildNineCupTrayMesh(
            float spacing,
            float cellHalfSize,
            float outerHalfSize,
            float holeRadius,
            float innerRadius,
            float cupDepth,
            int segments)
        {
            List<Vector3> vertices = new List<Vector3>(720);
            List<Vector2> uvs = new List<Vector2>(720);
            List<int> faceTriangles = new List<int>(900);
            List<int> wallTriangles = new List<int>(900);
            List<int> bottomTriangles = new List<int>(450);
            float gridHalfSize = spacing + cellHalfSize;

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    Vector2 center = new Vector2(
                        (column - 1) * spacing,
                        (1 - row) * spacing);
                    AddCellFaceWithCircularOpening(
                        vertices,
                        uvs,
                        faceTriangles,
                        center,
                        cellHalfSize,
                        holeRadius,
                        outerHalfSize,
                        segments);
                    AddCupWall(
                        vertices,
                        uvs,
                        wallTriangles,
                        center,
                        holeRadius,
                        innerRadius,
                        cupDepth,
                        outerHalfSize,
                        segments);
                    AddCupBottom(
                        vertices,
                        uvs,
                        bottomTriangles,
                        center,
                        innerRadius,
                        cupDepth,
                        outerHalfSize,
                        segments);
                }
            }

            AddFrontQuad(
                vertices,
                uvs,
                faceTriangles,
                new Vector2(-outerHalfSize, gridHalfSize),
                new Vector2(outerHalfSize, gridHalfSize),
                new Vector2(outerHalfSize, outerHalfSize),
                new Vector2(-outerHalfSize, outerHalfSize),
                outerHalfSize);
            AddFrontQuad(
                vertices,
                uvs,
                faceTriangles,
                new Vector2(-outerHalfSize, -outerHalfSize),
                new Vector2(outerHalfSize, -outerHalfSize),
                new Vector2(outerHalfSize, -gridHalfSize),
                new Vector2(-outerHalfSize, -gridHalfSize),
                outerHalfSize);
            AddFrontQuad(
                vertices,
                uvs,
                faceTriangles,
                new Vector2(-outerHalfSize, -gridHalfSize),
                new Vector2(-gridHalfSize, -gridHalfSize),
                new Vector2(-gridHalfSize, gridHalfSize),
                new Vector2(-outerHalfSize, gridHalfSize),
                outerHalfSize);
            AddFrontQuad(
                vertices,
                uvs,
                faceTriangles,
                new Vector2(gridHalfSize, -gridHalfSize),
                new Vector2(outerHalfSize, -gridHalfSize),
                new Vector2(outerHalfSize, gridHalfSize),
                new Vector2(gridHalfSize, gridHalfSize),
                outerHalfSize);

            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                subMeshCount = 3
            };
            mesh.SetTriangles(faceTriangles, 0, false);
            mesh.SetTriangles(wallTriangles, 1, false);
            mesh.SetTriangles(bottomTriangles, 2, false);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCellFaceWithCircularOpening(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2 center,
            float cellHalfSize,
            float holeRadius,
            float uvHalfSize,
            int segments)
        {
            int start = vertices.Count;
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = (segment / (float)segments) * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float perimeterScale = cellHalfSize /
                                       Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
                AddTrayVertex(vertices, uvs, center + (direction * perimeterScale), 0f, uvHalfSize);
                AddTrayVertex(vertices, uvs, center + (direction * holeRadius), 0f, uvHalfSize);
            }

            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int outerCurrent = start + (segment * 2);
                int innerCurrent = outerCurrent + 1;
                int outerNext = start + (next * 2);
                int innerNext = outerNext + 1;
                AddFrontQuadTriangles(
                    triangles,
                    outerCurrent,
                    outerNext,
                    innerNext,
                    innerCurrent);
            }
        }

        private static void AddCupWall(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2 center,
            float outerRadius,
            float innerRadius,
            float depth,
            float uvHalfSize,
            int segments)
        {
            int start = vertices.Count;
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = (segment / (float)segments) * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddTrayVertex(vertices, uvs, center + (direction * outerRadius), 0f, uvHalfSize);
                AddTrayVertex(vertices, uvs, center + (direction * innerRadius), depth, uvHalfSize);
            }

            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int outerCurrent = start + (segment * 2);
                int innerCurrent = outerCurrent + 1;
                int outerNext = start + (next * 2);
                int innerNext = outerNext + 1;
                AddFrontQuadTriangles(
                    triangles,
                    outerCurrent,
                    outerNext,
                    innerNext,
                    innerCurrent);
            }
        }

        private static void AddCupBottom(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2 center,
            float radius,
            float depth,
            float uvHalfSize,
            int segments)
        {
            int centerIndex = vertices.Count;
            AddTrayVertex(vertices, uvs, center, depth, uvHalfSize);
            int outlineStart = vertices.Count;
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = (segment / (float)segments) * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddTrayVertex(vertices, uvs, center + (direction * radius), depth, uvHalfSize);
            }

            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                triangles.Add(centerIndex);
                triangles.Add(outlineStart + next);
                triangles.Add(outlineStart + segment);
            }
        }

        private static void AddFrontQuad(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2 bottomLeft,
            Vector2 bottomRight,
            Vector2 topRight,
            Vector2 topLeft,
            float uvHalfSize)
        {
            int start = vertices.Count;
            AddTrayVertex(vertices, uvs, bottomLeft, 0f, uvHalfSize);
            AddTrayVertex(vertices, uvs, bottomRight, 0f, uvHalfSize);
            AddTrayVertex(vertices, uvs, topRight, 0f, uvHalfSize);
            AddTrayVertex(vertices, uvs, topLeft, 0f, uvHalfSize);
            AddFrontQuadTriangles(triangles, start, start + 1, start + 2, start + 3);
        }

        private static void AddFrontQuadTriangles(
            List<int> triangles,
            int bottomLeft,
            int bottomRight,
            int topRight,
            int topLeft)
        {
            triangles.Add(bottomLeft);
            triangles.Add(topRight);
            triangles.Add(bottomRight);
            triangles.Add(bottomLeft);
            triangles.Add(topLeft);
            triangles.Add(topRight);
        }

        private static void AddTrayVertex(
            List<Vector3> vertices,
            List<Vector2> uvs,
            Vector2 point,
            float z,
            float halfSize)
        {
            vertices.Add(new Vector3(point.x, point.y, z));
            float size = halfSize * 2f;
            uvs.Add(new Vector2(
                (point.x + halfSize) / size,
                (point.y + halfSize) / size));
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

        private readonly struct NineCupMeshKey : IEquatable<NineCupMeshKey>
        {
            private readonly int spacing;
            private readonly int cellHalfSize;
            private readonly int outerHalfSize;
            private readonly int holeRadius;
            private readonly int innerRadius;
            private readonly int depth;
            private readonly int segments;

            public NineCupMeshKey(
                float spacing,
                float cellHalfSize,
                float outerHalfSize,
                float holeRadius,
                float innerRadius,
                float depth,
                int segments)
            {
                this.spacing = Quantize(spacing);
                this.cellHalfSize = Quantize(cellHalfSize);
                this.outerHalfSize = Quantize(outerHalfSize);
                this.holeRadius = Quantize(holeRadius);
                this.innerRadius = Quantize(innerRadius);
                this.depth = Quantize(depth);
                this.segments = segments;
            }

            public bool Equals(NineCupMeshKey other)
            {
                return spacing == other.spacing &&
                       cellHalfSize == other.cellHalfSize &&
                       outerHalfSize == other.outerHalfSize &&
                       holeRadius == other.holeRadius &&
                       innerRadius == other.innerRadius &&
                       depth == other.depth &&
                       segments == other.segments;
            }

            public override bool Equals(object obj)
            {
                return obj is NineCupMeshKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = spacing;
                    hashCode = (hashCode * 397) ^ cellHalfSize;
                    hashCode = (hashCode * 397) ^ outerHalfSize;
                    hashCode = (hashCode * 397) ^ holeRadius;
                    hashCode = (hashCode * 397) ^ innerRadius;
                    hashCode = (hashCode * 397) ^ depth;
                    return (hashCode * 397) ^ segments;
                }
            }
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * 1000f);
        }
    }
}
