using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Уровни: собрать Hand_03 (Большой куш)».
    // Hand_03 — файл Егора: перед запуском «замок» в #locks («беру Hand_03»), после пуша — «отдал».
    // Ставит ручки сложности финального уровня (CONTEXT, таблица уровней) и расстановку, проверенную расчётом
    // на модели полёта (maxSpeed 23): проходится закрутом ±1…±2 или отскоком от стакана, прямой бросок закрыт.
    // Повторный запуск безопасен: препятствия пересоздаются, недостающие соперники добавляются, лишнего не трогает.
    public static class BuildHand03
    {
        private const string HandPath = "Assets/_Project/Prefabs/Levels/Hand_03.prefab";
        private const string SeatPath = "Assets/_Project/Prefabs/Seats/Seat.prefab";
        private const string BottlePath = "Assets/_Project/Prefabs/Obstacles/Bottle.prefab";
        private const string GlassPath = "Assets/_Project/Prefabs/Obstacles/Glass.prefab";

        // ---- Ручки сложности (уровень 3 из CONTEXT) ----
        private const string Title = "Большой куш";
        private const string Hint = "Мало времени, короткий прицел, соперники рядом с напарником — закрути в обход бутылок!";
        private const float TimeLimit = 15f;
        private const float PreviewFraction = 0.45f;

        // ---- Напарник: маленькая зона у самого игрока (как у Егора в Hand_01/02) ----
        private static readonly Vector2 PartnerZoneLocal = new Vector2(0.2f, 0f);
        private static readonly Vector2 PartnerZoneSize = new Vector2(0.9f, 0.7f);

        // ---- Соперники: двое у крупье, двое у напарника. zoneLocalY — сдвиг зоны от игрока к столу ----
        private struct Opp { public string name, display; public Vector2 pos; public float zoneLocalY; }
        private static readonly Opp[] Opponents =
        {
            new Opp { name = "Seat_Opp_1", display = "Игрок 1", pos = new Vector2(-2f,  3.7f), zoneLocalY = -0.15f },
            new Opp { name = "Seat_Opp_2", display = "Игрок 2", pos = new Vector2(-2f, -3.7f), zoneLocalY =  0.15f },
            new Opp { name = "Seat_Opp_3", display = "Игрок 3", pos = new Vector2( 5f,  3.7f), zoneLocalY = -0.8f  }, // зона выдвинута к линии броска
            new Opp { name = "Seat_Opp_4", display = "Игрок 4", pos = new Vector2( 5f, -3.7f), zoneLocalY =  0.8f  },
        };

        // ---- Препятствия: две бутылки на прямой линии, стакан снизу для отскока ----
        private static readonly Vector2[] Bottles = { new Vector2(1f, 0f), new Vector2(5.5f, 0f) };
        private static readonly Vector2[] Glasses = { new Vector2(3.5f, -2.2f) };

        [MenuItem("CardThrow/Уровни: собрать Hand_03 (Большой куш)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameObject seatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SeatPath);
            GameObject bottlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottlePath);
            GameObject glassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlassPath);
            if (seatPrefab == null || bottlePrefab == null || glassPrefab == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "Не найден один из префабов: Seat, Bottle или Glass.", "OK");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(HandPath);
            try
            {
                LevelConfig level = root.GetComponent<LevelConfig>();
                Transform seats = root.transform.Find("Seats");
                if (level == null || seats == null)
                {
                    EditorUtility.DisplayDialog("CardThrow", "В Hand_03 нет LevelConfig или объекта Seats.", "OK");
                    return;
                }

                SetupLevel(level);
                SetupPartner(seats);
                foreach (Opp o in Opponents) SetupOpponent(seats, seatPrefab, o);
                SetupObstacles(root.transform, bottlePrefab, glassPrefab);

                PrefabUtility.SaveAsPrefabAsset(root, HandPath);
                Debug.Log("[CT] Hand_03 собран: 15 с, превью 45%, зона 0.9×0.7, 4 соперника, 2 бутылки и стакан. Отпусти «замок» после пуша.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupLevel(LevelConfig level)
        {
            SerializedObject so = new SerializedObject(level);
            so.FindProperty("handTitle").stringValue = Title;
            so.FindProperty("hint").stringValue = Hint;
            so.FindProperty("timeLimit").floatValue = TimeLimit;
            so.FindProperty("previewFraction").floatValue = PreviewFraction;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupPartner(Transform seats)
        {
            foreach (Seat seat in seats.GetComponentsInChildren<Seat>(true))
            {
                if (!seat.IsPartner || seat.Zone == null) continue;
                seat.Zone.transform.localPosition = PartnerZoneLocal;
                BoxCollider2D box = seat.Zone.GetComponent<BoxCollider2D>();
                if (box != null) box.size = PartnerZoneSize;
                return;
            }
            Debug.LogWarning("[CT] Hand_03: напарник не найден — зону не меняю");
        }

        private static void SetupOpponent(Transform seats, GameObject seatPrefab, Opp o)
        {
            Transform t = seats.Find(o.name);
            if (t == null) // нет — добавляем экземпляр префаба Seat (связь с префабом сохраняется)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(seatPrefab, seats);
                go.name = o.name;
                t = go.transform;
            }
            t.localPosition = o.pos;

            Seat seat = t.GetComponent<Seat>();
            if (seat == null) return;
            SerializedObject so = new SerializedObject(seat);
            so.FindProperty("displayName").stringValue = o.display;
            so.FindProperty("isPartner").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (seat.Zone != null) seat.Zone.transform.localPosition = new Vector3(0f, o.zoneLocalY, 0f);
        }

        private static void SetupObstacles(Transform root, GameObject bottlePrefab, GameObject glassPrefab)
        {
            Transform obstacles = root.Find("Obstacles");
            if (obstacles == null)
            {
                obstacles = new GameObject("Obstacles").transform;
                obstacles.SetParent(root, false);
            }
            for (int i = obstacles.childCount - 1; i >= 0; i--) // пересоздаём, чтобы повторный запуск не дублировал
                Object.DestroyImmediate(obstacles.GetChild(i).gameObject);

            for (int i = 0; i < Bottles.Length; i++) Place(bottlePrefab, obstacles, Bottles[i], i == 0 ? "Bottle" : $"Bottle ({i})");
            for (int i = 0; i < Glasses.Length; i++) Place(glassPrefab, obstacles, Glasses[i], i == 0 ? "Glass" : $"Glass ({i})");
        }

        private static void Place(GameObject prefab, Transform parent, Vector2 pos, string name)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = pos;
        }
    }
}
