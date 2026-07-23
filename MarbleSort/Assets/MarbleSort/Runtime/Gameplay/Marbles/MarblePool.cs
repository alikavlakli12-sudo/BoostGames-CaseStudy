using System.Collections.Generic;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.Marbles
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class MarblePool : MonoBehaviour
    {
        // 36 loose board marbles + 24 conveyor occupants + four receiver
        // transitions require at most 64 actors. Eight spare actors keep the
        // production pool tolerant of short hand-off timing overlaps.
        public const int DefaultInitialCapacity = 72;
        public const float RestingMarbleDiameter = 0.254f;
        public const float ReceiverMarbleDiameter = 0.365f;
        public const float TransitMarbleDiameter = 0.52f;
        public const float TransitCollisionDiameter = 0.54f;
        public const float ConveyorMarbleDiameter = TransitMarbleDiameter;
        public const float TransitDepth = -0.16f;
        public const float MinimumMarbleSeparation = 0.012f;
        private const float RenderSeparationBuffer = 0.002f;

        [SerializeField] private MarblePalette palette;
        [SerializeField, Min(1)] private int initialCapacity = DefaultInitialCapacity;
        [SerializeField, Min(0.05f)] private float marbleDiameter = TransitMarbleDiameter;
        [SerializeField] private float returnBelowY = -8.5f;

        private readonly Stack<MarbleActor> available = new Stack<MarbleActor>();
        private readonly List<MarbleActor> active = new List<MarbleActor>();
        private PhysicsMaterial marblePhysicsMaterial;
        private ChuteBoundaryRig chuteBoundaryRig;
        private bool initialized;

        public int ActiveCount => active.Count;

        public int AvailableCount => available.Count;

        public int InitialCapacity => initialCapacity;

        public int CreatedCount { get; private set; }

        public int RuntimeExpansionCount { get; private set; }

        public int PeakActiveCount { get; private set; }

        public int PeakLooseMarbleCount { get; private set; }

        public int LooseMarbleCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < active.Count; index++)
                {
                    MarbleActor marble = active[index];
                    if (marble != null && marble.IsRented &&
                        marble.MotionMode == MarbleMotionMode.LoosePhysics)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public float MarbleDiameter => marbleDiameter;

        public float LastRenderedMinimumSeparation { get; private set; } = float.MaxValue;

        public int ReturnedBelowBoardCount { get; private set; }

        public Vector3 LastReturnedBelowBoardPosition { get; private set; }

        public string LastReturnedBelowBoardColorId { get; private set; } = string.Empty;

        public bool HasClearance(Vector3 worldPosition, float requestedDiameter)
        {
            return !TryGetClearanceBlocker(worldPosition, requestedDiameter, out _);
        }

        private bool TryGetClearanceBlocker(
            Vector3 worldPosition,
            float requestedDiameter,
            out MarbleActor blocker)
        {
            blocker = null;
            float safeRequestedDiameter = Mathf.Max(0.01f, requestedDiameter);
            for (int index = 0; index < active.Count; index++)
            {
                MarbleActor marble = active[index];
                if (marble == null || !marble.IsRented ||
                    marble.MotionMode == MarbleMotionMode.Pooled ||
                    marble.MotionMode == MarbleMotionMode.ReceiverTransition)
                {
                    continue;
                }

                float requiredDistance =
                    ((safeRequestedDiameter + marble.CollisionDiameter) * 0.5f) +
                    MinimumMarbleSeparation;
                Vector3 marblePosition = marble.Body != null
                    ? marble.Body.position
                    : marble.transform.position;
                Vector2 offset = marblePosition - worldPosition;
                if (offset.sqrMagnitude < requiredDistance * requiredDistance)
                {
                    blocker = marble;
                    return true;
                }
            }

            return false;
        }

        public void Configure(
            MarblePalette marblePalette,
            int capacity,
            float diameter,
            float recycleBelowY)
        {
            palette = marblePalette;
            initialCapacity = Mathf.Max(1, capacity);
            marbleDiameter = Mathf.Max(0.05f, diameter);
            returnBelowY = recycleBelowY;
        }

        public void Prewarm()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            for (int index = 0; index < initialCapacity; index++)
            {
                available.Push(CreateMarble());
            }
        }

        public MarbleActor Rent(string colorId, Vector3 position, Vector3 initialVelocity)
        {
            Prewarm();

            MarbleActor marble;
            if (available.Count > 0)
            {
                marble = available.Pop();
            }
            else
            {
                marble = CreateMarble();
                RuntimeExpansionCount++;
            }

            Material material = palette == null
                ? null
                : PresentationMaterialLibrary.GetGlossyBall(palette.GetMaterial(colorId));
            marble.Activate(colorId, material, position, initialVelocity);
            active.Add(marble);
            PeakActiveCount = Mathf.Max(PeakActiveCount, active.Count);
            PeakLooseMarbleCount = Mathf.Max(PeakLooseMarbleCount, LooseMarbleCount);
            return marble;
        }

        public bool Return(MarbleActor marble)
        {
            if (marble == null || marble.Owner != this || !marble.IsRented)
            {
                return false;
            }

            int index = active.IndexOf(marble);
            if (index < 0)
            {
                return false;
            }

            ReturnAt(index);
            return true;
        }

        public void ReturnAll()
        {
            for (int index = active.Count - 1; index >= 0; index--)
            {
                ReturnAt(index);
            }
        }

        private void Awake()
        {
            Prewarm();
        }

        private void Start()
        {
            chuteBoundaryRig = FindFirstObjectByType<ChuteBoundaryRig>();
        }

        private void FixedUpdate()
        {
            // The admission cap bounds this pairwise work to 36 loose marbles.
            // Twelve physics passes plus the render projection below keep dense
            // piles separated without returning to the former 88-pass budget.
            ResolveLooseMarbleOverlaps(12);
        }

        private void LateUpdate()
        {
            // Physics owns the simulation. This final projection only removes
            // sub-frame solver tolerance before rendering. Projecting the same
            // marbles back inside the solid artwork contour afterward guarantees
            // that solving a dense pile cannot trade overlap for wall penetration.
            const int constraintCycles = 8;
            const int separationPassesPerCycle = 4;
            for (int cycle = 0; cycle < constraintCycles; cycle++)
            {
                ResolveLooseMarbleOverlaps(separationPassesPerCycle);
                ProjectLooseMarblesInsideSolidChute();
            }

            LastRenderedMinimumSeparation = MeasureMinimumScreenSeparation();
            for (int index = active.Count - 1; index >= 0; index--)
            {
                Vector3 position = active[index].Body != null
                    ? active[index].Body.position
                    : active[index].transform.position;
                if (active[index].MotionMode == MarbleMotionMode.LoosePhysics &&
                    position.y < returnBelowY)
                {
                    LastReturnedBelowBoardPosition = position;
                    LastReturnedBelowBoardColorId = active[index].ColorId;
                    ReturnedBelowBoardCount++;
                    ReturnAt(index);
                }
            }
        }

        private void ProjectLooseMarblesInsideSolidChute()
        {
            if (chuteBoundaryRig == null)
            {
                chuteBoundaryRig = FindFirstObjectByType<ChuteBoundaryRig>();
            }

            if (chuteBoundaryRig == null)
            {
                return;
            }

            for (int index = 0; index < active.Count; index++)
            {
                chuteBoundaryRig.ProjectLooseMarbleInsideSolidChute(active[index]);
            }
        }

        private void ResolveLooseMarbleOverlaps(int separationPasses = 24)
        {
            for (int pass = 0; pass < separationPasses; pass++)
            {
                bool correctedAny = false;
                for (int firstIndex = 0; firstIndex < active.Count; firstIndex++)
                {
                    MarbleActor first = active[firstIndex];
                    if (!CanParticipateInSeparation(first))
                    {
                        continue;
                    }

                    for (int secondIndex = firstIndex + 1; secondIndex < active.Count; secondIndex++)
                    {
                        MarbleActor second = active[secondIndex];
                        if (!CanParticipateInSeparation(second))
                        {
                            continue;
                        }

                        bool moveFirst = first.MotionMode == MarbleMotionMode.LoosePhysics;
                        bool moveSecond = second.MotionMode == MarbleMotionMode.LoosePhysics;
                        if (!moveFirst && !moveSecond)
                        {
                            continue;
                        }

                        Vector3 difference3D = second.Body.position - first.Body.position;
                        Vector2 difference = new Vector2(difference3D.x, difference3D.y);
                        float requiredDistance =
                            ((first.CollisionDiameter + second.CollisionDiameter) * 0.5f) +
                            MinimumMarbleSeparation +
                            RenderSeparationBuffer;
                        float distance = difference.magnitude;
                        if (distance >= requiredDistance)
                        {
                            continue;
                        }

                        Vector2 direction = distance > 0.0001f
                            ? difference / distance
                            : DeterministicSeparationDirection(first, second);
                        float correction = requiredDistance - distance;
                        if (moveFirst && moveSecond)
                        {
                            MoveLooseMarble(first, -direction * (correction * 0.5f));
                            MoveLooseMarble(second, direction * (correction * 0.5f));
                        }
                        else if (moveFirst)
                        {
                            MoveLooseMarble(first, -direction * correction);
                        }
                        else
                        {
                            MoveLooseMarble(second, direction * correction);
                        }

                        correctedAny = true;
                    }
                }

                if (!correctedAny)
                {
                    break;
                }
            }

        }

        private float MeasureMinimumScreenSeparation()
        {
            float minimum = float.MaxValue;
            for (int firstIndex = 0; firstIndex < active.Count; firstIndex++)
            {
                MarbleActor first = active[firstIndex];
                if (!CanParticipateInSeparation(first))
                {
                    continue;
                }

                for (int secondIndex = firstIndex + 1; secondIndex < active.Count; secondIndex++)
                {
                    MarbleActor second = active[secondIndex];
                    if (!CanParticipateInSeparation(second))
                    {
                        continue;
                    }

                    if (first.MotionMode != MarbleMotionMode.LoosePhysics &&
                        second.MotionMode != MarbleMotionMode.LoosePhysics)
                    {
                        continue;
                    }

                    Vector3 difference = second.Body.position - first.Body.position;
                    float distance = new Vector2(difference.x, difference.y).magnitude;
                    minimum = Mathf.Min(minimum, distance);
                }
            }

            return minimum;
        }

        private static bool CanParticipateInSeparation(MarbleActor marble)
        {
            return marble != null && marble.IsRented &&
                   marble.MotionMode != MarbleMotionMode.Pooled &&
                   marble.MotionMode != MarbleMotionMode.ReceiverTransition;
        }

        private static Vector2 DeterministicSeparationDirection(
            MarbleActor first,
            MarbleActor second)
        {
            int hash = first.GetInstanceID() ^ second.GetInstanceID();
            float angle = Mathf.Repeat(Mathf.Abs(hash) * 0.6180339f, 1f) * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static void MoveLooseMarble(MarbleActor marble, Vector2 correction)
        {
            if (marble == null || marble.Body == null ||
                marble.MotionMode != MarbleMotionMode.LoosePhysics)
            {
                return;
            }

            Vector3 position = marble.Body.position;
            position.x += correction.x;
            position.y += correction.y;
            position.z = TransitDepth;
            marble.Body.position = position;

            if (correction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 outward = correction.normalized;
            Vector3 velocity3D = marble.Body.linearVelocity;
            Vector2 velocity = new Vector2(velocity3D.x, velocity3D.y);
            float inwardSpeed = Vector2.Dot(velocity, outward);
            if (inwardSpeed < 0f)
            {
                velocity -= outward * inwardSpeed;
                marble.Body.linearVelocity = new Vector3(velocity.x, velocity.y, 0f);
            }
        }

        private MarbleActor CreateMarble()
        {
            GameObject marbleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marbleObject.name = $"Pooled Marble {CreatedCount + 1:000}";
            marbleObject.transform.SetParent(transform, false);
            marbleObject.transform.localScale = Vector3.one * marbleDiameter;

            Rigidbody body = marbleObject.AddComponent<Rigidbody>();
            body.mass = 0.12f;
            body.linearDamping = 0.08f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationY;
            body.solverIterations = 24;
            body.solverVelocityIterations = 12;
            body.maxDepenetrationVelocity = 12f;

            SphereCollider sphere = marbleObject.GetComponent<SphereCollider>();
            sphere.radius = 0.5f;
            sphere.contactOffset = 0.012f;
            sphere.sharedMaterial = GetMarblePhysicsMaterial();

            Renderer marbleRenderer = marbleObject.GetComponent<Renderer>();
            marbleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            marbleRenderer.receiveShadows = false;

            MarbleActor marble = marbleObject.AddComponent<MarbleActor>();
            marble.ConfigureInfrastructure(this, body, marbleRenderer);
            marble.Deactivate();
            CreatedCount++;
            return marble;
        }

        private PhysicsMaterial GetMarblePhysicsMaterial()
        {
            if (marblePhysicsMaterial != null)
            {
                return marblePhysicsMaterial;
            }

            marblePhysicsMaterial = new PhysicsMaterial("Marble Low-Friction Physics")
            {
                dynamicFriction = 0.04f,
                staticFriction = 0.02f,
                bounciness = 0.025f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return marblePhysicsMaterial;
        }

        private void ReturnAt(int index)
        {
            MarbleActor marble = active[index];
            int lastIndex = active.Count - 1;
            active[index] = active[lastIndex];
            active.RemoveAt(lastIndex);
            marble.Deactivate();
            available.Push(marble);
        }

        private void OnValidate()
        {
            initialCapacity = Mathf.Max(1, initialCapacity);
            marbleDiameter = Mathf.Max(0.05f, marbleDiameter);
        }

        private void OnDestroy()
        {
            if (marblePhysicsMaterial != null)
            {
                Destroy(marblePhysicsMaterial);
                marblePhysicsMaterial = null;
            }
        }
    }
}
