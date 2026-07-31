using UnityEngine;

namespace IdleTower.Audio
{
    /// <summary>Тестовые клипы, пока в Inspector нет своих AudioClip.</summary>
    public static class ProceduralSfx
    {
        public static AudioClip Create(SfxId id)
        {
            switch (id)
            {
                case SfxId.RoomBuilt:
                    return CreateTone("Sfx_RoomBuilt", frequency: 660f, durationSeconds: 0.12f);
                case SfxId.ModeChanged:
                    return CreateTone("Sfx_ModeChanged", frequency: 480f, durationSeconds: 0.09f);
                case SfxId.ModeUnlocked:
                    return CreateTone("Sfx_ModeUnlocked", frequency: 880f, durationSeconds: 0.16f);
                default:
                    return CreateTone($"Sfx_{id}", frequency: 400f, durationSeconds: 0.1f);
            }
        }

        private static AudioClip CreateTone(string name, float frequency, float durationSeconds)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - t / durationSeconds;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.35f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
