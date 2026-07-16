using UnityEngine;

namespace MarbleSort.Presentation
{
    public sealed class ProceduralAudioBank : System.IDisposable
    {
        private const int SampleRate = 44100;

        public ProceduralAudioBank()
        {
            Tap = CreateTone("Tap", 0.07f, 520f, 390f, 0.16f);
            Admission = CreateTone("Admission", 0.055f, 690f, 820f, 0.1f);
            Collection = CreateTone("Collection", 0.08f, 780f, 1040f, 0.12f);
            ReceiverComplete = CreateArpeggio("Receiver Complete", 0.2f, 0.13f);
            LevelComplete = CreateVictory("Level Complete", 0.52f, 0.15f);
            Deadlock = CreateTone("Deadlock", 0.34f, 280f, 105f, 0.14f);
        }

        public AudioClip Tap { get; }

        public AudioClip Admission { get; }

        public AudioClip Collection { get; }

        public AudioClip ReceiverComplete { get; }

        public AudioClip LevelComplete { get; }

        public AudioClip Deadlock { get; }

        public void Dispose()
        {
            DestroyClip(Tap);
            DestroyClip(Admission);
            DestroyClip(Collection);
            DestroyClip(ReceiverComplete);
            DestroyClip(LevelComplete);
            DestroyClip(Deadlock);
        }

        private static AudioClip CreateTone(
            string name,
            float duration,
            float startFrequency,
            float endFrequency,
            float volume)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
                phase += (Mathf.PI * 2f * frequency) / SampleRate;
                float envelope = Mathf.Sin(Mathf.PI * normalized) * (1f - (normalized * 0.35f));
                samples[index] = Mathf.Sin(phase) * envelope * volume;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateArpeggio(string name, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (int index = 0; index < sampleCount; index++)
            {
                float normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                int noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(normalized * notes.Length));
                float local = Mathf.Repeat(normalized * notes.Length, 1f);
                float envelope = Mathf.Sin(Mathf.PI * local) * (1f - (normalized * 0.25f));
                samples[index] = Mathf.Sin((Mathf.PI * 2f * notes[noteIndex] * index) / SampleRate) *
                                 envelope * volume;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateVictory(string name, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            float[] chord = { 523.25f, 659.25f, 783.99f };
            for (int index = 0; index < sampleCount; index++)
            {
                float normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(normalized * 2.2f)) *
                                 Mathf.Pow(1f - normalized, 0.45f);
                float value = 0f;
                for (int note = 0; note < chord.Length; note++)
                {
                    value += Mathf.Sin((Mathf.PI * 2f * chord[note] * index) / SampleRate);
                }

                samples[index] = (value / chord.Length) * envelope * volume;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(clip);
            }
            else
            {
                Object.DestroyImmediate(clip);
            }
        }
    }
}
