using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Собрать Seat и раздачи Hand_00…03» (запускать ПОСЛЕ «Собрать сцену Main»).
    // Создаёт Prefabs/Seats/Seat.prefab, Prefabs/Levels/Hand_Template.prefab, Hand_00…Hand_03
    // и кладёт раздачи по порядку в LevelManager.hands на сцене Main.
    public static class BuildSeatAndHands
    {
        private const string Root = "Assets/_Project";
        private const string SeatPath = Root + "/Prefabs/Seats/Seat.prefab";
        private const string TemplatePath = Root + "/Prefabs/Levels/Hand_Template.prefab";
        private const string MainScene = Root + "/Scenes/Main.unity";
        private const int LayerZone = 8;

        [MenuItem("CardThrow/Собрать Seat и раздачи Hand_00…03")]
        public static void Build()
        {
            if (!File.Exists(MainScene))
            {
                Debug.LogError("[CT] Сначала запустите «CardThrow → Собрать сцену Main (один раз)».");
                return;
            }
            if (File.Exists(TemplatePath) &&
                !EditorUtility.DisplayDialog("CardThrow",
                    "Seat и раздачи уже существуют. Пересборка перезапишет их вместе с правками Егора. Продолжить?",
                    "Пересобрать", "Отмена"))
                return;

            GameObject seatPrefab = BuildSeat();
            BuildHands(seatPrefab);
            AssignToLevelManager();
            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Готово: Seat, Hand_Template, Hand_00…Hand_03 созданы и добавлены в LevelManager.hands.");
        }

        // ---------- Seat ----------
        private static GameObject BuildSeat()
        {
            GameObject root = new GameObject("Seat");
            Seat seat = root.AddComponent<Seat>();

            Transform body = Child(root, "Body", Vector3.zero);
            AddSprite(body.gameObject, "Characters/player_body_1.png", 0);

            Transform head = Child(root, "Head", new Vector3(0f, 0.15f, 0f));
            AddSprite(head.gameObject, "Characters/player_head_1.png", 1);

            Transform glow = Child(root, "PartnerGlow", Vector3.zero);
            AddSprite(glow.gameObject, "Characters/partner_glow.png", -1);
            glow.gameObject.SetActive(false);

            // ВАЖНО: TextMeshPro заменяет Transform на RectTransform, поэтому старую ссылку на Transform дальше не используем
            GameObject labelGo = new GameObject("ScoreLabel");
            labelGo.transform.SetParent(root.transform, false);
            TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
            label.rectTransform.localPosition = new Vector3(0f, 1.0f, 0f);
            label.text = "12";
            label.fontSize = 6f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            MeshRenderer mr = label.GetComponent<MeshRenderer>();
            mr.sortingLayerName = "Objects";
            mr.sortingOrder = 10;

            Transform zoneT = Child(root, "Zone", new Vector3(0f, -1.3f, 0f));
            zoneT.gameObject.layer = LayerZone;
            BoxCollider2D col = zoneT.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 1.0f);
            Zone zone = zoneT.gameObject.AddComponent<Zone>();

            SetRef(zone, "seat", seat);
            SetRef(seat, "zone", zone);
            SetRef(seat, "scoreLabel", label);
            SetRef(seat, "head", head);
            SetRef(seat, "partnerHighlight", glow.gameObject);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SeatPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------- раздачи ----------
        private static void BuildHands(GameObject seatPrefab)
        {
            // шаблон
            GameObject root = new GameObject("Hand_Template");
            LevelConfig config = root.AddComponent<LevelConfig>();
            Transform seats = Child(root, "Seats", Vector3.zero);
            Child(root, "Obstacles", Vector3.zero);

            // Бросок идёт слева направо вдоль оси X. Напарник — у правого торца, соперники — по краям от линии броска.
            // Зона карт лежит ближе к центру стола: у напарника слева от него, у верхнего соперника ниже, у нижнего — выше.
            AddSeat(seatPrefab, seats, "Seat_Opp_1", new Vector3(0f, 3.7f, 0f), new Vector3(0f, -1.3f, 0f), "Игрок 1", false);
            Seat partner = AddSeat(seatPrefab, seats, "Seat_Partner", new Vector3(7.9f, 0f, 0f), new Vector3(-1.5f, 0f, 0f), "Напарник", true);
            AddSeat(seatPrefab, seats, "Seat_Opp_2", new Vector3(0f, -3.7f, 0f), new Vector3(0f, 1.3f, 0f), "Игрок 2", false);
            SetRef(config, "partnerSeat", partner);
            GameObject template = PrefabUtility.SaveAsPrefabAsset(root, TemplatePath);
            Object.DestroyImmediate(root);

            // четыре копии и их начальные данные (Егор потом меняет всё в инспекторе)
            MakeHand(template, 0, true, "Обучение", 14, new[] { 5, 7, 9 });
            MakeHand(template, 1, false, "Первая раздача", 14, new[] { 5, 7, 9 });
            MakeHand(template, 2, false, "Вторая раздача", 11, new[] { 4, 10, 7 });
            MakeHand(template, 3, false, "Последняя раздача", 13, new[] { 8, 3, 6 });
        }

        private static void MakeHand(GameObject template, int n, bool tutorial, string title, int partnerScore, int[] cards)
        {
            string path = $"{Root}/Prefabs/Levels/Hand_0{n}.prefab";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CopyAsset(TemplatePath, path);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            contents.name = $"Hand_0{n}";
            LevelConfig cfg = contents.GetComponent<LevelConfig>();
            SerializedObject so = new SerializedObject(cfg);
            so.FindProperty("isTutorial").boolValue = tutorial;
            so.FindProperty("handTitle").stringValue = title;
            so.FindProperty("partnerStartScore").intValue = partnerScore;
            SerializedProperty arr = so.FindProperty("cardValues");
            arr.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++) arr.GetArrayElementAtIndex(i).intValue = cards[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static Seat AddSeat(GameObject seatPrefab, Transform parent, string name, Vector3 pos, Vector3 zoneLocalPos, string display, bool isPartner)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(seatPrefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            Seat seat = go.GetComponent<Seat>();
            SerializedObject so = new SerializedObject(seat);
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("isPartner").boolValue = isPartner;
            so.ApplyModifiedPropertiesWithoutUndo();
            Zone zone = go.GetComponentInChildren<Zone>();
            zone.transform.localPosition = zoneLocalPos;
            SerializedObject zso = new SerializedObject(zone);
            zso.FindProperty("type").enumValueIndex = isPartner ? (int)ZoneType.Partner : (int)ZoneType.Opponent;
            zso.ApplyModifiedPropertiesWithoutUndo();
            return seat;
        }

        // ---------- LevelManager.hands ----------
        private static void AssignToLevelManager()
        {
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            LevelManager lm = Object.FindAnyObjectByType<LevelManager>();
            if (lm == null) { Debug.LogError("[CT] На сцене Main нет LevelManager."); return; }
            SerializedObject so = new SerializedObject(lm);
            SerializedProperty hands = so.FindProperty("hands");
            hands.arraySize = 4;
            for (int i = 0; i < 4; i++)
                hands.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<LevelConfig>($"{Root}/Prefabs/Levels/Hand_0{i}.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        // ---------- помощники ----------
        private static Transform Child(GameObject parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static void AddSprite(GameObject go, string spritePath, int order)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/" + spritePath);
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = order;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[CT] У {target.GetType().Name} нет поля «{field}»"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
