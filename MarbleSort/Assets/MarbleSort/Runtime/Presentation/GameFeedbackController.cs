using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.Receivers;
using MarbleSort.Gameplay.TopGrid;
using UnityEngine;

namespace MarbleSort.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GameFeedbackController : MonoBehaviour
    {
        [SerializeField] private TopGridController topGrid;
        [SerializeField] private ConveyorAdmissionController admission;
        [SerializeField] private ReceiverQueueController receivers;
        [SerializeField] private LevelFlowController levelFlow;
        [SerializeField] private MarblePalette palette;
        [SerializeField] private Material particleMaterial;

        private AudioSource audioSource;
        private ParticleSystem burstParticles;
        private ProceduralAudioBank audioBank;
        private float lastAdmissionSoundTime = -1f;
        private float lastCollectionSoundTime = -1f;

        public bool AudioReady => audioBank != null && audioSource != null;

        public ParticleSystem BurstParticles => burstParticles;

        public int BurstEventCount { get; private set; }

        public void Configure(
            TopGridController grid,
            ConveyorAdmissionController admissionController,
            ReceiverQueueController receiverController,
            LevelFlowController flow,
            MarblePalette marblePalette,
            Material particles)
        {
            topGrid = grid;
            admission = admissionController;
            receivers = receiverController;
            levelFlow = flow;
            palette = marblePalette;
            particleMaterial = particles;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.72f;
            audioBank = new ProceduralAudioBank();
            burstParticles = CreateBurstParticles();
        }

        private void Start()
        {
            if (topGrid == null || admission == null || receivers == null || levelFlow == null)
            {
                Debug.LogError("Game feedback requires every gameplay event source.", this);
                enabled = false;
                return;
            }

            topGrid.BoxSelected += HandleBoxSelected;
            admission.MarbleAdmitted += HandleMarbleAdmitted;
            receivers.MarbleCollected += HandleMarbleCollected;
            receivers.ReceiverCompleted += HandleReceiverCompleted;
            levelFlow.StatusChanged += HandleStatusChanged;
            levelFlow.LevelStarted += HandleLevelStarted;
        }

        private ParticleSystem CreateBurstParticles()
        {
            GameObject particleObject = new GameObject("Pooled Feedback Particles");
            particleObject.SetActive(false);
            particleObject.transform.SetParent(transform, false);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            main.gravityModifier = 0.08f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            renderer.sortingOrder = 20;
            particleObject.SetActive(true);
            return particles;
        }

        private void HandleBoxSelected(string boxId, string colorId, Vector3 position)
        {
            EmitBurst(position + Vector3.back * 0.4f, GetColor(colorId), 9, 1.25f);
            Play(audioBank.Tap, 0.9f);
        }

        private void HandleMarbleAdmitted(int slotIndex, string colorId, MarbleActor marble)
        {
            Vector3 position = marble == null ? transform.position : marble.transform.position;
            EmitBurst(position + Vector3.back * 0.2f, GetColor(colorId), 3, 0.65f);
            if (Time.unscaledTime - lastAdmissionSoundTime >= 0.065f)
            {
                lastAdmissionSoundTime = Time.unscaledTime;
                Play(audioBank.Admission, 0.42f);
            }
        }

        private void HandleMarbleCollected(ReceiverAcceptanceResult result)
        {
            Vector3 position = receivers.GetCollectionWorldPosition(result.LaneIndex);
            EmitBurst(position + Vector3.back * 0.35f, GetColor(result.ColorId), 5, 0.8f);
            if (Time.unscaledTime - lastCollectionSoundTime >= 0.045f)
            {
                lastCollectionSoundTime = Time.unscaledTime;
                Play(audioBank.Collection, 0.62f);
            }
        }

        private void HandleReceiverCompleted(int laneIndex, string boxId, string colorId)
        {
            // The receiver owns its precisely timed four-star completion burst so it can
            // begin on the first frame after the lid closes. Keep the shared completion
            // sound here without layering the old generic sphere particles over the stars.
            Play(audioBank.ReceiverComplete, 0.88f);
        }

        private void HandleStatusChanged(LevelFlowStatus status)
        {
            if (status == LevelFlowStatus.Complete)
            {
                EmitCelebration();
                Play(audioBank.LevelComplete, 1f);
                MobileHaptics.VibrateMajor();
            }
            else if (status == LevelFlowStatus.Deadlocked)
            {
                Play(audioBank.Deadlock, 0.9f);
                MobileHaptics.VibrateMajor();
            }
        }

        private void HandleLevelStarted(int levelIndex)
        {
            if (burstParticles != null)
            {
                burstParticles.Clear(true);
            }
        }

        private void EmitCelebration()
        {
            Color[] colors =
            {
                GetColor("green"),
                GetColor("blue"),
                GetColor("orange"),
                GetColor("yellow")
            };
            for (int index = 0; index < colors.Length; index++)
            {
                Vector3 position = new Vector3(-2.7f + (index * 1.8f), -0.4f, -0.6f);
                EmitBurst(position, colors[index], 14, 2f);
            }
        }

        private void EmitBurst(Vector3 position, Color color, int count, float speedMultiplier)
        {
            if (burstParticles == null)
            {
                return;
            }

            BurstEventCount++;
            for (int index = 0; index < count; index++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
                {
                    position = position,
                    startColor = color,
                    startLifetime = Random.Range(0.26f, 0.48f),
                    startSize = Random.Range(0.07f, 0.14f),
                    velocity = new Vector3(direction.x, direction.y, 0f) *
                               Random.Range(0.55f, 1.35f) * speedMultiplier
                };
                burstParticles.Emit(emit, 1);
            }
        }

        private Color GetColor(string colorId)
        {
            Material material = palette == null ? null : palette.GetMaterial(colorId);
            return material == null ? Color.white : material.color;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        private void OnDestroy()
        {
            if (topGrid != null)
            {
                topGrid.BoxSelected -= HandleBoxSelected;
            }

            if (admission != null)
            {
                admission.MarbleAdmitted -= HandleMarbleAdmitted;
            }

            if (receivers != null)
            {
                receivers.MarbleCollected -= HandleMarbleCollected;
                receivers.ReceiverCompleted -= HandleReceiverCompleted;
            }

            if (levelFlow != null)
            {
                levelFlow.StatusChanged -= HandleStatusChanged;
                levelFlow.LevelStarted -= HandleLevelStarted;
            }

            audioBank?.Dispose();
            audioBank = null;
        }
    }

    public static class MobileHaptics
    {
        public static void VibrateMajor()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (Application.isMobilePlatform)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
