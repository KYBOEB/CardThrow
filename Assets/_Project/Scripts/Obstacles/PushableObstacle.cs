using UnityEngine;

namespace CardThrow
{
    // Стопка фишек.
    // До удара стопка — один неподвижный круглый коллайдер на корне. От удара карты фишки-дети «оживают»:
    // включаются их Rigidbody2D и коллайдеры, каждая получает импульс по направлению полёта карты с разбросом.
    // Разлетевшаяся фишка, врезавшись в стакан или домик, разбивает их (BreakableObstacle.OnCollisionEnter2D) —
    // это цепная реакция. Карта при ударе теряет pushSpeedLoss скорости (делает CardFlight).
    public class PushableObstacle : Obstacle
    {
        [SerializeField, Tooltip("Коллайдер целой стопки (на корне). После разлёта выключается")]
        private Collider2D stackCollider;
        [SerializeField, Tooltip("Фишки стопки: у каждой Rigidbody2D (Dynamic, gravity 0) и свой коллайдер")]
        private Rigidbody2D[] chips;

        [Header("Разлёт")]
        [SerializeField, Tooltip("Доля скорости карты, которую получает фишка")] private float pushFactor = 0.6f;
        [SerializeField, Tooltip("Разброс направления, градусы")] private float spreadAngle = 35f;
        [SerializeField, Tooltip("Случайный разброс силы, ±доля")] private float speedJitter = 0.3f;
        [SerializeField] private float maxSpin = 720f;
        [SerializeField] private SoundId scatterSound = SoundId.ChipsScatter;

        public override ObstacleKind Kind => ObstacleKind.Pushable;
        public bool IsScattered { get; private set; }

        private void Awake()
        {
            if (stackCollider == null) stackCollider = GetComponent<Collider2D>();
            // До удара фишки «спят»: не участвуют в физике, иначе наложенные друг на друга тела сразу разлетятся
            SetChipsActive(false);
        }

        public override void OnCardHit(Vector2 point, Vector2 cardVelocity)
        {
            AudioManager.Play(scatterSound);

            if (!IsScattered)
            {
                IsScattered = true;
                if (stackCollider != null) stackCollider.enabled = false;
                SetChipsActive(true);
                foreach (Rigidbody2D rb in chips) Push(rb, cardVelocity);
            }
            else
            {
                // Карта задела уже разлетевшиеся фишки — толкаем ближайшую к точке удара
                Rigidbody2D nearest = null;
                float best = float.MaxValue;
                foreach (Rigidbody2D rb in chips)
                {
                    if (rb == null) continue;
                    float d = (rb.position - point).sqrMagnitude;
                    if (d < best) { best = d; nearest = rb; }
                }
                if (nearest != null) Push(nearest, cardVelocity);
            }
        }

        // Импульс = масса × желаемая скорость: так результат не зависит от массы фишки в инспекторе
        private void Push(Rigidbody2D rb, Vector2 cardVelocity)
        {
            if (rb == null) return;
            float angle = Random.Range(-spreadAngle, spreadAngle) * Mathf.Deg2Rad;
            Vector2 dir = FlightSimulator.Rotate(cardVelocity.sqrMagnitude > 0f ? cardVelocity.normalized : Vector2.right, angle);
            float speed = cardVelocity.magnitude * pushFactor * (1f + Random.Range(-speedJitter, speedJitter));
            rb.AddForce(dir * (rb.mass * speed), ForceMode2D.Impulse);
            rb.angularVelocity = Random.Range(-maxSpin, maxSpin); // градусы в секунду — просто для красоты
        }

        private void SetChipsActive(bool active)
        {
            if (chips == null) return;
            foreach (Rigidbody2D rb in chips)
            {
                if (rb == null) continue;
                rb.simulated = active;
                Collider2D c = rb.GetComponent<Collider2D>();
                if (c != null) c.enabled = active;
            }
        }
    }
}
