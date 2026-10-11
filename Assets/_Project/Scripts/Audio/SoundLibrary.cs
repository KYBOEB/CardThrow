using System.Collections.Generic;
using UnityEngine;

namespace CardThrow
{
    // Таблица «SoundId → клипы». Наполняет Артас в ассете Data/SoundLibrary.asset.
    [CreateAssetMenu(menuName = "CardThrow/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public SoundId id;
            public AudioClip[] clips;                       // несколько вариантов — играется случайный
            [Range(0f, 1f)] public float volume = 0.8f;
            [Range(0f, 0.3f)] public float pitchJitter = 0.05f;
        }

        public List<Entry> entries = new List<Entry>();

        private Dictionary<SoundId, Entry> map;

        public bool TryGet(SoundId id, out Entry entry)
        {
            if (map == null) BuildMap();
            return map.TryGetValue(id, out entry);
        }

        private void BuildMap()
        {
            map = new Dictionary<SoundId, Entry>();
            foreach (Entry e in entries)
                if (e != null) map[e.id] = e;
        }

        private void OnValidate() { map = null; }
    }
}
