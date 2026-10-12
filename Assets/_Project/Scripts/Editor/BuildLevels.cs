using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Уровни: пересобрать Рикошет (Hand_02) и Стену (Hand_03)».
    // Раскладки подобраны перебором ~35 тыс. бросков на модели полёта (тот же расчёт, что меню «проверить проходимость»):
    //   Hand_02 «Рикошет» — напарник на ДЛИННОЙ стороне, путь закрыт стеной домиков; стакан у центра стола
    //                       поворачивает карту на 90° вверх (грань 45°). ≈0.29% удачных, из них ~2/3 — рикошетом.
    //   Hand_03 «Стена»   — два домика перед напарником, фишки на прямой; обход закрутом или отскок от стакана
    //                       у нижнего края. ≈0.17% удачных.
    // Старый Hand_03 «Большой куш» перед пересборкой копируется в Hand_05 (запасной финальный уровень).
    // Префабы раздач — файлы Егора: перед запуском «замок» в #locks на Hand_02 и Hand_03.
    public static class BuildLevels
    {
        private const string Levels = "Assets/_Project/Prefabs/Levels/";
        private const string SeatPath = "Assets/_Project/Prefabs/Seats/Seat.prefab";
        private const string Obstacles = "Assets/_Project/Prefabs/Obstacles/";

        private struct SeatSpec { public string name, display; public Vector2 pos, zoneLocal; public Vector2? zoneSize; }
        private struct ObstacleSpec { public string prefab; public Vector2 pos; }

        private class LevelSpec
        {
            public string file, title, hint;
            public float time, preview;
            public SeatSpec partner;
            public SeatSpec[] opponents;
            public ObstacleSpec[] obstacles;
        }

        [MenuItem("CardThrow/Уровни: пересобрать Рикошет (Hand_02) и Стену (Hand_03)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            // Запасной финальный уровень: старый «Большой куш» сохраняем один раз
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Levels + "Hand_05.prefab") == null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(Levels + "Hand_03.prefab") != null)
            {
                AssetDatabase.CopyAsset(Levels + "Hand_03.prefab", Levels + "Hand_05.prefab");
                Debug.Log("[CT] Старый Hand_03 «Большой куш» скопирован в Hand_05");
            }

            Build(Ricochet());
            Build(Wall());
            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Уровни пересобраны. Дальше: «Уровни: обновить список раздач» и «Уровни: проверить проходимость».");
        }

        // ---------- Раскладки ----------

        private static LevelSpec Ricochet()
        {
            const float px = -1f;                 // напарник у верхнего края
            List<ObstacleSpec> obs = new List<ObstacleSpec>();
            // Стена карточных домиков на y = 2.3 с проёмом 1.6 ед. над стаканом
            for (float x = -7f; x < 5.5f; x += 1.2f)
                if (Mathf.Abs(x - px) > 1.4f) obs.Add(new ObstacleSpec { prefab = "CardHouse", pos = new Vector2(x, 2.3f) });
            obs.Add(new ObstacleSpec { prefab = "Glass", pos = new Vector2(-0.7f, -0.5f) }); // левая-верхняя грань → карта вверх

            return new LevelSpec
            {
                file = "Hand_02", title = "Рикошет", time = 20f, preview = 0.7f,
                hint = "Стакан отражает карту. Целься в его левую грань — карта уйдёт вверх",
                partner = new SeatSpec { name = "Seat_Partner", pos = new Vector2(px, 3.7f), zoneLocal = new Vector2(0f, -0.15f), zoneSize = new Vector2(1.0f, 0.75f) },
                opponents = new[]
                {
                    new SeatSpec { name = "Seat_Opp_1", display = "Игрок 1", pos = new Vector2(px - 2.6f, 3.7f), zoneLocal = new Vector2(0f, -0.15f) },
                    new SeatSpec { name = "Seat_Opp_2", display = "Игрок 2", pos = new Vector2(px + 2.6f, 3.7f), zoneLocal = new Vector2(0f, -0.15f) },
                    new SeatSpec { name = "Seat_Opp_3", display = "Игрок 3", pos = new Vector2(-2f, -3.7f), zoneLocal = new Vector2(0f, 0.15f) },
                    new SeatSpec { name = "Seat_Opp_4", display = "Игрок 4", pos = new Vector2(4f, -3.7f), zoneLocal = new Vector2(0f, 0.15f) },
                },
                obstacles = obs.ToArray()
            };
        }

        private static LevelSpec Wall()
        {
            return new LevelSpec
            {
                file = "Hand_03", title = "Стена", time = 18f, preview = 0.55f,
                hint = "Домики стеной: обойди их закрутом или отрази карту от стакана",
                partner = new SeatSpec { name = "Seat_Partner", pos = new Vector2(7.9f, 0f), zoneLocal = new Vector2(0.2f, 0f), zoneSize = new Vector2(1.2f, 0.85f) },
                opponents = new[]
                {
                    new SeatSpec { name = "Seat_Opp_1", display = "Игрок 1", pos = new Vector2(-4f, 3.7f), zoneLocal = new Vector2(0f, -0.15f) },
                    new SeatSpec { name = "Seat_Opp_2", display = "Игрок 2", pos = new Vector2(-1f, -3.7f), zoneLocal = new Vector2(0f, 0.15f) },
                    new SeatSpec { name = "Seat_Opp_3", display = "Игрок 3", pos = new Vector2(3f, -3.7f), zoneLocal = new Vector2(0f, 0.15f) },
                },
                obstacles = new[]
                {
                    new ObstacleSpec { prefab = "CardHouse", pos = new Vector2(5.2f, 0.45f) },
                    new ObstacleSpec { prefab = "CardHouse", pos = new Vector2(5.2f, -0.45f) },
                    new ObstacleSpec { prefab = "Chips", pos = new Vector2(-2f, 0f) },
                    new ObstacleSpec { prefab = "Glass", pos = new Vector2(1f, -3.3f) },
                }
            };
        }

        // ---------- Сборка ----------

        private static void Build(LevelSpec spec)
        {
            string path = Levels + spec.file + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                LevelConfig level = root.GetComponent<LevelConfig>();
                Transform seats = root.transform.Find("Seats");
                if (level == null || seats == null)
                {
                    Debug.LogError($"[CT] {spec.file}: нет LevelConfig или Seats");
                    return;
                }

                SerializedObject so = new SerializedObject(level);
                so.FindProperty("handTitle").stringValue = spec.title;
                so.FindProperty("hint").stringValue = spec.hint;
                so.FindProperty("timeLimit").floatValue = spec.time;
                so.FindProperty("previewFraction").floatValue = spec.preview;
                so.ApplyModifiedPropertiesWithoutUndo();

                GameObject seatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SeatPath);
                HashSet<string> keep = new HashSet<string> { spec.partner.name };
                ApplySeat(seats, seatPrefab, spec.partner, true);
                foreach (SeatSpec s in spec.opponents)
                {
                    ApplySeat(seats, seatPrefab, s, false);
                    keep.Add(s.name);
                }
                for (int i = seats.childCount - 1; i >= 0; i--) // лишние соперники прошлой раскладки
                    if (!keep.Contains(seats.GetChild(i).name)) Object.DestroyImmediate(seats.GetChild(i).gameObject);

                Transform obstacles = root.transform.Find("Obstacles");
                if (obstacles == null)
                {
                    obstacles = new GameObject("Obstacles").transform;
                    obstacles.SetParent(root.transform, false);
                }
                for (int i = obstacles.childCount - 1; i >= 0; i--) Object.DestroyImmediate(obstacles.GetChild(i).gameObject);
                Dictionary<string, int> counts = new Dictionary<string, int>();
                foreach (ObstacleSpec o in spec.obstacles)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Obstacles + o.prefab + ".prefab");
                    if (prefab == null) { Debug.LogError($"[CT] Нет префаба {o.prefab}"); continue; }
                    counts.TryGetValue(o.prefab, out int n);
                    counts[o.prefab] = n + 1;
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, obstacles);
                    go.name = n == 0 ? o.prefab : $"{o.prefab} ({n})";
                    go.transform.localPosition = o.pos;
                }

                SerializedObject levelSo = new SerializedObject(level); // ссылка на напарника — после пересборки мест
                Transform partner = seats.Find(spec.partner.name);
                levelSo.FindProperty("partnerSeat").objectReferenceValue = partner != null ? partner.GetComponent<Seat>() : null;
                levelSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[CT] {spec.file} «{spec.title}»: {spec.opponents.Length} соперника, {spec.obstacles.Length} препятствий, {spec.time} с, превью {spec.preview:P0}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplySeat(Transform seats, GameObject seatPrefab, SeatSpec s, bool isPartner)
        {
            Transform t = seats.Find(s.name);
            if (t == null)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(seatPrefab, seats);
                go.name = s.name;
                t = go.transform;
            }
            t.localPosition = s.pos;

            Seat seat = t.GetComponent<Seat>();
            if (seat == null) return;
            SerializedObject so = new SerializedObject(seat);
            so.FindProperty("isPartner").boolValue = isPartner;
            if (!string.IsNullOrEmpty(s.display)) so.FindProperty("displayName").stringValue = s.display;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (seat.Zone == null) return;
            seat.Zone.transform.localPosition = s.zoneLocal;
            SerializedObject zso = new SerializedObject(seat.Zone);
            SerializedProperty type = zso.FindProperty("type");
            if (type != null) type.enumValueIndex = (int)(isPartner ? ZoneType.Partner : ZoneType.Opponent);
            zso.ApplyModifiedPropertiesWithoutUndo();

            BoxCollider2D box = seat.Zone.GetComponent<BoxCollider2D>();
            if (box != null) box.size = s.zoneSize ?? new Vector2(1.4f, 1.0f);
        }
    }
}
