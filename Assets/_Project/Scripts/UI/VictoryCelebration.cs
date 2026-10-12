using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow
{
    // Праздник на экране победы (на объекте VictoryScreen рядом с EndScreen): набегающий «джекпот смены»
    // и фейерверк из UI-частиц по бокам от панели, у каждого залпа — хлопок.
    // Всё на unscaled-времени (на финише может идти slow-mo); частицы берутся из пула.
    public class VictoryCelebration : MonoBehaviour
    {
        [Header("Джекпот")]
        [SerializeField] private TMP_Text jackpotLabel;
        [SerializeField] private long jackpotAmount = 67000000;
        [SerializeField] private string jackpotFormat = "{0} $";
        [SerializeField] private float countUpTime = 1.8f;

        [Header("Фейерверк")]
        [Tooltip("Области по бокам от панели, внутри них случайная точка залпа")]
        [SerializeField] private RectTransform leftZone;
        [SerializeField] private RectTransform rightZone;
        [SerializeField] private Sprite[] particleSprites;
        [SerializeField] private int particlesPerBurst = 26;
        [SerializeField] private float burstSpeed = 520f;      // px/с
        [SerializeField] private float gravity = 420f;         // px/с²
        [SerializeField] private float particleLife = 1.3f;
        [SerializeField] private float firstBurstsInterval = 0.35f;
        [SerializeField] private float calmInterval = 1.1f;
        [SerializeField] private int firstBursts = 8;

        [Header("Звук")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip popClip;
        [SerializeField, Range(0f, 1f)] private float popVolume = 0.7f;

        [SerializeField] private float startDelay = 0.25f;

        private static readonly Color[] Colors =
        {
            new Color(1f, 0.84f, 0.3f), new Color(1f, 0.35f, 0.3f), new Color(0.4f, 0.85f, 1f),
            new Color(0.55f, 1f, 0.5f), new Color(1f, 0.5f, 0.9f), Color.white
        };

        private class Particle { public RectTransform rt; public Image img; public Vector2 vel; public float age, life; public Color color; }
        private readonly List<Particle> pool = new List<Particle>();

        private void OnEnable() => GameEvents.StateChanged += OnStateChanged;
        private void OnDisable() { GameEvents.StateChanged -= OnStateChanged; Stop(); }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Victory) { Stop(); StartCoroutine(Celebrate()); }
            else Stop();
        }

        private void Stop()
        {
            StopAllCoroutines(); // и залпы, и счётчик джекпота
            foreach (Particle p in pool) { p.age = p.life; p.img.enabled = false; }
        }

        private IEnumerator Celebrate()
        {
            if (jackpotLabel != null) jackpotLabel.text = Format(0);
            yield return new WaitForSecondsRealtime(startDelay);
            StartCoroutine(CountUp());
            for (int i = 0; ; i++)
            {
                Burst(i % 2 == 0 ? leftZone : rightZone);
                yield return new WaitForSecondsRealtime(i < firstBursts ? firstBurstsInterval : calmInterval);
            }
        }

        private IEnumerator CountUp()
        {
            if (jackpotLabel == null) yield break;
            float t = 0f;
            while (t < countUpTime)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / countUpTime), 3f);
                jackpotLabel.text = Format((long)(jackpotAmount * k));
                yield return null;
            }
            jackpotLabel.text = Format(jackpotAmount);
        }

        private string Format(long v) =>
            string.Format(jackpotFormat, v.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " "));

        private void Burst(RectTransform zone)
        {
            if (zone == null || particleSprites == null || particleSprites.Length == 0) return;
            Vector2 size = zone.rect.size;
            Vector2 origin = new Vector2(Random.Range(-0.35f, 0.35f) * size.x, Random.Range(-0.1f, 0.35f) * size.y);
            Color baseColor = Colors[Random.Range(0, Colors.Length)];
            for (int i = 0; i < particlesPerBurst; i++)
            {
                Particle p = Get(zone);
                float ang = (i / (float)particlesPerBurst) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                float sp = burstSpeed * Random.Range(0.55f, 1f);
                p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp;
                p.rt.anchoredPosition = origin;
                p.age = 0f;
                p.life = particleLife * Random.Range(0.8f, 1.2f);
                p.color = Random.value < 0.7f ? baseColor : Colors[Random.Range(0, Colors.Length)];
                p.img.sprite = particleSprites[Random.Range(0, particleSprites.Length)];
                p.rt.localScale = Vector3.one * Random.Range(0.6f, 1.1f);
                p.img.enabled = true;
            }
            if (audioSource != null && popClip != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.15f);
                audioSource.PlayOneShot(popClip, popVolume);
            }
        }

        private Particle Get(RectTransform parent)
        {
            foreach (Particle p in pool)
                if (p.age >= p.life && p.rt.parent == parent) return p;
            GameObject go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Particle np = new Particle { rt = (RectTransform)go.transform, img = go.GetComponent<Image>() };
            np.rt.sizeDelta = new Vector2(34f, 34f);
            np.img.raycastTarget = false;
            pool.Add(np);
            return np;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (Particle p in pool)
            {
                if (p.age >= p.life) continue;
                p.age += dt;
                p.vel.y -= gravity * dt;
                p.vel *= 1f - 1.2f * dt; // сопротивление воздуха
                p.rt.anchoredPosition += p.vel * dt;
                float k = p.age / p.life;
                Color c = p.color; c.a = 1f - k * k;
                p.img.color = c;
                if (p.age >= p.life) p.img.enabled = false;
            }
        }
    }
}
