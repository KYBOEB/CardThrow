using System.Collections;
using UnityEngine;

namespace CardThrow
{
    // Хрупкий предмет: стакан, карточный домик, поднос.
    // Ломается от карты (OnCardHit) или от сильного удара фишки (цепная реакция).
    // При поломке: спрайт «разбито», коллайдеры выключаются, звук, короткий «пинок» масштабом
    // и разлёт обломков (осколки у стакана, карты у домика) — обломки чисто визуальные, без физики.
    public class BreakableObstacle : Obstacle
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite brokenSprite;
        [SerializeField] private SoundId breakSound = SoundId.BottleBreak;
        [SerializeField, Tooltip("С какой скорости удар фишки разбивает предмет")]
        private float chainBreakSpeed = 2.5f;

        [Header("Обломки (необязательно)")]
        [SerializeField, Tooltip("Спрайт обломка: fx_shard для стакана, card_back для домика. Пусто — без обломков")]
        private Sprite debrisSprite;
        [SerializeField] private int debrisCount = 5;
        [SerializeField] private float debrisScale = 0.6f;
        [SerializeField, Tooltip("Скорость разлёта, ед/с")] private float debrisSpeed = 3f;
        [SerializeField, Tooltip("Сколько секунд обломки летят и тают")] private float debrisLife = 0.6f;

        [Header("Пинок")]
        [SerializeField] private float punchScale = 1.15f;
        [SerializeField] private float punchTime = 0.15f;

        public override ObstacleKind Kind => ObstacleKind.Breakable;
        public bool IsBroken { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Удар карты. Событие CardHitObstacle поднимает сама карта (CardFlight).
        public override void OnCardHit(Vector2 point, Vector2 cardVelocity)
        {
            Break(cardVelocity);
        }

        // Удар фишки (у фишек есть Rigidbody2D, поэтому Unity сообщает о столкновении).
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsBroken || collision.relativeVelocity.magnitude < chainBreakSpeed) return;

            Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            Break(-collision.relativeVelocity);
            // Цепная реакция: сообщаем всем — летящая карта засчитает провал.
            GameEvents.RaiseCardHitObstacle(point, ObstacleKind.Breakable);
        }

        private void Break(Vector2 impactVelocity)
        {
            if (IsBroken) return;
            IsBroken = true;

            if (spriteRenderer != null && brokenSprite != null) spriteRenderer.sprite = brokenSprite;
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            AudioManager.Play(breakSound);

            StartCoroutine(Punch());
            if (debrisSprite != null && debrisCount > 0) StartCoroutine(Debris(impactVelocity));
        }

        // Короткое «вздрагивание»: 1 → punchScale → 1
        private IEnumerator Punch()
        {
            Vector3 baseScale = transform.localScale;
            float t = 0f;
            while (t < punchTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / punchTime) * Mathf.PI); // 0 → 1 → 0
                transform.localScale = baseScale * Mathf.Lerp(1f, punchScale, k);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        // Обломки разлетаются веером по направлению удара, крутятся и тают
        private IEnumerator Debris(Vector2 impactVelocity)
        {
            Vector2 forward = impactVelocity.sqrMagnitude > 0.01f ? impactVelocity.normalized : Random.insideUnitCircle.normalized;
            var pieces = new SpriteRenderer[debrisCount];
            var velocities = new Vector2[debrisCount];
            var spins = new float[debrisCount];

            for (int i = 0; i < debrisCount; i++)
            {
                GameObject go = new GameObject("Debris");
                // Обломки — внутри раздачи: при перезапуске уровня они удалятся вместе с ней
                go.transform.SetParent(transform.parent, false);
                go.transform.position = transform.position;
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                go.transform.localScale = Vector3.one * debrisScale;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = debrisSprite;
                sr.sortingLayerName = "FX";
                pieces[i] = sr;
                // половина импульса — по удару, половина — во все стороны
                Vector2 dir = (forward + Random.insideUnitCircle * 1.2f).normalized;
                velocities[i] = dir * debrisSpeed * Random.Range(0.6f, 1.2f);
                spins[i] = Random.Range(-540f, 540f);
            }

            float t = 0f;
            while (t < debrisLife)
            {
                float dt = Time.deltaTime;
                t += dt;
                float k = Mathf.Clamp01(t / debrisLife);
                for (int i = 0; i < debrisCount; i++)
                {
                    if (pieces[i] == null) continue;
                    Transform tr = pieces[i].transform;
                    tr.position += (Vector3)(velocities[i] * dt);
                    tr.Rotate(0f, 0f, spins[i] * dt);
                    velocities[i] *= 1f - 3f * dt; // трение о сукно
                    pieces[i].color = new Color(1f, 1f, 1f, 1f - k);
                }
                yield return null;
            }

            foreach (SpriteRenderer sr in pieces) if (sr != null) Destroy(sr.gameObject);
        }
    }
}
