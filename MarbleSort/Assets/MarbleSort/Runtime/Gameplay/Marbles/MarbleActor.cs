using UnityEngine;

namespace MarbleSort.Gameplay.Marbles
{
    public enum MarbleMotionMode
    {
        Pooled,
        LoosePhysics,
        ConveyorTransition,
        Conveyor,
        ReceiverTransition
    }

    [DisallowMultipleComponent]
    public sealed class MarbleActor : MonoBehaviour
    {
        private const float ReleaseGrowthDuration = 0.14f;
        private const float ExponentialGrowthStrength = 5f;

        private MarblePool owner;
        private Rigidbody body;
        private Renderer visual;
        private Collider marbleCollider;
        private SphereCollider sphereCollider;
        private float scaleTransitionElapsed;
        private float scaleTransitionStart;
        private float scaleTransitionTarget;
        private bool scaleTransitionActive;

        public string ColorId { get; private set; } = string.Empty;

        public Rigidbody Body => body;

        public bool IsRented { get; private set; }

        public MarbleMotionMode MotionMode { get; private set; } = MarbleMotionMode.Pooled;

        public float VisualDiameter => transform.localScale.x;

        public float CollisionDiameter { get; private set; } = MarblePool.TransitMarbleDiameter;

        internal MarblePool Owner => owner;

        internal void ConfigureInfrastructure(MarblePool pool, Rigidbody rigidbody, Renderer marbleRenderer)
        {
            owner = pool;
            body = rigidbody;
            visual = marbleRenderer;
            marbleCollider = GetComponent<Collider>();
            sphereCollider = marbleCollider as SphereCollider;
        }

        internal void Activate(string colorId, Material material, Vector3 position, Vector3 initialVelocity)
        {
            ColorId = MarblePalette.Normalize(colorId);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            visual.sharedMaterial = material;
            CollisionDiameter = MarblePool.TransitMarbleDiameter;
            ApplyVisualDiameter(MarblePool.RestingMarbleDiameter, CollisionDiameter);
            BeginScaleTransition(
                MarblePool.RestingMarbleDiameter,
                MarblePool.TransitMarbleDiameter);
            gameObject.SetActive(true);

            if (marbleCollider != null)
            {
                marbleCollider.enabled = true;
            }

            body.isKinematic = false;
            body.detectCollisions = true;
            body.linearVelocity = initialVelocity;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            IsRented = true;
            MotionMode = MarbleMotionMode.LoosePhysics;
        }

        internal bool BeginConveyorTransition()
        {
            if (!IsRented || MotionMode != MarbleMotionMode.LoosePhysics)
            {
                return false;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.detectCollisions = false;
            if (marbleCollider != null)
            {
                marbleCollider.enabled = false;
            }

            MotionMode = MarbleMotionMode.ConveyorTransition;
            CompleteScaleTransition(MarblePool.TransitMarbleDiameter);
            return true;
        }

        internal void SetConveyorTransitionPosition(Vector3 worldPosition)
        {
            if (MotionMode == MarbleMotionMode.ConveyorTransition)
            {
                transform.position = worldPosition;
            }
        }

        internal bool AttachToConveyor(Vector3 worldPosition)
        {
            if (!IsRented || MotionMode != MarbleMotionMode.ConveyorTransition)
            {
                return false;
            }

            transform.position = worldPosition;
            CollisionDiameter = MarblePool.ConveyorMarbleDiameter;
            CompleteScaleTransition(MarblePool.ConveyorMarbleDiameter);
            MotionMode = MarbleMotionMode.Conveyor;
            return true;
        }

        internal void SetConveyorPosition(Vector3 worldPosition)
        {
            if (MotionMode == MarbleMotionMode.Conveyor)
            {
                transform.position = worldPosition;
            }
        }

        internal bool BeginReceiverTransfer()
        {
            if (!IsRented || MotionMode != MarbleMotionMode.Conveyor)
            {
                return false;
            }

            MotionMode = MarbleMotionMode.ReceiverTransition;
            scaleTransitionActive = false;
            return true;
        }

        internal void SetReceiverTransferPosition(Vector3 worldPosition)
        {
            if (MotionMode == MarbleMotionMode.ReceiverTransition)
            {
                transform.position = worldPosition;
            }
        }

        internal void SetReceiverTransferProgress(float normalizedProgress)
        {
            if (MotionMode != MarbleMotionMode.ReceiverTransition)
            {
                return;
            }

            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(normalizedProgress), 3f);
            float diameter = Mathf.LerpUnclamped(
                MarblePool.ConveyorMarbleDiameter,
                MarblePool.ReceiverMarbleDiameter,
                eased);
            ApplyVisualDiameter(diameter, diameter);
        }

        internal bool ResumeLoosePhysics(Vector3 initialVelocity)
        {
            if (!IsRented || MotionMode != MarbleMotionMode.ConveyorTransition)
            {
                return false;
            }

            if (marbleCollider != null)
            {
                marbleCollider.enabled = true;
            }

            body.detectCollisions = true;
            body.isKinematic = false;
            body.linearVelocity = initialVelocity;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            MotionMode = MarbleMotionMode.LoosePhysics;
            CollisionDiameter = MarblePool.TransitMarbleDiameter;
            CompleteScaleTransition(MarblePool.TransitMarbleDiameter);
            return true;
        }

        internal void Deactivate()
        {
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
                body.detectCollisions = false;
            }

            if (marbleCollider != null)
            {
                marbleCollider.enabled = false;
            }

            ColorId = string.Empty;
            IsRented = false;
            MotionMode = MarbleMotionMode.Pooled;
            scaleTransitionActive = false;
            CollisionDiameter = MarblePool.TransitMarbleDiameter;
            ApplyVisualDiameter(MarblePool.TransitMarbleDiameter, CollisionDiameter);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!scaleTransitionActive || MotionMode != MarbleMotionMode.LoosePhysics)
            {
                return;
            }

            scaleTransitionElapsed += Time.deltaTime;
            float normalized = ReleaseGrowthDuration <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01(scaleTransitionElapsed / ReleaseGrowthDuration);
            float exponential = 1f - Mathf.Exp(-ExponentialGrowthStrength * normalized);
            float exponentialEnd = 1f - Mathf.Exp(-ExponentialGrowthStrength);
            float eased = exponentialEnd <= Mathf.Epsilon
                ? normalized
                : exponential / exponentialEnd;
            float diameter = Mathf.LerpUnclamped(
                scaleTransitionStart,
                scaleTransitionTarget,
                eased);
            ApplyVisualDiameter(diameter, CollisionDiameter);

            if (normalized >= 1f)
            {
                CompleteScaleTransition(scaleTransitionTarget);
            }
        }

        private void BeginScaleTransition(float startDiameter, float targetDiameter)
        {
            scaleTransitionElapsed = 0f;
            scaleTransitionStart = startDiameter;
            scaleTransitionTarget = targetDiameter;
            scaleTransitionActive = true;
        }

        private void CompleteScaleTransition(float diameter)
        {
            scaleTransitionActive = false;
            ApplyVisualDiameter(diameter, CollisionDiameter);
        }

        private void ApplyVisualDiameter(float visualDiameter, float physicalDiameter)
        {
            float safeVisualDiameter = Mathf.Max(0.01f, visualDiameter);
            transform.localScale = Vector3.one * safeVisualDiameter;

            if (sphereCollider != null)
            {
                sphereCollider.radius = Mathf.Max(0.01f, physicalDiameter) /
                                        (2f * safeVisualDiameter);
            }
        }
    }
}
