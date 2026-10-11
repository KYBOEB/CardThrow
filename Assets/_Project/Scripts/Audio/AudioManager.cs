using System.Collections.Generic;
using UnityEngine;

namespace CardThrow
{
    // Простой звуковой менеджер: пул источников для коротких звуков + отдельные источники для лупов.
    // Вызов из любого места: AudioManager.Play(SoundId.CardThrow);
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SoundLibrary library;
        [SerializeField, Min(1)] private int sfxVoices = 12;

        private AudioSource[] voices;
        private int nextVoice;
        private readonly Dictionary<SoundId, AudioSource> loops = new Dictionary<SoundId, AudioSource>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            voices = new AudioSource[sfxVoices];
            for (int i = 0; i < sfxVoices; i++) voices[i] = CreateSource(false);
        }

        public static void Play(SoundId id) { if (Instance != null) Instance.PlayInternal(id); }
        public static void PlayLoop(SoundId id) { if (Instance != null) Instance.PlayLoopInternal(id); }
        public static void StopLoop(SoundId id) { if (Instance != null) Instance.StopLoopInternal(id); }

        private void PlayInternal(SoundId id)
        {
            if (!TryPickClip(id, out AudioClip clip, out SoundLibrary.Entry entry)) return;
            AudioSource src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            src.clip = clip;
            src.volume = entry.volume;
            src.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
            src.Play();
        }

        private void PlayLoopInternal(SoundId id)
        {
            if (!TryPickClip(id, out AudioClip clip, out SoundLibrary.Entry entry)) return;
            if (!loops.TryGetValue(id, out AudioSource src))
            {
                src = CreateSource(true);
                loops[id] = src;
            }
            if (src.isPlaying && src.clip == clip) return;
            src.clip = clip;
            src.volume = entry.volume;
            src.Play();
        }

        private void StopLoopInternal(SoundId id)
        {
            if (loops.TryGetValue(id, out AudioSource src)) src.Stop();
        }

        private bool TryPickClip(SoundId id, out AudioClip clip, out SoundLibrary.Entry entry)
        {
            clip = null;
            entry = null;
            if (library == null || !library.TryGet(id, out entry)) return false;
            if (entry.clips == null || entry.clips.Length == 0) return false;
            clip = entry.clips[Random.Range(0, entry.clips.Length)];
            return clip != null;
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f; // 2D-звук
            return src;
        }
    }
}
