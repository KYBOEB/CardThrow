using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow
{
    // Инструменты Алексея (только редактор). Меню CardThrow → Алексей.
    // Собирают префабы и песочницу кодом — так меньше ручных шагов и ошибок.
    public static class GameplayTools
    {
        private const string Root = "Assets/_Project/";
        private const string ObstacleFolder = Root + "Prefabs/Obstacles";
        private const string CardPrefabPath = Root + "Prefabs/Gameplay/Card.prefab";
        private const string GlassPath = ObstacleFolder + "/Glass.prefab";
        private const string BottlePath = ObstacleFolder + "/Bottle.prefab";
        private const string FlightSettingsPath = Root + "Data/FlightSettings.asset";

        private static readonly Vector3 LaunchPoint = new Vector3(-7.6f, 0f, 0f); // как в Main: Dealer (−9;0) + LaunchPoint (1.4;0)

        // ---------- 1. Префабы препятствий ----------

        [MenuItem("CardThrow/Алексей/1. Создать префабы Glass и Bottle")]
        private static void CreateObstaclePrefabs()
        {
            if (!AssetDatabase.IsValidFolder(ObstacleFolder))
                AssetDatabase.CreateFolder(Root + "Prefabs", "Obstacles");

            // Пересоздание с нуля меняет внутренние ID объектов → стаканы и бутылки в уровнях Егора станут Missing.
            // Поэтому существующие префабы этот пункт больше не трогает: правки — через пункты 5 и 6.
            if (AssetExists(GlassPath) || AssetExists(BottlePath))
            {
                EditorUtility.DisplayDialog("Префабы уже есть",
                    "Glass и Bottle уже созданы и стоят в уровнях. Пересоздавать нельзя — сломаются уровни.\n" +
                    "Стакан меняет пункт 5, бутылку — пункт 6.", "OK");
                return;
            }

            // Стакан: рикошет. Круглый коллайдер r = 0.3.
            GameObject glass = NewObstacle("Glass", "Obstacles/glass.png");
            glass.AddComponent<CircleCollider2D>().radius = 0.3f;
            glass.AddComponent<RicochetObstacle>();
            SavePrefab(glass, GlassPath);

            // Бутылка: хрупкая. Круглый коллайдер r = 0.35, спрайт осколков, звук.
            GameObject bottle = NewObstacle("Bottle", "Obstacles/bottle.png");
            bottle.AddComponent<CircleCollider2D>().radius = 0.35f;
            BreakableObstacle breakable = bottle.AddComponent<BreakableObstacle>();
            SerializedObject so = new SerializedObject(breakable);
            so.FindProperty("spriteRenderer").objectReferenceValue = bottle.GetComponent<SpriteRenderer>();
            so.FindProperty("brokenSprite").objectReferenceValue = LoadSprite("Obstacles/bottle_broken.png");
            so.FindProperty("breakSound").enumValueIndex = (int)SoundId.BottleBreak;
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(bottle, BottlePath);

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Готово: " + GlassPath + ", " + BottlePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(BottlePath);
        }

        // ---------- 2. Префаб карты ----------

        [MenuItem("CardThrow/Алексей/2. Настроить префаб Card")]
        private static void SetupCardPrefab()
        {
            if (!AssetExists(CardPrefabPath))
            {
                Debug.LogError("[CT] Не найден " + CardPrefabPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            CardFlight flight = root.GetComponent<CardFlight>();
            if (flight == null) flight = root.AddComponent<CardFlight>();

            SerializedObject so = new SerializedObject(flight);
            so.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FlightSettings>(FlightSettingsPath);
            so.FindProperty("cardRenderer").objectReferenceValue = root.GetComponent<SpriteRenderer>();
            SerializedProperty faces = so.FindProperty("faces");
            faces.arraySize = 10;
            for (int i = 0; i < 10; i++)
                faces.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite($"Cards/card_{i + 2}.png");
            so.FindProperty("crumpledSprite").objectReferenceValue = LoadSprite("Cards/card_crumpled.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[CT] Префаб Card настроен: FlightSettings, 10 лиц карт, смятая карта.");
        }

        // ---------- 4. Префабы Dealer и TrajectoryPreview ----------

        private const string DealerPrefabPath = Root + "Prefabs/Gameplay/Dealer.prefab";
        private const string PreviewPrefabPath = Root + "Prefabs/Gameplay/TrajectoryPreview.prefab";

        [MenuItem("CardThrow/Алексей/4. Настроить префабы Dealer и TrajectoryPreview")]
        private static void SetupDealerAndPreview()
        {
            FlightSettings fs = AssetDatabase.LoadAssetAtPath<FlightSettings>(FlightSettingsPath);

            // Dealer: рука + три карты веером (дочерние объекты), ссылки в DealerHand и AimController.
            GameObject dealer = PrefabUtility.LoadPrefabContents(DealerPrefabPath);

            SpriteRenderer hand = GetOrCreateChildRenderer(dealer.transform, "Hand", Vector3.zero, 0f);
            hand.sprite = LoadSprite("Characters/dealer_hand_idle.png");
            hand.sortingLayerName = "Objects";
            hand.sortingOrder = 5;

            SpriteRenderer[] cards = new SpriteRenderer[3];
            float[] ys = { -0.85f, 0f, 0.85f };
            for (int i = 0; i < 3; i++)
            {
                // Карты лежат «боком» (−90°) вдоль стола, чуть веером.
                cards[i] = GetOrCreateChildRenderer(dealer.transform, "HandCard_" + i,
                    new Vector3(0.55f, ys[i], 0f), -90f + (i - 1) * -6f);
                cards[i].sprite = LoadSprite("Cards/card_back.png");
                cards[i].sortingLayerName = "Card";
            }

            DealerHand dh = dealer.GetComponent<DealerHand>();
            SerializedObject dso = new SerializedObject(dh);
            SerializedProperty cr = dso.FindProperty("cardRenderers");
            cr.arraySize = 3;
            for (int i = 0; i < 3; i++) cr.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            SerializedProperty faces = dso.FindProperty("faces");
            faces.arraySize = 10;
            for (int i = 0; i < 10; i++)
                faces.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite($"Cards/card_{i + 2}.png");
            dso.FindProperty("cardBack").objectReferenceValue = LoadSprite("Cards/card_back.png");
            dso.FindProperty("handRenderer").objectReferenceValue = hand;
            dso.FindProperty("handIdle").objectReferenceValue = LoadSprite("Characters/dealer_hand_idle.png");
            dso.FindProperty("handPinch").objectReferenceValue = LoadSprite("Characters/dealer_hand_pinch.png");
            dso.FindProperty("handPull").objectReferenceValue = LoadSprite("Characters/dealer_hand_pull.png");
            dso.ApplyModifiedPropertiesWithoutUndo();

            AimController aim = dealer.GetComponent<AimController>();
            SerializedObject aso = new SerializedObject(aim);
            aso.FindProperty("settings").objectReferenceValue = fs;
            aso.FindProperty("dealerHand").objectReferenceValue = dh;
            aso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(dealer, DealerPrefabPath);
            PrefabUtility.UnloadPrefabContents(dealer);

            // TrajectoryPreview: настройки и спрайты точки/крестика.
            GameObject prev = PrefabUtility.LoadPrefabContents(PreviewPrefabPath);
            TrajectoryPreview tp = prev.GetComponent<TrajectoryPreview>();
            SerializedObject pso = new SerializedObject(tp);
            pso.FindProperty("settings").objectReferenceValue = fs;
            pso.FindProperty("dotSprite").objectReferenceValue = LoadSprite("VFX/fx_dot.png");
            pso.FindProperty("crossSprite").objectReferenceValue = LoadSprite("VFX/fx_cross.png");
            pso.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prev, PreviewPrefabPath);
            PrefabUtility.UnloadPrefabContents(prev);

            Debug.Log("[CT] Префабы Dealer (рука + 3 карты, ссылки) и TrajectoryPreview настроены.");
        }

        private static SpriteRenderer GetOrCreateChildRenderer(Transform parent, string name, Vector3 localPos, float angleZ)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(0f, 0f, angleZ);
            t.localScale = Vector3.one;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
            return sr;
        }

        // ---------- 5. Новый стакан: восьмиугольник ----------

        private const float GlassRadius = 0.45f;

        [MenuItem("CardThrow/Алексей/5. Стакан: восьмиугольник r = 0.45")]
        private static void UpgradeGlass()
        {
            // Правим СУЩЕСТВУЮЩИЙ префаб (а не пересоздаём) — тогда стаканы в уровнях Егора обновятся сами.
            GameObject root = PrefabUtility.LoadPrefabContents(GlassPath);

            // Круглый коллайдер → правильный восьмиугольник: у гранёного стакана плоские грани,
            // отражение от грани предсказуемо (угол падения = углу отражения).
            foreach (CircleCollider2D c in root.GetComponents<CircleCollider2D>()) Object.DestroyImmediate(c, true);
            PolygonCollider2D poly = root.GetComponent<PolygonCollider2D>();
            if (poly == null) poly = root.AddComponent<PolygonCollider2D>();
            var pts = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                // +22.5°: грани смотрят ровно вправо/влево/вверх/вниз, а не углы
                float a = (22.5f + 45f * i) * Mathf.Deg2Rad;
                pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * GlassRadius;
            }
            poly.pathCount = 1;
            poly.SetPath(0, pts);

            // Картинку подгоняем под диаметр коллайдера (дочерний Visual), сам корень не масштабируем —
            // иначе вырос бы и коллайдер. Когда Кирилл заменит glass.png на 180 px — запусти пункт ещё раз.
            SpriteRenderer rootSr = root.GetComponent<SpriteRenderer>();
            Sprite sprite = rootSr != null && rootSr.sprite != null ? rootSr.sprite : LoadSprite("Obstacles/glass.png");
            if (rootSr != null) Object.DestroyImmediate(rootSr, true);
            SpriteRenderer vis = GetOrCreateChildRenderer(root.transform, "Visual", Vector3.zero, 0f);
            vis.sprite = sprite;
            vis.sortingLayerName = "Objects";
            if (sprite != null)
            {
                float k = 2f * GlassRadius / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                vis.transform.localScale = new Vector3(k, k, 1f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, GlassPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[CT] Стакан: восьмиугольник r = " + GlassRadius + ", картинка подогнана под диаметр.");
        }

        // ---------- 6. Фишки, карточный домик, осколки бутылки ----------

        private const string ChipsPath = ObstacleFolder + "/Chips.prefab";
        private const string CardHousePath = ObstacleFolder + "/CardHouse.prefab";

        [MenuItem("CardThrow/Алексей/6. Создать Chips и CardHouse, осколки у Bottle")]
        private static void CreateChipsAndCardHouse()
        {
            if (AssetExists(ChipsPath) || AssetExists(CardHousePath))
            {
                if (!EditorUtility.DisplayDialog("Префабы уже есть",
                        "Chips или CardHouse уже существуют. Если их уже поставили в уровни, пересоздание их сломает.\n" +
                        "Пересоздать?", "Пересоздать", "Отмена")) return;
            }

            // --- Стопка фишек: корень с круглым коллайдером + 5 фишек-детей с Rigidbody2D ---
            GameObject chips = new GameObject("Chips");
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            chips.layer = obstacleLayer;
            chips.AddComponent<CircleCollider2D>().radius = 0.25f;
            string[] colors = { "red", "blue", "green", "black", "red" };
            var bodies = new Rigidbody2D[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                GameObject chip = new GameObject("Chip_" + i);
                chip.layer = obstacleLayer;
                chip.transform.SetParent(chips.transform, false);
                chip.transform.localPosition = new Vector3(0.015f * i, 0.03f * i, 0f); // стопка чуть «съезжает»
                SpriteRenderer sr = chip.AddComponent<SpriteRenderer>();
                sr.sprite = LoadSprite($"Obstacles/chip_{colors[i]}.png");
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = i;
                chip.AddComponent<CircleCollider2D>().radius = 0.2f;
                Rigidbody2D rb = chip.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.mass = 0.2f;
                rb.linearDamping = 3f;
                rb.angularDamping = 3f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                bodies[i] = rb;
            }
            PushableObstacle push = chips.AddComponent<PushableObstacle>();
            SerializedObject pso = new SerializedObject(push);
            pso.FindProperty("stackCollider").objectReferenceValue = chips.GetComponent<CircleCollider2D>();
            SerializedProperty arr = pso.FindProperty("chips");
            arr.arraySize = bodies.Length;
            for (int i = 0; i < bodies.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = bodies[i];
            pso.FindProperty("scatterSound").enumValueIndex = (int)SoundId.ChipsScatter;
            pso.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(chips, ChipsPath);

            // --- Карточный домик: хрупкий, прямоугольник 1.2 × 0.8, при поломке разлетаются карты ---
            GameObject house = NewObstacle("CardHouse", "Obstacles/cardhouse.png");
            house.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 0.8f);
            BreakableObstacle hb = house.AddComponent<BreakableObstacle>();
            SerializedObject hso = new SerializedObject(hb);
            hso.FindProperty("spriteRenderer").objectReferenceValue = house.GetComponent<SpriteRenderer>();
            hso.FindProperty("brokenSprite").objectReferenceValue = LoadSprite("Obstacles/cardhouse_broken.png");
            hso.FindProperty("breakSound").enumValueIndex = (int)SoundId.CardHouseCollapse;
            hso.FindProperty("debrisSprite").objectReferenceValue = LoadSprite("Cards/card_back.png");
            hso.FindProperty("debrisCount").intValue = 5;
            hso.FindProperty("debrisScale").floatValue = 0.5f;
            hso.FindProperty("debrisSpeed").floatValue = 2.5f;
            hso.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(house, CardHousePath);

            // --- Бутылка: только добавляем осколки (префаб правим, а не пересоздаём) ---
            GameObject bottle = PrefabUtility.LoadPrefabContents(BottlePath);
            BreakableObstacle bb = bottle.GetComponent<BreakableObstacle>();
            if (bb != null)
            {
                SerializedObject bso = new SerializedObject(bb);
                bso.FindProperty("debrisSprite").objectReferenceValue = LoadSprite("VFX/fx_shard.png");
                bso.FindProperty("debrisCount").intValue = 7;
                bso.FindProperty("debrisScale").floatValue = 0.8f;
                bso.FindProperty("debrisSpeed").floatValue = 3.5f;
                bso.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(bottle, BottlePath);
            }
            PrefabUtility.UnloadPrefabContents(bottle);

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Готово: " + ChipsPath + ", " + CardHousePath + ", осколки у Bottle.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ChipsPath);
        }

        // ---------- 7. Движущаяся бутылка (вариант префаба Bottle) ----------

        private const string BottleTossPath = ObstacleFolder + "/BottleToss.prefab";

        [MenuItem("CardThrow/Алексей/7. Создать BottleToss (перекидка бутылки)")]
        private static void CreateBottleToss()
        {
            if (AssetExists(BottleTossPath))
            {
                EditorUtility.DisplayDialog("Уже есть", "BottleToss.prefab уже создан. Настраивай его в инспекторе.", "OK");
                return;
            }

            // Вариант префаба: всё, что меняется в Bottle (спрайт Кирилла, осколки), достанется и BottleToss
            GameObject bottle = AssetDatabase.LoadAssetAtPath<GameObject>(BottlePath);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(bottle);
            inst.name = "BottleToss";
            Rigidbody2D rb = inst.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            inst.AddComponent<ObstacleMover>();
            PrefabUtility.SaveAsPrefabAsset(inst, BottleTossPath);
            Object.DestroyImmediate(inst);

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Готово: " + BottleTossPath + " (вариант Bottle + ObstacleMover)");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(BottleTossPath);
        }

        // ---------- 3. Песочница ----------

        [MenuItem("CardThrow/Алексей/3. Собрать тестовый стол в открытой сцене")]
        private static void BuildSandbox()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.Contains("/Sandbox/"))
            {
                EditorUtility.DisplayDialog("Не та сцена",
                    "Открой свою песочницу Scenes/Sandbox/Sandbox_Alexey. Main и чужие сцены не трогаем.", "OK");
                return;
            }

            // Стол: спрайт + триггер 19 × 9.5 на слое Table.
            GameObject table = new GameObject("Table");
            table.layer = LayerMask.NameToLayer("Table");
            SpriteRenderer tsr = table.AddComponent<SpriteRenderer>();
            tsr.sprite = LoadSprite("Table/table_felt.png");
            tsr.sortingLayerName = "Table";
            BoxCollider2D tcol = table.AddComponent<BoxCollider2D>();
            tcol.isTrigger = true;
            tcol.size = new Vector2(19f, 9.5f);
            Undo.RegisterCreatedObjectUndo(table, "Sandbox");

            // Зоны: напарник прямо по курсу, соперник выше.
            MakeZone("Zone_Partner", ZoneType.Partner, "Table/zone_partner.png", new Vector3(6f, 0f, 0f));
            MakeZone("Zone_Opponent", ZoneType.Opponent, "Table/zone_opponent.png", new Vector3(6f, 3f, 0f));

            // Препятствия: бутылка на прямой линии, стакан для рикошета.
            PlacePrefab(BottlePath, new Vector3(-1f, 0f, 0f));
            PlacePrefab(GlassPath, new Vector3(1f, -2.8f, 0f));

            // Карта в точке броска.
            GameObject card = PlacePrefab(CardPrefabPath, LaunchPoint);

            // «Метатель»: существующий FlightDebugGizmo или новый.
            FlightDebugGizmo gizmo = Object.FindAnyObjectByType<FlightDebugGizmo>();
            if (gizmo == null)
            {
                GameObject g = new GameObject("FlightGizmo");
                gizmo = g.AddComponent<FlightDebugGizmo>();
                Undo.RegisterCreatedObjectUndo(g, "Sandbox");
            }
            Undo.RecordObject(gizmo.transform, "Sandbox");
            gizmo.transform.position = LaunchPoint;
            SerializedObject gso = new SerializedObject(gizmo);
            gso.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FlightSettings>(FlightSettingsPath);
            gso.FindProperty("card").objectReferenceValue = card != null ? card.GetComponent<CardFlight>() : null;
            gso.FindProperty("angleDeg").floatValue = 0f;
            gso.FindProperty("power01").floatValue = 0.8f;
            gso.FindProperty("spinStep").intValue = 0;
            gso.ApplyModifiedProperties();

            // Камера как в Main: ортографическая, видно весь стол.
            Camera cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam, "Sandbox");
                Undo.RecordObject(cam.transform, "Sandbox");
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = gizmo.gameObject;
            Debug.Log("[CT] Песочница собрана. Ctrl+S — сохранить. Play → Space — бросок, R — перезапуск.");
        }

        // ---------- Вспомогательное ----------

        private static GameObject NewObstacle(string name, string spritePath)
        {
            GameObject go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("Obstacle");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spritePath);
            sr.sortingLayerName = "Objects";
            return go;
        }

        private static void MakeZone(string name, ZoneType type, string spritePath, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("Zone");
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spritePath);
            sr.sortingLayerName = "Table";
            sr.sortingOrder = 1;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 1f);
            Zone zone = go.AddComponent<Zone>();
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(go, "Sandbox");
        }

        private static GameObject PlacePrefab(string path, Vector3 pos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[CT] Нет префаба " + path + " — сначала пункт меню 1.");
                return null;
            }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(inst, "Sandbox");
            return inst;
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static Sprite LoadSprite(string relativePath)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Sprites/" + relativePath);
            if (s == null) Debug.LogWarning("[CT] Не найден спрайт " + relativePath);
            return s;
        }

        private static bool AssetExists(string path) => AssetDatabase.LoadAssetAtPath<Object>(path) != null;
    }
}
