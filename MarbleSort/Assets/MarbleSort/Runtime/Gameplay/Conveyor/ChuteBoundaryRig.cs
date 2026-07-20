using System.Collections.Generic;
using MarbleSort.Gameplay.Marbles;
using UnityEngine;

namespace MarbleSort.Gameplay.Conveyor
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ChuteBoundaryRig : MonoBehaviour
    {
        public const float SolidDepth = 1.2f;
        public const float FunnelThickness = 0.28f;
        public const float AdmissionGateThickness = 0.8f;
        public const float AdmissionGateWidth = 1.08f;
        public const int ExpectedArtMatchedBoundarySegmentCount = 28;
        public const int ExpectedSolidColliderCount = 29;

        private const float ArtworkWidth = 853f;
        private const float ArtworkHeight = 1844f;
        private const float ReferenceArtworkWorldWidth = 9.3f;
        private const float ReferenceArtworkWorldHeight = 20.12f;
        private const float ContactTolerance = 0.003f;

        // Measured on PortraitEnvironmentSingleBoard.png. These pixels follow the
        // artwork's actual inner contact edge from the upper-left wall, through its
        // rounded lower corner, and along the funnel slope to the chute opening.
        private static readonly Vector2[] LeftArtworkContactPixels =
        {
            new Vector2(68f, 450f),
            new Vector2(68f, 964f),
            new Vector2(75f, 980f),
            new Vector2(90f, 994f),
            new Vector2(110f, 1003f),
            new Vector2(150f, 1012f),
            new Vector2(200f, 1023f),
            new Vector2(250f, 1035f),
            new Vector2(300f, 1046f),
            new Vector2(330f, 1053f),
            new Vector2(350f, 1058f),
            new Vector2(365f, 1064f),
            new Vector2(375f, 1080f),
            new Vector2(377f, 1100f),
            new Vector2(377f, 1235f)
        };

        private PhysicsMaterial boundaryMaterial;
        private readonly List<BoxCollider> solidBoundaries =
            new List<BoxCollider>(ExpectedSolidColliderCount);
        private readonly List<BoxCollider> artworkBoundarySegments =
            new List<BoxCollider>(ExpectedArtMatchedBoundarySegmentCount);
        private readonly List<BoxCollider> leftSurfaceColliders = new List<BoxCollider>(14);
        private readonly List<BoxCollider> rightSurfaceColliders = new List<BoxCollider>(14);
        private readonly List<ArtworkContactSegment> measuredSegments =
            new List<ArtworkContactSegment>(ExpectedArtMatchedBoundarySegmentCount);

        private BoxCollider leftWallCollider;
        private BoxCollider rightWallCollider;
        private BoxCollider admissionGateCollider;
        private Transform generatedContourRoot;
        private MarbleActor[] pooledMarbles;

        public int SolidBoundaryColliderCount { get; private set; }

        public int ArtMatchedBoundarySegmentCount => artworkBoundarySegments.Count;

        public IReadOnlyList<BoxCollider> SolidBoundaries => solidBoundaries;

        public int RenderedBoundaryCount
        {
            get
            {
                int rendered = 0;
                for (int index = 0; index < artworkBoundarySegments.Count; index++)
                {
                    Renderer renderer = artworkBoundarySegments[index] == null
                        ? null
                        : artworkBoundarySegments[index].GetComponent<Renderer>();
                    if (renderer != null && renderer.enabled)
                    {
                        rendered++;
                    }
                }

                return rendered;
            }
        }

        public bool BoundaryContourMatchesArtwork
        {
            get
            {
                if (artworkBoundarySegments.Count != ExpectedArtMatchedBoundarySegmentCount ||
                    measuredSegments.Count != artworkBoundarySegments.Count)
                {
                    return false;
                }

                for (int index = 0; index < artworkBoundarySegments.Count; index++)
                {
                    BoxCollider collider = artworkBoundarySegments[index];
                    ArtworkContactSegment measured = measuredSegments[index];
                    if (collider == null || !collider.enabled || collider.isTrigger)
                    {
                        return false;
                    }

                    Vector3 firstWorld = collider.transform.TransformPoint(
                        collider.center + new Vector3(-0.5f, 0.5f, 0f));
                    Vector3 secondWorld = collider.transform.TransformPoint(
                        collider.center + new Vector3(0.5f, 0.5f, 0f));
                    Vector2 first = transform.InverseTransformPoint(firstWorld);
                    Vector2 second = transform.InverseTransformPoint(secondWorld);
                    if (Vector2.Distance(first, measured.Start) > ContactTolerance ||
                        Vector2.Distance(second, measured.End) > ContactTolerance)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Awake()
        {
            StrengthenCollisionRig();
        }

        private void Start()
        {
            MarblePool pool = FindFirstObjectByType<MarblePool>();
            pooledMarbles = pool == null
                ? null
                : pool.GetComponentsInChildren<MarbleActor>(true);
        }

        private void StrengthenCollisionRig()
        {
            boundaryMaterial = new PhysicsMaterial("Artwork-Matched Chute Boundary")
            {
                dynamicFriction = 0.02f,
                staticFriction = 0.02f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            SolidBoundaryColliderCount = 0;
            solidBoundaries.Clear();
            artworkBoundarySegments.Clear();
            leftSurfaceColliders.Clear();
            rightSurfaceColliders.Clear();
            measuredSegments.Clear();

            if (generatedContourRoot != null)
            {
                Destroy(generatedContourRoot.gameObject);
            }

            GameObject contourRoot = new GameObject("Artwork-Matched Invisible Collision");
            generatedContourRoot = contourRoot.transform;
            generatedContourRoot.SetParent(transform, false);

            Vector2[] leftContactPoints = BuildLeftArtworkContactPoints();
            Vector2[] rightContactPoints = MirrorContactPoints(leftContactPoints);

            Transform leftWall = FindDescendant(transform, "Left Wall");
            Transform rightWall = FindDescendant(transform, "Right Wall");
            Transform leftFunnel = FindDescendant(transform, "Left Funnel");
            Transform rightFunnel = FindDescendant(transform, "Right Funnel");
            Transform board = transform.parent;
            Transform leftEntranceGuide = FindDescendant(board, "Left Entrance Guide");
            Transform rightEntranceGuide = FindDescendant(board, "Right Entrance Guide");
            int finalSegmentIndex = leftContactPoints.Length - 2;

            for (int index = 0; index < leftContactPoints.Length - 1; index++)
            {
                Transform segment = index == 0
                    ? leftWall
                    : index == 1
                        ? leftFunnel
                        : index == finalSegmentIndex
                            ? leftEntranceGuide
                            : CreateContourSegment(
                                $"Left Backboard Collision Segment {index + 1:00}");
                if (segment == leftEntranceGuide && segment != null)
                {
                    segment.SetParent(transform, false);
                }

                BoxCollider collider = ConfigureArtworkSegment(
                    segment,
                    leftContactPoints[index],
                    leftContactPoints[index + 1]);
                if (index == 0)
                {
                    leftWallCollider = collider;
                }
                else if (collider != null)
                {
                    leftSurfaceColliders.Add(collider);
                }
            }

            for (int index = 0; index < rightContactPoints.Length - 1; index++)
            {
                Transform segment = index == 0
                    ? rightWall
                    : index == 1
                        ? rightFunnel
                        : index == finalSegmentIndex
                            ? rightEntranceGuide
                            : CreateContourSegment(
                                $"Right Backboard Collision Segment {index + 1:00}");
                if (segment == rightEntranceGuide && segment != null)
                {
                    segment.SetParent(transform, false);
                }

                // Reverse each right-side segment so local +Y always points into the
                // basin. The geometry is mirrored while the contact solver can use
                // the same inward-facing convention on both sides.
                BoxCollider collider = ConfigureArtworkSegment(
                    segment,
                    rightContactPoints[index + 1],
                    rightContactPoints[index]);
                if (index == 0)
                {
                    rightWallCollider = collider;
                }
                else if (collider != null)
                {
                    rightSurfaceColliders.Add(collider);
                }
            }

            Transform admissionGate = FindDescendant(board, "Admission Gate");
            ConfigureSolid(
                admissionGate,
                new Vector3(AdmissionGateWidth, AdmissionGateThickness, SolidDepth),
                // Preserve the approved -0.26 top surface while extending the hidden
                // catch downward so a fast marble cannot tunnel through the gate.
                new Vector3(0f, -0.66f, 0f));
            admissionGateCollider = admissionGate == null
                ? null
                : admissionGate.GetComponent<BoxCollider>();
        }

        private Vector2[] BuildLeftArtworkContactPoints()
        {
            Vector2[] points = new Vector2[LeftArtworkContactPixels.Length];
            for (int index = 0; index < LeftArtworkContactPixels.Length; index++)
            {
                Vector2 pixel = LeftArtworkContactPixels[index];
                points[index] = new Vector2(
                    ((pixel.x / ArtworkWidth) - 0.5f) * ReferenceArtworkWorldWidth,
                    (0.5f - (pixel.y / ArtworkHeight)) * ReferenceArtworkWorldHeight);
            }

            return points;
        }

        private static Vector2[] MirrorContactPoints(Vector2[] source)
        {
            Vector2[] mirrored = new Vector2[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                mirrored[index] = new Vector2(-source[index].x, source[index].y);
            }

            return mirrored;
        }

        private Transform CreateContourSegment(string objectName)
        {
            GameObject segment = new GameObject(objectName);
            segment.transform.SetParent(generatedContourRoot, false);
            return segment.transform;
        }

        private BoxCollider ConfigureArtworkSegment(
            Transform boundary,
            Vector2 contactStart,
            Vector2 contactEnd)
        {
            if (boundary == null)
            {
                Debug.LogError("The artwork-matched collision contour is missing a segment.", this);
                return null;
            }

            Vector2 delta = contactEnd - contactStart;
            float length = delta.magnitude;
            if (length <= 0.0001f)
            {
                Debug.LogError("An artwork collision segment has zero length.", this);
                return null;
            }

            Vector2 tangent = delta / length;
            Vector2 inwardNormal = new Vector2(-tangent.y, tangent.x);
            Vector2 midpoint = (contactStart + contactEnd) * 0.5f;
            Vector2 center = midpoint - (inwardNormal * (FunnelThickness * 0.5f));

            boundary.localPosition = new Vector3(center.x, center.y, 0f);
            boundary.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            boundary.localScale = new Vector3(length, FunnelThickness, SolidDepth);

            BoxCollider collider = PrepareCollider(boundary);
            if (collider == null)
            {
                return null;
            }

            Renderer renderer = boundary.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            artworkBoundarySegments.Add(collider);
            measuredSegments.Add(new ArtworkContactSegment(contactStart, contactEnd));
            solidBoundaries.Add(collider);
            SolidBoundaryColliderCount++;
            return collider;
        }

        private void ConfigureSolid(
            Transform boundary,
            Vector3 localScale,
            Vector3? localPosition = null)
        {
            if (boundary == null)
            {
                Debug.LogError("The solid chute collision rig is missing a required boundary.", this);
                return;
            }

            boundary.localScale = localScale;
            boundary.localRotation = Quaternion.identity;
            if (localPosition.HasValue)
            {
                boundary.localPosition = localPosition.Value;
            }

            BoxCollider collider = PrepareCollider(boundary);
            if (collider == null)
            {
                return;
            }

            solidBoundaries.Add(collider);
            SolidBoundaryColliderCount++;
        }

        private BoxCollider PrepareCollider(Transform boundary)
        {
            BoxCollider collider = boundary.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = boundary.gameObject.AddComponent<BoxCollider>();
            }

            collider.enabled = true;
            collider.isTrigger = false;
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            collider.contactOffset = 0.012f;
            collider.sharedMaterial = boundaryMaterial;
            return collider;
        }

        private void LateUpdate()
        {
            if (pooledMarbles == null)
            {
                return;
            }

            for (int marbleIndex = 0; marbleIndex < pooledMarbles.Length; marbleIndex++)
            {
                MarbleActor marble = pooledMarbles[marbleIndex];
                if (marble == null || !marble.IsRented ||
                    marble.MotionMode != MarbleMotionMode.LoosePhysics)
                {
                    continue;
                }

                // A shallow molded slope should keep rolling under gravity. Unity
                // may otherwise put a slow sphere to sleep while it is touching two
                // neighboring contour segments at their shared point.
                marble.Body?.WakeUp();
                ResolveBasinSideContact(marble);
                ResolveSurfaceContacts(marble, leftSurfaceColliders, true);
                ResolveSurfaceContacts(marble, rightSurfaceColliders, false);
                ResolveAdmissionFloorContact(marble);
                ResolveBoundaryPenetration(marble);
            }
        }

        private static void ResolveSurfaceContacts(
            MarbleActor marble,
            List<BoxCollider> surfaces,
            bool leftSide)
        {
            for (int index = 0; index < surfaces.Count; index++)
            {
                ResolveFunnelSurfaceContact(marble, surfaces[index]);
            }

            ApplyNaturalDownhillAcceleration(marble, surfaces, leftSide);
        }

        private static void ApplyNaturalDownhillAcceleration(
            MarbleActor marble,
            List<BoxCollider> surfaces,
            bool leftSide)
        {
            SphereCollider sphere = marble.GetComponent<SphereCollider>();
            Rigidbody body = marble.Body;
            if (sphere == null || body == null)
            {
                return;
            }

            float sphereRadius = sphere.radius *
                                 Mathf.Max(
                                     Mathf.Abs(marble.transform.lossyScale.x),
                                     Mathf.Abs(marble.transform.lossyScale.y));
            BoxCollider closestSurface = null;
            float closestContactError = float.MaxValue;
            for (int index = 0; index < surfaces.Count; index++)
            {
                BoxCollider surface = surfaces[index];
                Vector3 offset = body.position - surface.transform.position;
                float halfLength = Mathf.Abs(surface.transform.lossyScale.x) *
                                   surface.size.x * 0.5f;
                float along = Vector3.Dot(offset, surface.transform.right.normalized);
                if (Mathf.Abs(along) > halfLength + 0.025f)
                {
                    continue;
                }

                float halfThickness = Mathf.Abs(surface.transform.lossyScale.y) *
                                      surface.size.y * 0.5f;
                float distanceFromContactLine =
                    Vector3.Dot(offset, surface.transform.up.normalized) - halfThickness;
                float contactError = Mathf.Abs(distanceFromContactLine - sphereRadius);
                if (contactError <= 0.035f && contactError < closestContactError)
                {
                    closestContactError = contactError;
                    closestSurface = surface;
                }
            }

            if (closestSurface == null)
            {
                return;
            }

            // This is tangent to the measured surface, never toward the chute or
            // through a boundary. It merely prevents a sphere from numerically
            // balancing on the seam between two shallow static collider segments.
            Vector3 downhill = leftSide
                ? closestSurface.transform.right.normalized
                : -closestSurface.transform.right.normalized;
            body.AddForce(downhill * 3.2f, ForceMode.Acceleration);
            body.WakeUp();
        }

        private static void ResolveFunnelSurfaceContact(
            MarbleActor marble,
            BoxCollider funnel)
        {
            SphereCollider sphere = marble.GetComponent<SphereCollider>();
            Rigidbody body = marble.Body;
            if (funnel == null || sphere == null || body == null)
            {
                return;
            }

            float sphereRadius = sphere.radius *
                                 Mathf.Max(
                                     Mathf.Abs(marble.transform.lossyScale.x),
                                     Mathf.Abs(marble.transform.lossyScale.y));
            Vector3 axis = funnel.transform.right.normalized;
            Vector3 inwardNormal = funnel.transform.up.normalized;
            Vector3 offset = body.position - funnel.transform.position;
            float alongSurface = Vector3.Dot(offset, axis);
            float halfLength = Mathf.Abs(funnel.transform.lossyScale.x) *
                               funnel.size.x * 0.5f;
            float halfThickness = Mathf.Abs(funnel.transform.lossyScale.y) *
                                  funnel.size.y * 0.5f;
            Vector3 correctionDirection;
            float correctionDistance;
            if (Mathf.Abs(alongSurface) > halfLength + 0.001f)
            {
                // Adjacent segments own their shared endpoint. Treating every joint
                // as a rounded cap creates a row of invisible bumps that can hold a
                // slowly rolling marble on the otherwise continuous artwork slope.
                return;
            }

            float surfaceDistance = Vector3.Dot(offset, inwardNormal) - halfThickness;
            float requiredDistance = sphereRadius + 0.002f;
            if (surfaceDistance >= requiredDistance)
            {
                return;
            }

            correctionDirection = inwardNormal;
            correctionDistance = requiredDistance - surfaceDistance;

            Vector3 correction = correctionDirection * correctionDistance;
            Vector3 position = body.position + correction;
            position.z = MarblePool.TransitDepth;
            body.position = position;
            marble.transform.position = position;

            Vector3 velocity = body.linearVelocity;
            float inwardSpeed = Vector3.Dot(velocity, correctionDirection);
            if (inwardSpeed < 0f)
            {
                body.linearVelocity = velocity - (correctionDirection * inwardSpeed);
            }
        }

        private void ResolveBasinSideContact(MarbleActor marble)
        {
            SphereCollider sphere = marble.GetComponent<SphereCollider>();
            Rigidbody body = marble.Body;
            if (leftWallCollider == null || rightWallCollider == null ||
                sphere == null || body == null)
            {
                return;
            }

            float sphereRadius = sphere.radius *
                                 Mathf.Max(
                                     Mathf.Abs(marble.transform.lossyScale.x),
                                     Mathf.Abs(marble.transform.lossyScale.y));
            Bounds leftBounds = leftWallCollider.bounds;
            Bounds rightBounds = rightWallCollider.bounds;
            Vector3 position = body.position;
            bool withinWallHeight =
                position.y + sphereRadius >= leftBounds.min.y &&
                position.y - sphereRadius <= leftBounds.max.y;
            if (!withinWallHeight)
            {
                return;
            }

            float leftInsideX = leftBounds.max.x + sphereRadius + 0.002f;
            float rightInsideX = rightBounds.min.x - sphereRadius - 0.002f;
            float correctionDirection = 0f;
            if (position.x < leftInsideX)
            {
                position.x = leftInsideX;
                correctionDirection = 1f;
            }
            else if (position.x > rightInsideX)
            {
                position.x = rightInsideX;
                correctionDirection = -1f;
            }

            if (Mathf.Approximately(correctionDirection, 0f))
            {
                return;
            }

            position.z = MarblePool.TransitDepth;
            body.position = position;
            marble.transform.position = position;

            Vector3 velocity = body.linearVelocity;
            if (velocity.x * correctionDirection < 0f)
            {
                velocity.x = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void ResolveAdmissionFloorContact(MarbleActor marble)
        {
            SphereCollider sphere = marble.GetComponent<SphereCollider>();
            Rigidbody body = marble.Body;
            if (admissionGateCollider == null || sphere == null || body == null)
            {
                return;
            }

            float sphereRadius = sphere.radius *
                                 Mathf.Max(
                                     Mathf.Abs(marble.transform.lossyScale.x),
                                     Mathf.Abs(marble.transform.lossyScale.y));
            Bounds gateBounds = admissionGateCollider.bounds;
            Vector3 position = body.position;
            bool overlapsGateWidth =
                position.x + sphereRadius >= gateBounds.min.x &&
                position.x - sphereRadius <= gateBounds.max.x;
            float restingCenterY = gateBounds.max.y + sphereRadius + 0.002f;
            if (!overlapsGateWidth || position.y >= restingCenterY)
            {
                return;
            }

            position.y = restingCenterY;
            position.z = MarblePool.TransitDepth;
            body.position = position;
            marble.transform.position = position;

            Vector3 velocity = body.linearVelocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void ResolveBoundaryPenetration(MarbleActor marble)
        {
            SphereCollider sphere = marble.GetComponent<SphereCollider>();
            Rigidbody body = marble.Body;
            if (sphere == null || body == null)
            {
                return;
            }

            const int correctionPasses = 3;
            const float separationSkin = 0.002f;
            for (int pass = 0; pass < correctionPasses; pass++)
            {
                bool corrected = false;
                for (int boundaryIndex = 0;
                     boundaryIndex < solidBoundaries.Count;
                     boundaryIndex++)
                {
                    BoxCollider boundary = solidBoundaries[boundaryIndex];
                    if (boundary == null || !boundary.enabled ||
                        !Physics.ComputePenetration(
                            sphere,
                            marble.transform.position,
                            marble.transform.rotation,
                            boundary,
                            boundary.transform.position,
                            boundary.transform.rotation,
                            out Vector3 direction,
                            out float distance))
                    {
                        continue;
                    }

                    Vector3 position = body.position +
                                       (direction * (distance + separationSkin));
                    if (boundary.name == "Admission Gate" && direction.y < 0f)
                    {
                        float sphereRadius = sphere.radius *
                                             Mathf.Max(
                                                 Mathf.Abs(marble.transform.lossyScale.x),
                                                 Mathf.Abs(marble.transform.lossyScale.y));
                        direction = Vector3.up;
                        position.y = boundary.bounds.max.y + sphereRadius + separationSkin;
                    }

                    position.z = MarblePool.TransitDepth;
                    body.position = position;
                    marble.transform.position = position;

                    Vector3 velocity = body.linearVelocity;
                    float inwardSpeed = Vector3.Dot(velocity, direction);
                    if (inwardSpeed < 0f)
                    {
                        body.linearVelocity = velocity - (direction * inwardSpeed);
                    }

                    corrected = true;
                }

                if (!corrected)
                {
                    break;
                }
            }
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                if (descendants[index].name == objectName)
                {
                    return descendants[index];
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            if (boundaryMaterial != null)
            {
                Destroy(boundaryMaterial);
                boundaryMaterial = null;
            }
        }

        private readonly struct ArtworkContactSegment
        {
            public ArtworkContactSegment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }

            public Vector2 Start { get; }

            public Vector2 End { get; }
        }
    }
}
