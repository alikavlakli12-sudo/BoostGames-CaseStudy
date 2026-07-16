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
        private MarblePool owner;
        private Rigidbody body;
        private Renderer visual;
        private Collider marbleCollider;

        public string ColorId { get; private set; } = string.Empty;

        public Rigidbody Body => body;

        public bool IsRented { get; private set; }

        public MarbleMotionMode MotionMode { get; private set; } = MarbleMotionMode.Pooled;

        internal MarblePool Owner => owner;

        internal void ConfigureInfrastructure(MarblePool pool, Rigidbody rigidbody, Renderer marbleRenderer)
        {
            owner = pool;
            body = rigidbody;
            visual = marbleRenderer;
            marbleCollider = GetComponent<Collider>();
        }

        internal void Activate(string colorId, Material material, Vector3 position, Vector3 initialVelocity)
        {
            ColorId = MarblePalette.Normalize(colorId);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            visual.sharedMaterial = material;
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
            return true;
        }

        internal void SetReceiverTransferPosition(Vector3 worldPosition)
        {
            if (MotionMode == MarbleMotionMode.ReceiverTransition)
            {
                transform.position = worldPosition;
            }
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
            gameObject.SetActive(false);
        }
    }
}
