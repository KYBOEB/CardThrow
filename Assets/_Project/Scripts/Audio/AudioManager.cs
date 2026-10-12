using System.Collections.Generic;
using UnityEngine;

namespace CardThrow
{
    // Простой звуковой менеджер: пул источников для коротких звуков + отдельные источники для лупов.
    // Вызов из любого места: AudioManager.Play(SoundId.CardThrow);
    // Громкость музыки (0…1) — ползунок в меню паузы, хранится в PlayerPrefs. Музыка играет и на паузе
    // (ignoreListenerPause), чтобы громкость было слышно, пока её крутишь; остальные звуки на паузе молчат.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SoundLibrary library;
        [SerializeField, Min(1)] private int sfxVoices = 12;

        private AudioSource[] voices;
        private int nextVoice;
        private readonly Dictionary<SoundId, AudioSource> loops = new Dictionary<SoundId, AudioSource>();
        private readonly Dictionary<SoundId, float> loopBaseVolume = new Dictionary<SoundId, float>();

        private const string MusicVolumeKey = "CT_MusicVolume";
        private static float musicVolume = -1f; // −1 — ещё не прочитали из PlayerPrefs

        public static float MusicVolume
        {
            get
            {
                if (musicVolume < 0f) musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
                return musicVolume;
            }
        }

        public static void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            if (Instance != null) Instance.ApplyMusicVolume();
        }

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
            loopBaseVolume[id] = entry.volume;
            src.ignoreListenerPause = id == SoundId.MusicLoop;
            src.volume = entry.volume * (id == SoundId.MusicLoop ? MusicVolume : 1f);
            src.Play();
        }

        private void ApplyMusicVolume()
        {
            if (loops.TryGetValue(SoundId.MusicLoop, out AudioSource src) && loopBaseVolume.TryGetValue(SoundId.MusicLoop, out float baseVolume))
                src.volume = baseVolume * MusicVolume;
        }

        // PlayerPrefs.Save на выходе, а не на каждый сдвиг ползунка (запись на диск дорогая)
        private void OnApplicationQuit() => PlayerPrefs.Save();

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
