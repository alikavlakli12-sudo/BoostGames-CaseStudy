using UnityEngine;

namespace MarbleSort.Gameplay.Marbles
{
    [DisallowMultipleComponent]
    public sealed class MarbleActor : MonoBehaviour
    {
        private MarblePool owner;
        private Rigidbody body;
        private Renderer visual;

        public string ColorId { get; private set; } = string.Empty;

        public Rigidbody Body => body;

        public bool IsRented { get; private set; }

        internal MarblePool Owner => owner;

        internal void ConfigureInfrastructure(MarblePool pool, Rigidbody rigidbody, Renderer marbleRenderer)
        {
            owner = pool;
            body = rigidbody;
            visual = marbleRenderer;
        }

        internal void Activate(string colorId, Material material, Vector3 position, Vector3 initialVelocity)
        {
            ColorId = MarblePalette.Normalize(colorId);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            visual.sharedMaterial = material;
            gameObject.SetActive(true);

            body.isKinematic = false;
            body.linearVelocity = initialVelocity;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            IsRented = true;
        }

        internal void Deactivate()
        {
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            ColorId = string.Empty;
            IsRented = false;
            gameObject.SetActive(false);
        }
    }
}
