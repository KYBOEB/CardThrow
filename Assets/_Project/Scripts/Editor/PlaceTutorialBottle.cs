using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Обучение: поставить бутылку в Hand_00».
    // Hand_00 — файл Егора: перед запуском взять «замок» в #locks («беру Hand_00»), после пуша — «отдал».
    // Ставит экземпляр Prefabs/Obstacles/Bottle в Hand_00/Obstacles на линию броска, чуть выше оси:
    // прямой бросок её задевает (радиус бутылки 0.35 + карты 0.25 > смещения 0.35), закрутом она обходится.
    // Если бутылка (BreakableObstacle) в Hand_00 уже есть — ничего не делает. Остальной префаб не трогает.
    public static class PlaceTutorialBottle
    {
        private const string HandPath = "Assets/_Project/Prefabs/Levels/Hand_00.prefab";
        private const string BottlePath = "Assets/_Project/Prefabs/Obstacles/Bottle.prefab";
        private static readonly Vector3 BottlePosition = new Vector3(0.5f, 0.35f, 0f); // середина пути крупье (−7.6) → напарник

        [MenuItem("CardThrow/Обучение: поставить бутылку в Hand_00")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameObject bottlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottlePath);
            if (bottlePrefab == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "Не найден " + BottlePath, "OK");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(HandPath);
            try
            {
                if (root.GetComponentInChildren<BreakableObstacle>(true) != null)
                {
                    Debug.Log("[CT] В Hand_00 бутылка уже есть — ничего не меняю");
                    return;
                }

                Transform obstacles = root.transform.Find("Obstacles");
                if (obstacles == null)
                {
                    obstacles = new GameObject("Obstacles").transform;
                    obstacles.SetParent(root.transform, false);
                }

                GameObject bottle = (GameObject)PrefabUtility.InstantiatePrefab(bottlePrefab, obstacles);
                bottle.transform.localPosition = BottlePosition;

                PrefabUtility.SaveAsPrefabAsset(root, HandPath);
                Debug.Log($"[CT] Hand_00: бутылка поставлена в {BottlePosition}. Не забудь отпустить «замок» на Hand_00 после пуша.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
