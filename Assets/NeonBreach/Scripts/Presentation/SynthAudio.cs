using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreach
{
    public enum SoundCue { Shot, Hit, Kill, Reload, ReloadEnd, Hurt, Pickup, Wave, EnemyShot, Empty, Jump }
    public sealed class SynthAudio : MonoBehaviour
    {
        readonly Dictionary<SoundCue, AudioClip> clips = new Dictionary<SoundCue, AudioClip>();
        AudioSource source;
        public bool Muted { get; private set; }
        void Awake()
        {
            Muted = PlayerPrefs.GetInt("NeonBreach.Muted", 0) != 0;
            source = gameObject.AddComponent<AudioSource>(); source.mute = Muted; source.spatialBlend = 0; source.volume = 0.55f;
            foreach (SoundCue cue in Enum.GetValues(typeof(SoundCue))) clips[cue] = Generate(cue);
        }
        public void ToggleMute() { Muted = !Muted; source.mute = Muted; PlayerPrefs.SetInt("NeonBreach.Muted", Muted ? 1 : 0); PlayerPrefs.Save(); }
        public void Play(SoundCue cue, float volume = 1f) { source.PlayOneShot(clips[cue], volume); }
        AudioClip Generate(SoundCue cue)
        {
            const int rate = 22050;
            float duration = cue == SoundCue.Wave ? 0.7f : cue == SoundCue.Reload ? 0.32f : cue == SoundCue.Kill ? 0.24f : 0.13f;
            var samples = new float[(int)(rate * duration)]; var random = new System.Random((int)cue + 77);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, p = t / duration, envelope = Mathf.Pow(1 - p, 2);
                float noise = (float)random.NextDouble() * 2 - 1, value;
                switch (cue)
                {
                    case SoundCue.Shot: value = noise * 0.6f + Mathf.Sin(2 * Mathf.PI * (160 * t - 300 * t * t)) * 0.4f; break;
                    case SoundCue.EnemyShot: value = Mathf.Sin(2 * Mathf.PI * (480 * t - 800 * t * t)) * 0.5f + noise * 0.2f; break;
                    case SoundCue.Hit: value = Mathf.Sin(2 * Mathf.PI * 1400 * t) * 0.4f + noise * 0.2f; break;
                    case SoundCue.Kill: value = Mathf.Sin(2 * Mathf.PI * (620 * t + 1100 * t * t)) * 0.55f; break;
                    case SoundCue.Reload: value = noise * (Mathf.Sin(t * 80) > 0.7f ? 0.75f : 0.1f); break;
                    case SoundCue.ReloadEnd: value = noise * 0.45f + Mathf.Sin(2 * Mathf.PI * 750 * t) * 0.3f; break;
                    case SoundCue.Hurt: value = noise * 0.3f + Mathf.Sin(2 * Mathf.PI * 75 * t) * 0.6f; break;
                    case SoundCue.Pickup: value = Mathf.Sin(2 * Mathf.PI * (p < 0.5f ? 880 : 1320) * t) * 0.55f; break;
                    case SoundCue.Wave: value = (Mathf.Sin(2 * Mathf.PI * 220 * t) + Mathf.Sin(2 * Mathf.PI * 330 * t) + Mathf.Sin(2 * Mathf.PI * 440 * t)) * 0.2f; break;
                    case SoundCue.Jump: value = noise * 0.2f; break;
                    default: value = noise * 0.25f; break;
                }
                samples[i] = Mathf.Clamp(value * envelope * Mathf.Min(1, t * 800), -1, 1);
            }
            var clip = AudioClip.Create(cue.ToString(), samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void OnDestroy() { foreach (var clip in clips.Values) Destroy(clip); }
    }
}
