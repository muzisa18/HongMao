using System.Collections.Generic;
using UnityEngine;

namespace HongMao
{
    public enum PrototypeSound { Hit, Parry, Dodge, Absorb, PoiseBreak, Mask, Win, Lose }

    [RequireComponent(typeof(AudioSource))]
    public sealed class PrototypeAudio : MonoBehaviour
    {
        readonly Dictionary<PrototypeSound, AudioClip> m_Clips = new();
        AudioSource m_Source;

        public void Configure()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Clips[PrototypeSound.Hit] = Build("Hit", 150f, 0.08f, true);
            m_Clips[PrototypeSound.Parry] = Build("Parry", 880f, 0.13f, false);
            m_Clips[PrototypeSound.Dodge] = Build("Dodge", 440f, 0.09f, false);
            m_Clips[PrototypeSound.Absorb] = Build("Absorb", 620f, 0.22f, false);
            m_Clips[PrototypeSound.PoiseBreak] = Build("PoiseBreak", 90f, 0.25f, true);
            m_Clips[PrototypeSound.Mask] = Build("Mask", 240f, 0.42f, false);
            m_Clips[PrototypeSound.Win] = Build("Win", 720f, 0.34f, false);
            m_Clips[PrototypeSound.Lose] = Build("Lose", 110f, 0.34f, false);
        }

        public void Play(PrototypeSound sound, float volume = 0.35f)
        {
            if (m_Source != null && m_Clips.TryGetValue(sound, out AudioClip clip))
                m_Source.PlayOneShot(clip, volume);
        }

        static AudioClip Build(string clipName, float frequency, float duration, bool noise)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)count;
                float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f);
                if (noise) wave = wave * 0.55f + Random.Range(-0.45f, 0.45f);
                samples[i] = wave * envelope * 0.45f;
            }
            AudioClip clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
