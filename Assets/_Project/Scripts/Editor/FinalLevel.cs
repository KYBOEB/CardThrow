using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Уровни: баланс для жюри (превью, время, финал Hand_05)».  Плейтест 12.10, 21:15.
    //
    // 1) Превью и время. Сложность росла сразу по трём осям: меньше удачных бросков (0.39% → 0.11%),
    //    короче видимая траектория (100% → 45%) и меньше времени (25 → 15 с). Короткое превью бьёт сильнее всего:
    //    игрок не видит, где карта остановится, и бросает вслепую. Жюри играет впервые — смягчаем превью и время,
    //    а препятствия (сама задача уровня) не трогаем:
    //       превью 100 / 80 / 70 / 65 / 60 %,  время 25 / 22 / 20 / 20 / 18 с.
    // 2) Финал Hand_05 (модель полёта: 0.08% → ≈0.19%, между Ур.3 и Ур.4; плюс движение бутылок и стаканов):
    //    • зона напарника 1.6×1.4 (была 0.9×0.7), внешний край 8.6 — ровно до бортика;
    //    • второй стакан (0, −2.9), карточный домик (−5.5, 2.8), фишки (2.6, 1.5) — места подобраны перебором
    //      так, чтобы не перекрыть ни один из путей закрутом;
    //    • WaiterRush: официантка Кирилла (×0.9), до 2 за раз (стаканов теперь 2).
    // Повторный запуск безопасен. Hand_04 — файл Егора, WaiterRush — Алексея: «замок» в #locks.
    public static class FinalLevel
    {
        private const string Levels = "Assets/_Project/Prefabs/Levels/";
        private const string Obstacles = "Assets/_Project/Prefabs/Obstacles/";
        private const string WaitressPath = "Assets/_Project/Art/Sprites/Characters/waitress.png";

        private static readonly (string hand, float preview, float time)[] Balance =
        {
            ("Hand_01", 1.00f, 25f),
            ("Hand_02", 0.80f, 22f),
            ("Hand_03", 0.70f, 20f),
            ("Hand_04", 0.65f, 20f),
            ("Hand_05", 0.60f, 18f),
        };

        [MenuItem("CardThrow/Уровни: баланс для жюри (превью, время, финал Hand_05)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            foreach (var b in Balance)
                Edit(b.hand, root =>
                {
                    LevelConfig level = root.GetComponent<LevelConfig>();
                    if (level == null) return;
                    SerializedObject so = new SerializedObject(level);
                    so.FindProperty("previewFraction").floatValue = b.preview;
                    so.FindProperty("timeLimit").floatValue = b.time;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    if (b.hand == "Hand_05") BuildFinal(root, level);
                    Debug.Log($"[CT] {b.hand}: превью {b.preview:P0}, время {b.time} с");
                });

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Баланс применён. Дальше: «Уровни: проверить проходимость» (Hand_05 ≈ 0.19%).");
        }

        private static void BuildFinal(GameObject root, LevelConfig level)
        {
            Zone zone = level.PartnerSeat != null ? level.PartnerSeat.Zone : null;
            BoxCollider2D box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
            if (box != null)
            {
                zone.transform.localPosition = new Vector3(-0.1f, 0f, zone.transform.localPosition.z);
                box.offset = Vector2.zero;
                box.size = new Vector2(1.6f, 1.4f);
            }
            else Debug.LogWarning("[CT] Hand_05: не нашёл зону напарника (LevelConfig.PartnerSeat)");

            Transform parent = root.transform.Find("Obstacles");
            if (parent == null) parent = root.transform;
            EnsureCount(root, parent, "Glass", new[] { new Vector2(3.5f, -2.2f), new Vector2(0f, -2.9f) });
            EnsureCount(root, parent, "CardHouse", new[] { new Vector2(-5.5f, 2.8f) });
            EnsureCount(root, parent, "Chips", new[] { new Vector2(2.6f, 1.5f) });

            WaiterRush rush = root.GetComponent<WaiterRush>();
            if (rush == null) rush = root.AddComponent<WaiterRush>();
            SerializedObject so = new SerializedObject(rush);
            Set(so, "waiterSprite", AssetDatabase.LoadAssetAtPath<Sprite>(WaitressPath));
            SetFloat(so, "waiterScale", 0.9f);
            SerializedProperty max = so.FindProperty("maxPerWave");
            if (max != null) max.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Довести число экземпляров префаба до нужного: недостающие ставим в заданные точки, лишние не трогаем
        private static void EnsureCount(GameObject root, Transform parent, string prefabName, Vector2[] spots)
        {
            List<Transform> have = FindInstances(root, prefabName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Obstacles + prefabName + ".prefab");
            if (prefab == null) { Debug.LogError($"[CT] Нет префаба {prefabName}"); return; }
            for (int i = have.Count; i < spots.Length; i++)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.name = i == 0 ? prefabName : $"{prefabName} ({i})";
                go.transform.localPosition = spots[i];
                Debug.Log($"[CT] Hand_05: + {prefabName} в ({spots[i].x}, {spots[i].y})");
            }
        }

        private static List<Transform> FindInstances(GameObject root, string prefabName)
        {
            List<Transform> result = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (source != null && source.name == prefabName) result.Add(t);
            }
            return result;
        }

        private static void Edit(string hand, System.Action<GameObject> change)
        {
            string path = Levels + hand + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning($"[CT] Нет {path}"); return; }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                change(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null) p.floatValue = value;
        }
    }
}
