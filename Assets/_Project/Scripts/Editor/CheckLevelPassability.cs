using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню CardThrow → «Уровни: проверить проходимость».
    // Перебирает броски (угол × сила × дуга) на той же модели полёта, что и игра: FlightSimulator.StepWithCollisions,
    // те же коллайдеры и правила исхода, что в CardFlight. Сцену и префабы не изменяет.
    // Проверяет выделенные в Project префабы раздач или, если ничего не выделено, все из Prefabs/Levels.
    // Экземпляр раздачи временно ставится в открытую сцену Main (нужен её TableSurface) и затем удаляется.
    // Ограничение: движущиеся предметы (ObstacleMover) считаются неподвижными.
    public static class CheckLevelPassability
    {
        private const string LevelsDir = "Assets/_Project/Prefabs/Levels";
        private const string SettingsPath = "Assets/_Project/Data/FlightSettings.asset";

        // Сетка перебора: 114 углов × 29 сил × 7 шагов дуги ≈ 23 тыс. бросков на раздачу (несколько секунд)
        private const float AngleRange = 85f;      // ± градусов от направления «вдоль стола» (+X)
        private const float AngleStep = 1.5f;
        private const float PowerMin = 0.3f;
        private const float PowerStep = 0.025f;

        // Те же значения, что в CardFlight (страховка по времени, игнор задетой фишки, допуск зоны напарника)
        private const float MaxFlightTime = 12f;
        private const float PushIgnoreTime = 0.2f;
        private const float DefaultGrace = 0.3f;
        private static readonly Vector2 DefaultOrigin = new Vector2(-7.6f, 0f); // запасная точка броска крупье

        private enum Outcome { Success, Breakable, Opponent, OffTable, Stopped }

        private struct Sample { public float angle, power; public int spin, ricochets; }

        private static readonly Collider2D[] buffer = new Collider2D[8];

        [MenuItem("CardThrow/Уровни: проверить проходимость")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            FlightSettings settings = AssetDatabase.LoadAssetAtPath<FlightSettings>(SettingsPath);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "Не найден " + SettingsPath, "OK");
                return;
            }

            List<GameObject> hands = CollectHands();
            if (hands.Count == 0)
            {
                EditorUtility.DisplayDialog("CardThrow", "Не найдено ни одной раздачи (префаб с LevelConfig) в " + LevelsDir, "OK");
                return;
            }

            ContactFilter2D zoneFilter = TriggerFilter(settings.zoneMask);
            ContactFilter2D tableFilter = TriggerFilter(settings.tableMask);
            Vector2 origin = FindOrigin();
            float grace = FindGrace();

            Physics2D.SyncTransforms();
            bool hasTable = settings.tableMask.value != 0 && Physics2D.OverlapPoint(Vector2.zero, tableFilter, buffer) > 0;
            if (!hasTable)
                Debug.LogWarning("[CT] Проходимость: в открытой сцене нет TableSurface — вылет со стола не проверяется. Открой Scenes/Main.unity.");

            StringBuilder log = new StringBuilder("[CT] Проходимость раздач (точка броска " + origin + ", допуск зоны " + grace + ")\n");
            StringBuilder summary = new StringBuilder();

            try
            {
                for (int h = 0; h < hands.Count; h++)
                {
                    GameObject prefab = hands[h];
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.position = Vector3.zero;
                    try
                    {
                        Physics2D.SyncTransforms(); // в режиме редактирования коллайдеры нового объекта иначе «не видны» запросам
                        if (!Test(prefab.name, h, hands.Count, settings, origin, grace, zoneFilter, tableFilter, hasTable,
                                  out string line, out string shortLine))
                        {
                            log.AppendLine("Прервано пользователем.");
                            break;
                        }
                        log.AppendLine(line);
                        summary.AppendLine(shortLine);
                    }
                    finally
                    {
                        Object.DestroyImmediate(instance);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Physics2D.SyncTransforms();
            }

            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("CardThrow — проходимость", summary.Length > 0 ? summary.ToString() : "Нет результатов", "OK");
        }

        // ---------- Перебор ----------

        private static bool Test(string name, int handIndex, int handCount, FlightSettings s, Vector2 origin, float grace,
            ContactFilter2D zoneFilter, ContactFilter2D tableFilter, bool hasTable, out string line, out string shortLine)
        {
            line = shortLine = "";
            int angles = Mathf.RoundToInt(2f * AngleRange / AngleStep) + 1;
            int powers = Mathf.RoundToInt((1f - PowerMin) / PowerStep) + 1;
            int maxSpin = Mathf.Max(0, s.maxSpinStep);

            int total = 0, success = 0, withRicochet = 0;
            int[] bySpin = new int[2 * maxSpin + 1];
            int[] fails = new int[5];
            List<Sample> examples = new List<Sample>();

            for (int spin = -maxSpin; spin <= maxSpin; spin++)
            {
                float progress = (handIndex + (spin + maxSpin) / (2f * maxSpin + 1f)) / handCount;
                if (EditorUtility.DisplayCancelableProgressBar("Проверка проходимости", $"{name}: дуга {spin:+0;-0;0}", progress))
                    return false;

                for (int a = 0; a < angles; a++)
                {
                    float angle = -AngleRange + a * AngleStep;
                    Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    for (int p = 0; p < powers; p++)
                    {
                        float power = Mathf.Min(1f, PowerMin + p * PowerStep);
                        ThrowParams tp = new ThrowParams { origin = origin, direction = dir, power01 = power, spinStep = spin };
                        Outcome o = Simulate(FlightSimulator.Launch(tp, s), s, grace, zoneFilter, tableFilter, hasTable, out int ricochets);
                        total++;
                        fails[(int)o]++;
                        if (o != Outcome.Success) continue;

                        success++;
                        bySpin[spin + maxSpin]++;
                        if (ricochets > 0) withRicochet++;
                        // Примеры: первый удачный бросок для каждого шага дуги (не больше трёх)
                        if (examples.Count < 3 && (examples.Count == 0 || examples[examples.Count - 1].spin != spin))
                            examples.Add(new Sample { angle = angle, power = power, spin = spin, ricochets = ricochets });
                    }
                }
            }

            float percent = total > 0 ? 100f * success / total : 0f;
            StringBuilder sb = new StringBuilder();
            sb.Append($"• {name}: удачных {success} из {total} ({percent:0.00}%), из них с рикошетом {withRicochet}. По дуге:");
            for (int i = 0; i < bySpin.Length; i++) sb.Append($" {i - maxSpin:+0;-0;0}={bySpin[i]}");
            sb.Append($"\n    Провалы: стакан/домик {fails[(int)Outcome.Breakable]}, соперник {fails[(int)Outcome.Opponent]}, " +
                      $"со стола {fails[(int)Outcome.OffTable]}, остановилась мимо {fails[(int)Outcome.Stopped]}");
            foreach (Sample e in examples)
                sb.Append($"\n    Пример: угол {e.angle:+0.0;-0.0;0}°, сила {e.power * 100f:0}%, дуга {e.spin:+0;-0;0}" +
                          (e.ricochets > 0 ? $", рикошетов {e.ricochets}" : ""));
            if (success == 0) sb.Append("\n    ⚠ НЕПРОХОДИМА — ни одного удачного броска!");

            line = sb.ToString();
            shortLine = $"{name}: {percent:0.00}% удачных{(success == 0 ? "  ⚠ НЕПРОХОДИМА" : "")}";
            return true;
        }

        // Повтор правил CardFlight.FixedUpdate без побочных эффектов (ничего не ломаем и не толкаем)
        private static Outcome Simulate(FlightState st, FlightSettings s, float grace, ContactFilter2D zoneFilter,
            ContactFilter2D tableFilter, bool hasTable, out int ricochets)
        {
            ricochets = 0;
            float dt = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            Collider2D ignored = null;
            float ignoreUntil = 0f;

            while (true)
            {
                Collider2D ignore = st.time < ignoreUntil ? ignored : null;
                StepEvent ev = FlightSimulator.StepWithCollisions(ref st, s, dt, ignore,
                    out Vector2 _, out Obstacle _, out Collider2D hitCollider);

                switch (ev)
                {
                    case StepEvent.Ricochet:
                        ricochets++;
                        break;
                    case StepEvent.HitPushable:
                        st.vel *= 1f - s.pushSpeedLoss;
                        ignored = hitCollider;
                        ignoreUntil = st.time + PushIgnoreTime;
                        break;
                    case StepEvent.HitBreakable:
                        return Outcome.Breakable;
                }

                if (FindZone(st.pos, 0f, ZoneType.Opponent, zoneFilter)) return Outcome.Opponent;
                if (hasTable && Physics2D.OverlapPoint(st.pos, tableFilter, buffer) == 0) return Outcome.OffTable;
                if (st.IsStopped || st.time >= MaxFlightTime)
                    return FindZone(st.pos, grace, ZoneType.Partner, zoneFilter) ? Outcome.Success : Outcome.Stopped;
            }
        }

        private static bool FindZone(Vector2 point, float radius, ZoneType type, ContactFilter2D filter)
        {
            int count = radius > 0f
                ? Physics2D.OverlapCircle(point, radius, filter, buffer)
                : Physics2D.OverlapPoint(point, filter, buffer);
            for (int i = 0; i < count; i++)
            {
                Zone z = buffer[i].GetComponent<Zone>();
                if (z != null && z.Type == type) return true;
            }
            return false;
        }

        // ---------- Окружение ----------

        private static List<GameObject> CollectHands()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Object o in Selection.objects)
            {
                if (o is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go) && go.GetComponent<LevelConfig>() != null)
                    result.Add(go);
            }
            if (result.Count > 0) return result;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LevelsDir }))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go != null && go.GetComponent<LevelConfig>() != null) result.Add(go);
            }
            result.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
            return result;
        }

        // Точка броска — дочерний LaunchPoint крупье в открытой сцене (как в игре)
        private static Vector2 FindOrigin()
        {
            GameObject launch = GameObject.Find("LaunchPoint"); // активный объект сцены (дочерний у Dealer)
            if (launch != null) return launch.transform.position;
            Debug.LogWarning("[CT] Проходимость: LaunchPoint в сцене не найден — беру точку по умолчанию " + DefaultOrigin);
            return DefaultOrigin;
        }

        // Допуск зоны напарника — из CardFlight в сцене, чтобы проверка совпадала с игрой
        private static float FindGrace()
        {
            CardFlight flight = Object.FindAnyObjectByType<CardFlight>(FindObjectsInactive.Include);
            if (flight == null) return DefaultGrace;
            SerializedProperty p = new SerializedObject(flight).FindProperty("partnerZoneGrace");
            return p != null ? p.floatValue : DefaultGrace;
        }

        private static ContactFilter2D TriggerFilter(LayerMask mask)
        {
            ContactFilter2D f = new ContactFilter2D();
            f.SetLayerMask(mask);
            f.useTriggers = true;
            return f;
        }
    }
}
