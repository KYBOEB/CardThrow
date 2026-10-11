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

            if (AssetExists(GlassPath) || AssetExists(BottlePath))
            {
                if (!EditorUtility.DisplayDialog("Префабы уже есть",
                        "Glass.prefab или Bottle.prefab уже существуют. Пересоздать (ручные правки в них пропадут)?",
                        "Пересоздать", "Отмена")) return;
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
