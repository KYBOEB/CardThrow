using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardThrow
{
    // «Час пик»: официантки переставляют пепельницы между бросками. Вешается на корень раздачи.
    // Каждые interval секунд официантка подходит к длинному краю стола (за бортик, между сидящими игроками),
    // и 1–2 пепельницы «скользят» от неё в случайные точки полосы |y| ∈ [yMin; yMax], x ∈ [xMin; xMax],
    // не ближе minDistance к зоне напарника и другим предметам. Официантки выходят, только пока игрок целится.
    // Коллайдер переносится сразу, едет только картинка (дочерний Visual) — превью и полёт честные.
    // Если карту бросили во время перестановки, она мгновенно завершается.
    public class WaiterRush : MonoBehaviour
    {
        [Header("Ритм")]
        [SerializeField] private float interval = 7f; // пауза между уходом официантки и следующим выходом
        [SerializeField, Min(1)] private int minPerWave = 1;
        [SerializeField, Min(1), Tooltip("Последний уровень: 1 официантка за раз. Сложнее — поставить 2")] private int maxPerWave = 1;

        [Header("Куда ставить пепельницу")]
        [SerializeField] private float xMin = -5f;
        [SerializeField] private float xMax = 5f;
        [SerializeField, Tooltip("Полоса вдоль длинного края: |y| от yMin до yMax")] private float yMin = 2f;
        [SerializeField] private float yMax = 3.2f;
        [SerializeField, Tooltip("Не ближе этого к зоне напарника и к другим предметам")] private float minDistance = 1.5f;

        [Header("Официантка")]
        [SerializeField, Tooltip("Спрайт официантки")]
        private Sprite waiterSprite;
        [SerializeField] private float waiterScale = 1f;
        [SerializeField, Tooltip("Скорость шага официантки, ед/с (одинаковая на любом расстоянии)")] private float walkSpeed = 2.5f;
        [SerializeField] private string sortingLayer = "FX";

        [Header("Подача с края стола")]
        [SerializeField, Tooltip("Где стоит официантка, |y|: бортик ≈ 5.15 + полкорпуса официантки ≈ 1")] private float standY = 6.2f;
        [SerializeField, Tooltip("Не ближе этого по X к сидящему игроку на той же стороне — не перекрывать соперников")] private float seatClearance = 2.3f;
        [SerializeField] private float standXMax = 8.5f;
        [SerializeField, Tooltip("Сколько пепельница скользит от официантки до новой точки, с")] private float tossTime = 0.45f;

        private readonly List<Transform> glasses = new List<Transform>();
        private readonly List<Transform> others = new List<Transform>();
        private Zone partnerZone;
        private bool cardFlying;
        private readonly List<Coroutine> running = new List<Coroutine>();
        private readonly List<Transform> seats = new List<Transform>();

        private class Job
        {
            public Transform glass, visual;
            public Vector3 visualLocal;
            public Vector2 target, stand;
            public GameObject waiter;
        }
        private readonly List<Job> inProgress = new List<Job>();

        private void OnEnable()
        {
            GameEvents.CardThrown += OnCardThrown;
            GameEvents.CardFlightFinished += OnFlightFinished;
        }

        private void OnDisable()
        {
            GameEvents.CardThrown -= OnCardThrown;
            GameEvents.CardFlightFinished -= OnFlightFinished;
        }

        private void Start()
        {
            CollectObjects();
            StartCoroutine(Loop());
        }

        // Пепельницы и прочие предметы — внутри этой раздачи. Если их нет в детях (песочница) — ищем по сцене.
        private void CollectObjects()
        {
            RicochetObstacle[] g = GetComponentsInChildren<RicochetObstacle>();
            if (g.Length == 0) g = FindInScene<RicochetObstacle>();
            foreach (RicochetObstacle r in g) glasses.Add(r.transform);

            Obstacle[] all = GetComponentsInChildren<Obstacle>();
            if (all.Length == 0) all = FindInScene<Obstacle>();
            foreach (Obstacle o in all) if (!(o is RicochetObstacle)) others.Add(o.transform);

            foreach (Seat st in GetComponentsInChildren<Seat>()) seats.Add(st.transform);

            Zone[] zones = GetComponentsInChildren<Zone>();
            if (zones.Length == 0) zones = FindInScene<Zone>();
            foreach (Zone z in zones) if (z.Type == ZoneType.Partner) { partnerZone = z; break; }

            if (glasses.Count == 0) Debug.LogWarning("[CT] WaiterRush: на уровне нет стаканов (Glass) — официанткам нечего переставлять", this);
        }

        // Все компоненты T в сцене этого объекта (обходим корневые объекты сцены).
        // Без устаревшего в Unity 6.6 FindObjectsByType(FindObjectsSortMode).
        private T[] FindInScene<T>() where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>());
            return result.ToArray();
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                // Ждём, пока игрок целится и карта не летит
                while (!CanRush()) yield return null;

                int count = Mathf.Min(glasses.Count, Random.Range(minPerWave, maxPerWave + 1));
                List<Transform> pool = new List<Transform>(glasses);
                for (int i = 0; i < count; i++)
                {
                    int k = Random.Range(0, pool.Count);
                    Transform glass = pool[k];
                    pool.RemoveAt(k);
                    if (glass == null) continue;
                    if (TryFindSpot(glass, out Vector2 spot)) running.Add(StartCoroutine(Move(glass, spot)));
                }

                // Пауза interval считается с момента, когда официантка УШЛА, а не когда вышла
                while (inProgress.Count > 0) yield return null;
                running.Clear();
            }
        }

        private bool CanRush()
        {
            if (cardFlying) return false;
            GameManager gm = GameManager.Instance;
            return gm == null || gm.State == GameState.Aiming; // без GameManager (песочница) — всегда
        }

        // Случайная точка полосы, далеко от зоны напарника и других предметов (включая другие пепельницы)
        private bool TryFindSpot(Transform glass, out Vector2 spot)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float y = Random.Range(yMin, yMax) * (Random.value < 0.5f ? -1f : 1f);
                spot = new Vector2(Random.Range(xMin, xMax), y);
                if (partnerZone != null && Vector2.Distance(spot, partnerZone.Center) < minDistance) continue;
                if (TooClose(spot, others, null) || TooClose(spot, glasses, glass)) continue;
                return true;
            }
            spot = glass.position;
            return false; // места нет — эту пепельницу в этот раз не трогаем
        }

        private bool TooClose(Vector2 spot, List<Transform> list, Transform skip)
        {
            foreach (Transform t in list)
                if (t != null && t != skip && Vector2.Distance(spot, t.position) < minDistance) return true;
            return false;
        }

        // Официантка: из-за экрана → к краю стола (за бортик) → пепельница скользит от неё к новой точке → уходит
        private IEnumerator Move(Transform glass, Vector2 target)
        {
            float side = Mathf.Sign(target.y == 0f ? 1f : target.y);
            Vector2 stand = new Vector2(FindStandX(target.x, side), side * standY);
            Vector2 enter = new Vector2(stand.x, side * (standY + 3f));

            var job = new Job { glass = glass, target = target, stand = stand };
            job.visual = glass.Find("Visual");
            if (job.visual != null) job.visualLocal = job.visual.localPosition;
            job.waiter = CreateWaiter(enter, side);
            inProgress.Add(job);

            yield return Run(job.waiter.transform, enter, stand, LegTime(enter, stand));
            yield return Toss(job);
            yield return new WaitForSeconds(0.2f);
            yield return Run(job.waiter.transform, stand, enter, LegTime(stand, enter));

            inProgress.Remove(job);
            if (job.waiter != null) Destroy(job.waiter);
        }

        // Коллайдер — сразу в новой точке (превью и полёт честные), картинка догоняет от рук официантки
        private IEnumerator Toss(Job job)
        {
            Transform glass = job.glass;
            if (glass == null) yield break;
            glass.position = new Vector3(job.target.x, job.target.y, glass.position.z);
            Physics2D.SyncTransforms();
            if (job.visual == null) yield break;

            Vector3 from = new Vector3(job.stand.x, job.stand.y, 0f) - new Vector3(job.target.x, job.target.y, 0f);
            float t = 0f;
            while (t < tossTime)
            {
                if (job.visual == null) yield break;
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / tossTime), 2f); // быстро вылетает, мягко встаёт
                Vector3 p = glass.position + from * (1f - k);
                job.visual.position = new Vector3(p.x, p.y, job.visual.position.z);
                yield return null;
            }
            job.visual.localPosition = job.visualLocal;
        }

        // X у края стола ближе к цели, но не напротив сидящего игрока и не рядом с другой официанткой
        private float FindStandX(float x, float side)
        {
            float bestX = Mathf.Clamp(x, -standXMax, standXMax);
            for (int i = 0; i <= 40; i++)
            {
                float d = (i + 1) / 2 * 0.4f * (i % 2 == 0 ? 1f : -1f);
                float c = Mathf.Clamp(x + d, -standXMax, standXMax);
                if (IsClear(c, side)) return c;
            }
            return bestX; // свободного места нет — встанет напротив цели (редко: стол забит игроками)
        }

        private bool IsClear(float x, float side)
        {
            foreach (Transform s in seats)
            {
                if (s == null) continue;
                Vector2 p = s.position;
                if (Mathf.Abs(p.y) > 2f && Mathf.Sign(p.y) == side && Mathf.Abs(p.x - x) < seatClearance) return false;
            }
            foreach (Job j in inProgress)
                if (Mathf.Sign(j.stand.y) == side && Mathf.Abs(j.stand.x - x) < seatClearance) return false;
            return true;
        }

        // Время участка по длине пути: официантка идёт, а не телепортируется
        private float LegTime(Vector2 a, Vector2 b) => Vector2.Distance(a, b) / Mathf.Max(0.1f, walkSpeed);

        private IEnumerator Run(Transform who, Vector2 a, Vector2 b, float time)
        {
            float t = 0f;
            while (t < time)
            {
                if (who == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / time));
                Vector2 p = Vector2.Lerp(a, b, k);
                who.position = p;
                yield return null;
            }
        }

        private GameObject CreateWaiter(Vector2 at, float side)
        {
            GameObject go = new GameObject("Waiter");
            go.transform.SetParent(transform, false); // внутри раздачи: удалится вместе с ней при перезапуске
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, 0f, side > 0f ? 180f : 0f); // спрайт нарисован «вверх» — лицом к столу
            go.transform.localScale = Vector3.one * waiterScale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = waiterSprite;
            sr.sortingLayerName = sortingLayer;
            return go;
        }

        // Бросок во время перестановки: всё «доигрываем» мгновенно — пепельницы встают на места, официантки исчезают
        private void OnCardThrown(ThrowParams p)
        {
            cardFlying = true;
            foreach (Coroutine c in running) if (c != null) StopCoroutine(c);
            running.Clear();
            foreach (Job job in inProgress)
            {
                if (job.glass != null) job.glass.position = new Vector3(job.target.x, job.target.y, job.glass.position.z);
                if (job.visual != null) job.visual.localPosition = job.visualLocal;
                if (job.waiter != null) Destroy(job.waiter);
            }
            inProgress.Clear();
            Physics2D.SyncTransforms();
        }

        private void OnFlightFinished(FlightReport r) => cardFlying = false;
    }
}
