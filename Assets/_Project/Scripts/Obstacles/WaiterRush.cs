using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardThrow
{
    // «Час пик» (уровень 5). Владелец — Алексей.
    // Вешается на корень раздачи. Каждые interval секунд официантка выбегает из-за длинного края стола,
    // подбегает к 1–2 стаканам и переставляет их в случайную точку полосы вдоль длинного края:
    // |y| ∈ [yMin; yMax], x ∈ [xMin; xMax], не ближе minDistance к зоне напарника и другим предметам.
    // Пока игрок не целится или карта летит — официантки не выходят. Если игрок бросил карту,
    // пока официантка несёт стакан, стакан сразу «ставится» на место: полёт карты честный.
    public class WaiterRush : MonoBehaviour
    {
        [Header("Ритм")]
        [SerializeField] private float interval = 7f; // пауза между уходом официантки и следующим выходом (плейтест 12.10)
        [SerializeField, Min(1)] private int minPerWave = 1;
        [SerializeField, Min(1), Tooltip("Последний уровень: 1 официантка за раз. Сложнее — поставить 2")] private int maxPerWave = 1;

        [Header("Куда ставить стакан")]
        [SerializeField] private float xMin = -5f;
        [SerializeField] private float xMax = 5f;
        [SerializeField, Tooltip("Полоса вдоль длинного края: |y| от yMin до yMax")] private float yMin = 2f;
        [SerializeField] private float yMax = 3.2f;
        [SerializeField, Tooltip("Не ближе этого к зоне напарника и к другим предметам")] private float minDistance = 1.5f;

        [Header("Официантка")]
        [SerializeField, Tooltip("Спрайт официантки (пока — поднос; заменить, когда Кирилл пришлёт)")]
        private Sprite waiterSprite;
        [SerializeField] private float waiterScale = 1f;
        [SerializeField, Tooltip("За краем стола, откуда выбегает официантка (|y|)")] private float offTableY = 5.6f;
        [SerializeField, Tooltip("Скорость шага официантки, ед/с (одинаковая на любом расстоянии)")] private float walkSpeed = 2.5f;
        [SerializeField] private string sortingLayer = "FX";

        private readonly List<Transform> glasses = new List<Transform>();
        private readonly List<Transform> others = new List<Transform>();
        private Zone partnerZone;
        private bool cardFlying;
        private readonly List<Coroutine> running = new List<Coroutine>();
        private readonly List<(Transform glass, Vector2 target, GameObject waiter)> inProgress =
            new List<(Transform, Vector2, GameObject)>();

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

        // Стаканы и прочие предметы — внутри этой раздачи. Если их нет в детях (песочница) — ищем по сцене.
        private void CollectObjects()
        {
            RicochetObstacle[] g = GetComponentsInChildren<RicochetObstacle>();
            if (g.Length == 0) g = FindInScene<RicochetObstacle>();
            foreach (RicochetObstacle r in g) glasses.Add(r.transform);

            Obstacle[] all = GetComponentsInChildren<Obstacle>();
            if (all.Length == 0) all = FindInScene<Obstacle>();
            foreach (Obstacle o in all) if (!(o is RicochetObstacle)) others.Add(o.transform);

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

        // Случайная точка полосы, далеко от зоны напарника и других предметов (включая другие стаканы)
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
            return false; // места нет — этот стакан в этот раз не трогаем
        }

        private bool TooClose(Vector2 spot, List<Transform> list, Transform skip)
        {
            foreach (Transform t in list)
                if (t != null && t != skip && Vector2.Distance(spot, t.position) < minDistance) return true;
            return false;
        }

        // Официантка: из-за края → к стакану → несёт его к новой точке → убегает за край
        private IEnumerator Move(Transform glass, Vector2 target)
        {
            Vector2 from = glass.position;
            float side = Mathf.Sign(target.y == 0f ? 1f : target.y);
            Vector2 enter = new Vector2(from.x, side * offTableY);
            Vector2 exit = new Vector2(target.x, side * offTableY);

            GameObject waiter = CreateWaiter(enter);
            var job = (glass, target, waiter);
            inProgress.Add(job);

            yield return Run(waiter.transform, enter, from, LegTime(enter, from), null);
            yield return Run(waiter.transform, from, target, LegTime(from, target), glass);   // несёт стакан
            yield return Run(waiter.transform, target, exit, LegTime(target, exit), null);

            inProgress.Remove(job);
            if (waiter != null) Destroy(waiter);
        }

        // Время участка по длине пути: официантка идёт, а не телепортируется
        private float LegTime(Vector2 a, Vector2 b) => Vector2.Distance(a, b) / Mathf.Max(0.1f, walkSpeed);

        private IEnumerator Run(Transform who, Vector2 a, Vector2 b, float time, Transform carried)
        {
            float t = 0f;
            while (t < time)
            {
                if (who == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / time));
                Vector2 p = Vector2.Lerp(a, b, k);
                who.position = p;
                if (carried != null) carried.position = new Vector3(p.x, p.y, carried.position.z);
                yield return null;
            }
            if (carried != null) carried.position = new Vector3(b.x, b.y, carried.position.z);
            Physics2D.SyncTransforms(); // коллайдер стакана сразу на новом месте — для превью и полёта
        }

        private GameObject CreateWaiter(Vector2 at)
        {
            GameObject go = new GameObject("Waiter");
            go.transform.SetParent(transform, false); // внутри раздачи: удалится вместе с ней при перезапуске
            go.transform.position = at;
            go.transform.localScale = Vector3.one * waiterScale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = waiterSprite;
            sr.sortingLayerName = sortingLayer;
            return go;
        }

        // Бросок во время перестановки: всё «доигрываем» мгновенно — стаканы встают на места, официантки исчезают
        private void OnCardThrown(ThrowParams p)
        {
            cardFlying = true;
            foreach (Coroutine c in running) if (c != null) StopCoroutine(c);
            running.Clear();
            foreach (var job in inProgress)
            {
                if (job.glass != null) job.glass.position = new Vector3(job.target.x, job.target.y, job.glass.position.z);
                if (job.waiter != null) Destroy(job.waiter);
            }
            inProgress.Clear();
            Physics2D.SyncTransforms();
        }

        private void OnFlightFinished(FlightReport r) => cardFlying = false;
    }
}
