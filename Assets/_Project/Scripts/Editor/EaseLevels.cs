using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Уровни: облегчить Стену (Hand_03), Бутылку (Hand_04) и стенку стаканов (Hand_00c)».  Плейтест 12.10.
    //   Hand_03 «Стена»: два домика → один, по центру перед напарником (5.2, 0); зона напарника 1.2×0.85 → 1.2×1.1.
    //                    Модель полёта: 0.13% → 0.21% удачных бросков (между Ур.2 0.24% и Ур.4) — кривая сложности ровная.
    //   Hand_04 (Егор):  три домика → два: убирается верхний, центральный остаётся (прямой бросок по-прежнему закрыт).
    //   Hand_00c (Егор): три стакана стопкой (шаг 0.34 при диаметре 0.9 — картинки налезают) → два крайних.
    //                    Модель полёта: 0.27% → 0.28% — стенка работает так же, средний стакан ничего не добавлял.
    // Повторный запуск безопасен: если домиков уже столько, сколько нужно, ничего не удаляется.
    // Hand_04 и Hand_00c — файлы Егора: перед запуском «замок» в #locks.
    public static class EaseLevels
    {
        private const string Levels = "Assets/_Project/Prefabs/Levels/";
        private const string HousePrefab = "CardHouse";
        private const string WallHint = "Домик перед напарником — обойди его дугой. Фишки тормозят карту";

        [MenuItem("CardThrow/Уровни: облегчить Стену (Hand_03), Бутылку (Hand_04) и стаканы (Hand_00c)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            Edit("Hand_03", root =>
            {
                List<Transform> houses = FindHouses(root);
                if (houses.Count == 0) { Debug.LogWarning("[CT] Hand_03: домиков не найдено"); return; }
                houses[0].localPosition = new Vector3(5.2f, 0f, houses[0].localPosition.z);
                for (int i = houses.Count - 1; i >= 1; i--) Object.DestroyImmediate(houses[i].gameObject);

                LevelConfig level = root.GetComponent<LevelConfig>();
                if (level == null) return;
                SerializedObject so = new SerializedObject(level);
                so.FindProperty("hint").stringValue = WallHint;
                so.ApplyModifiedPropertiesWithoutUndo();

                Zone zone = level.PartnerSeat != null ? level.PartnerSeat.Zone : null;
                BoxCollider2D box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
                if (box != null) box.size = new Vector2(1.2f, 1.1f);
                Debug.Log($"[CT] Hand_03: домиков {houses.Count} → 1 (по центру), зона напарника 1.2×1.1");
            });

            Edit("Hand_04", root =>
            {
                List<Transform> houses = FindHouses(root);
                if (houses.Count < 3) { Debug.Log($"[CT] Hand_04: домиков уже {houses.Count} — не трогаю"); return; }
                Transform top = houses[0];
                foreach (Transform h in houses) if (h.localPosition.y > top.localPosition.y) top = h;
                Object.DestroyImmediate(top.gameObject);
                Debug.Log($"[CT] Hand_04: домиков {houses.Count} → {houses.Count - 1} (убран верхний)");
            });

            Edit("Hand_00c", root =>
            {
                List<Transform> glasses = FindPrefabInstances(root, "Glass");
                if (glasses.Count < 3) { Debug.Log($"[CT] Hand_00c: стаканов уже {glasses.Count} — не трогаю"); return; }
                // Оставляем самый нижний и самый верхний — стенка той же высоты, без наложения картинок
                glasses.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
                for (int i = glasses.Count - 2; i >= 1; i--) Object.DestroyImmediate(glasses[i].gameObject);
                Debug.Log($"[CT] Hand_00c: стаканов {glasses.Count} → 2 (крайние)");
            });

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Готово. Дальше: «Уровни: проверить проходимость» — Ур.3 должен стать ≈0.2%.");
        }

        private static void Edit(string file, System.Action<GameObject> change)
        {
            string path = Levels + file + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogError($"[CT] Нет {path}"); return; }
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

        // Экземпляры префаба CardHouse внутри раздачи (по исходному префабу, запасной вариант — по имени)
        private static List<Transform> FindHouses(GameObject root) => FindPrefabInstances(root, HousePrefab);

        private static List<Transform> FindPrefabInstances(GameObject root, string prefabName)
        {
            List<Transform> result = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                string n = source != null ? source.name : t.name;
                if (n == prefabName || t.name.StartsWith(prefabName)) result.Add(t);
            }
            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }
    }
}
