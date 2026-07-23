using System;
using UnityEngine;

namespace MarbleSort.Presentation
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class RuntimePerformanceProbe : MonoBehaviour
    {
        [SerializeField, Range(30, 300)] private int sampleWindow = 120;

        private float[] frameTimes = Array.Empty<float>();
        private int writeIndex;
        private int sampleCount;
        private float frameTimeSum;
        private int initialGenerationZeroCollections;

        public int FrameSampleCount => sampleCount;

        public float AverageFramesPerSecond => frameTimeSum <= 0f
            ? 0f
            : sampleCount / frameTimeSum;

        public float WorstFrameMilliseconds
        {
            get
            {
                float worst = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    worst = Mathf.Max(worst, frameTimes[index]);
                }

                return worst * 1000f;
            }
        }

        public int GenerationZeroCollections =>
            Math.Max(0, GC.CollectionCount(0) - initialGenerationZeroCollections);

        public void ResetMeasurements()
        {
            if (frameTimes.Length > 0)
            {
                Array.Clear(frameTimes, 0, frameTimes.Length);
            }

            writeIndex = 0;
            sampleCount = 0;
            frameTimeSum = 0f;
            initialGenerationZeroCollections = GC.CollectionCount(0);
        }

        private void Awake()
        {
            frameTimes = new float[Mathf.Clamp(sampleWindow, 30, 300)];
            ResetMeasurements();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f || frameTimes.Length == 0)
            {
                return;
            }

            if (sampleCount == frameTimes.Length)
            {
                frameTimeSum -= frameTimes[writeIndex];
            }
            else
            {
                sampleCount++;
            }

            frameTimes[writeIndex] = deltaTime;
            frameTimeSum += deltaTime;
            writeIndex = (writeIndex + 1) % frameTimes.Length;
        }

        private void OnValidate()
        {
            sampleWindow = Mathf.Clamp(sampleWindow, 30, 300);
        }
    }
}
