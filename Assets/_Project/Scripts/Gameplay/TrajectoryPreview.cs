using UnityEngine;

namespace CardThrow
{
    // Точки траектории при прицеливании (CONTEXT, раздел 3 «Превью»). Владелец — Алексей.
    // Считает путь ТЕМ ЖЕ FlightSimulator.StepWithCollisions, что и настоящая карта, поэтому точки совпадают с полётом.
    // Показывает только часть пути (fraction) — так растёт сложность. Ничего не ломает.
    public class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private FlightSettings settings;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Sprite crossSprite;

        [Header("Вид")]
        [SerializeField] private int maxDots = 40;
        [SerializeField, Tooltip("Расстояние между точками вдоль пути")] private float dotSpacing = 0.35f;
        [SerializeField] private float dotScaleStart = 1f;
        [SerializeField] private float dotScaleEnd = 0.45f;
        [SerializeField] private float alphaEnd = 0.25f;
        [SerializeField] private Color dotColor = Color.white;
        [SerializeField] private Color dangerColor = new Color(1f, 0.25f, 0.25f, 1f); // точки внутри чужой зоны
        [SerializeField] private string sortingLayer = "FX";

        private SpriteRenderer[] dots;
        private SpriteRenderer cross;
        private Vector2[] path;          // точки пути на каждом шаге симуляции
        private float[] pathLength;      // накопленная длина до каждой точки
        private readonly Collider2D[] overlap = new Collider2D[4];
        private ContactFilter2D zoneFilter;

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("[CT] TrajectoryPreview: не задан FlightSettings (меню CardThrow → Алексей → 4)", this);
                enabled = false;
                return;
            }

            // Пул создаём один раз — потом только двигаем, без выделения памяти каждый кадр.
            dots = new SpriteRenderer[maxDots];
            for (int i = 0; i < maxDots; i++) dots[i] = CreateRenderer("Dot_" + i, dotSprite, 0);
            cross = CreateRenderer("Cross", crossSprite, 1);

            path = new Vector2[settings.maxPreviewSteps + 1];
            pathLength = new float[settings.maxPreviewSteps + 1];

            zoneFilter = new ContactFilter2D();
            zoneFilter.SetLayerMask(settings.zoneMask);
            zoneFilter.useTriggers = true;
        }

        public void Show(ThrowParams p, float fraction)
        {
            if (dots == null) return;

            // 1. Симулируем весь путь до остановки (или до удара о хрупкое / фишку).
            FlightState st = FlightSimulator.Launch(p, settings);
            float dt = Time.fixedDeltaTime;
            int count = 0;
            path[0] = st.pos;
            pathLength[0] = 0f;
            bool hitSomething = false;

            for (int i = 0; i < settings.maxPreviewSteps && !st.IsStopped; i++)
            {
                StepEvent ev = FlightSimulator.StepWithCollisions(ref st, settings, dt, null, out _, out _, out _);
                count++;
                path[count] = st.pos;
                pathLength[count] = pathLength[count - 1] + Vector2.Distance(path[count - 1], st.pos);

                if (ev == StepEvent.HitBreakable || ev == StepEvent.HitPushable)
                {
                    hitSomething = true; // обрываем: дальше путь непредсказуем
                    break;
                }
                // Ricochet — просто продолжаем: отскок виден в точках
            }

            float total = pathLength[count];
            float visible = total * Mathf.Clamp01(fraction);

            // 2. Расставляем точки через равные промежутки до видимой длины.
            int seg = 0;
            int used = 0;
            for (float d = dotSpacing; d <= visible && used < maxDots; d += dotSpacing)
            {
                while (seg < count && pathLength[seg + 1] < d) seg++;
                if (seg >= count) break;

                float segLen = pathLength[seg + 1] - pathLength[seg];
                float k = segLen > 0f ? (d - pathLength[seg]) / segLen : 0f;
                Vector2 pos = Vector2.Lerp(path[seg], path[seg + 1], k);

                float t = visible > 0f ? d / visible : 0f; // 0 у руки → 1 в конце видимой части
                SpriteRenderer dot = dots[used++];
                dot.transform.position = pos;
                dot.transform.localScale = Vector3.one * Mathf.Lerp(dotScaleStart, dotScaleEnd, t);
                Color c = IsInOpponentZone(pos) ? dangerColor : dotColor;
                c.a = Mathf.Lerp(1f, alphaEnd, t);
                dot.color = c;
                dot.enabled = true;
            }
            for (int i = used; i < maxDots; i++) dots[i].enabled = false;

            // 3. Крестик «удар» — только если место удара попало в видимую часть пути.
            bool showCross = hitSomething && total <= visible + 0.001f;
            cross.enabled = showCross;
            if (showCross) cross.transform.position = path[count];
        }

        public void Hide()
        {
            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++) dots[i].enabled = false;
            cross.enabled = false;
        }

        private bool IsInOpponentZone(Vector2 point)
        {
            int n = Physics2D.OverlapPoint(point, zoneFilter, overlap);
            for (int i = 0; i < n; i++)
            {
                Zone z = overlap[i].GetComponent<Zone>();
                if (z != null && z.Type == ZoneType.Opponent) return true;
            }
            return false;
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }
    }
}
