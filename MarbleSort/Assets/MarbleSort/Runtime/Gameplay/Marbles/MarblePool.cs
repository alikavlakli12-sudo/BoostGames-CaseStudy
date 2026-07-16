using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Gameplay.Marbles
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class MarblePool : MonoBehaviour
    {
        [SerializeField] private MarblePalette palette;
        [SerializeField, Min(1)] private int initialCapacity = 72;
        [SerializeField, Min(0.05f)] private float marbleDiameter = 0.22f;
        [SerializeField] private float returnBelowY = -8.5f;

        private readonly Stack<MarbleActor> available = new Stack<MarbleActor>();
        private readonly List<MarbleActor> active = new List<MarbleActor>();
        private bool initialized;

        public int ActiveCount => active.Count;

        public int AvailableCount => available.Count;

        public int CreatedCount { get; private set; }

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

            MarbleActor marble = available.Count > 0 ? available.Pop() : CreateMarble();
            Material material = palette == null ? null : palette.GetMaterial(colorId);
            marble.Activate(colorId, material, position, initialVelocity);
            active.Add(marble);
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

        private void LateUpdate()
        {
            for (int index = active.Count - 1; index >= 0; index--)
            {
                if (active[index].MotionMode == MarbleMotionMode.LoosePhysics &&
                    active[index].transform.position.y < returnBelowY)
                {
                    ReturnAt(index);
                }
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
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezePositionZ |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationY;
            body.solverIterations = 6;
            body.solverVelocityIterations = 2;

            SphereCollider sphere = marbleObject.GetComponent<SphereCollider>();
            sphere.contactOffset = 0.01f;

            MarbleActor marble = marbleObject.AddComponent<MarbleActor>();
            marble.ConfigureInfrastructure(this, body, marbleObject.GetComponent<Renderer>());
            marble.Deactivate();
            CreatedCount++;
            return marble;
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
    }
}
