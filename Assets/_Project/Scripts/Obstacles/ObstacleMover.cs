using UnityEngine;

namespace CardThrow
{
    // Движение препятствия туда-обратно между точками A и B (уровень «Перекидка»).
    // Режим tossInAir: стакан «перекидывают» — в середине пути он «в воздухе» (масштаб 1 → 1.3 → 1),
    // коллайдер выключен, и карта пролетает ПОД ним. На концах стакан «приземляется»: коллайдер включён.
    //
    // Двигаем Rigidbody2D (Kinematic) через MovePosition в FixedUpdate — тогда CircleCast карты на каждом шаге
    // видит актуальное положение стакана.
    // Превью траектории движение НЕ предсказывает (точки считаются по текущему снимку стола) — так задумано:
    // игрок должен поймать момент, а не просто прочитать ответ с превью.
    [RequireComponent(typeof(Rigidbody2D))]
    public class ObstacleMover : MonoBehaviour
    {
        [Header("Кто передаёт (если заданы — точки A/B ниже не нужны)")]
        [SerializeField, Tooltip("Игрок, у которого стакан стоит в начале (например Seat_Opp_1)")] private Transform holderA;
        [SerializeField, Tooltip("Игрок напротив, которому её передают")] private Transform holderB;
        [SerializeField, Tooltip("Насколько стакан стоит ближе к центру стола, чем сам игрок, ед.")]
        private float holderInset = 1.3f;

        [Header("Путь вручную (смещения от позиции стакана), если игроки не заданы")]
        [SerializeField] private Vector2 pointA = new Vector2(0f, -3f);
        [SerializeField] private Vector2 pointB = new Vector2(0f, 3f);

        [Header("Темп")]
        [SerializeField, Tooltip("Скорость полёта между точками, ед/с")] private float speed = 2.5f;
        [SerializeField, Tooltip("Сколько секунд стоит на каждом конце")] private float pauseAtEnds = 0.8f;
        [SerializeField, Tooltip("Начать с середины цикла, чтобы два стакана на уровне не летали синхронно (0..1)")]
        [Range(0f, 1f)] private float phaseOffset = 0f;

        [Header("«Перекидка»")]
        [SerializeField, Tooltip("Вкл: в середине пути стакан в воздухе — карта проходит под ним.\n" +
                                 "Выкл: стакан скользит по столу — разбить его можно на всём пути")]
        private bool tossInAir = false; // по умолчанию стакан передают по столу, а не перекидывают
        [SerializeField, Tooltip("Масштаб в верхней точке полёта")] private float airScale = 1.3f;
        [SerializeField, Tooltip("Доля пути у каждого конца, где стакан ещё/уже «на столе» (коллайдер включён)")]
        [Range(0f, 0.45f)] private float groundedMargin = 0.12f;

        private Rigidbody2D rb;
        private Collider2D[] colliders;
        private BreakableObstacle breakable;
        private Vector2 origin;
        private Vector3 baseScale;
        private float clock;           // время внутри цикла
        private bool airborne;

        private float TravelTime => Mathf.Max(0.05f, Vector2.Distance(pointA, pointB) / Mathf.Max(0.01f, speed));
        private float CycleTime => 2f * (TravelTime + pauseAtEnds);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            colliders = GetComponentsInChildren<Collider2D>();
            breakable = GetComponent<BreakableObstacle>();
            origin = rb.position;
            baseScale = transform.localScale;

            // Заданы игроки — считаем точки от них: у каждого, чуть ближе к центру стола (0;0)
            if (holderA != null && holderB != null)
            {
                pointA = HolderSpot(holderA) - origin;
                pointB = HolderSpot(holderB) - origin;
            }
            clock = phaseOffset * CycleTime;
            rb.position = origin + Evaluate(clock, out _);
        }

        private void FixedUpdate()
        {
            if (breakable != null && breakable.IsBroken) return; // разбитый стакан остаётся на месте
            if (!IsGameRunning()) return;

            clock = (clock + Time.fixedDeltaTime) % CycleTime;
            Vector2 offset = Evaluate(clock, out float travel01);
            rb.MovePosition(origin + offset);

            // «Полёт»: середина пути — в воздухе. Высота — синус, 0 на концах и 1 посередине.
            // Режим «скользит по столу» — никогда не в воздухе.
            bool inAir = tossInAir && travel01 > groundedMargin && travel01 < 1f - groundedMargin;
            float height = tossInAir && travel01 > 0f && travel01 < 1f ? Mathf.Sin(travel01 * Mathf.PI) : 0f;
            transform.localScale = baseScale * Mathf.Lerp(1f, airScale, height);

            if (inAir != airborne)
            {
                airborne = inAir;
                foreach (Collider2D c in colliders) if (c != null) c.enabled = !inAir;
            }
        }

        // Позиция внутри цикла: пауза у A → полёт A→B → пауза у B → полёт B→A.
        // travel01 — доля пройденного пути в текущем полёте (0 или 1 на паузах).
        private Vector2 Evaluate(float t, out float travel01)
        {
            float travel = TravelTime;
            if (t < pauseAtEnds) { travel01 = 0f; return pointA; }
            t -= pauseAtEnds;
            if (t < travel) { travel01 = Smooth(t / travel); return Vector2.Lerp(pointA, pointB, travel01); }
            t -= travel;
            if (t < pauseAtEnds) { travel01 = 0f; return pointB; }
            t -= pauseAtEnds;
            travel01 = Smooth(Mathf.Clamp01(t / travel));
            return Vector2.Lerp(pointB, pointA, travel01);
        }

        // Плавный старт и остановка (smoothstep)
        private static float Smooth(float x) => x * x * (3f - 2f * x);

        // В игре стакан движется, только пока игрок целится или карта летит.
        // Без GameManager (песочница) — движется всегда.
        private static bool IsGameRunning()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return true;
            return gm.State == GameState.Aiming || gm.State == GameState.Flying;
        }

        // Место «у игрока»: от игрока на holderInset в сторону центра стола
        private Vector2 HolderSpot(Transform holder)
        {
            Vector2 p = holder.position;
            Vector2 toCenter = -p;
            if (toCenter.sqrMagnitude < 0.0001f) return p;
            return p + toCenter.normalized * Mathf.Min(holderInset, toCenter.magnitude);
        }

        // В редакторе: линия пути и точки A/B
        private void OnDrawGizmosSelected()
        {
            Vector2 a, b;
            if (holderA != null && holderB != null) { a = HolderSpot(holderA); b = HolderSpot(holderB); }
            else
            {
                Vector2 o = Application.isPlaying ? origin : (Vector2)transform.position;
                a = o + pointA; b = o + pointB;
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.15f);
            Gizmos.DrawWireSphere(b, 0.15f);
        }
    }
}
