using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Уровни: финал (Hand_05) — зона напарника и официантки со стаканами».  Плейтест 12.10.
    //   • Зона напарника 0.9×0.7 → 1.2×1.0 и без захода на бортик (центр x = 7.9, внешний край 8.5 < 8.6).
    //     Сложность финала даёт не крошечная цель, а движение: бутылки (BottleToss) + официантки переставляют стаканы.
    //   • WaiterRush (Алексей) на корне раздачи: официантка Кирилла (waitress.png, масштаб 0.9 — как у Seat),
    //     раз в 7 с переносит стакан на новое место. Ищет стаканы среди детей раздачи (RicochetObstacle).
    // Повторный запуск безопасен. Hand_05 — «замок» в #locks; настройки WaiterRush дальше крутит Алексей в инспекторе.
    public static class FinalLevel
    {
        private const string HandPath = "Assets/_Project/Prefabs/Levels/Hand_05.prefab";
        private const string WaitressPath = "Assets/_Project/Art/Sprites/Characters/waitress.png";

        [MenuItem("CardThrow/Уровни: финал (Hand_05) — зона напарника и официантки со стаканами")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(HandPath);
            try
            {
                LevelConfig level = root.GetComponent<LevelConfig>();
                Zone zone = level != null && level.PartnerSeat != null ? level.PartnerSeat.Zone : null;
                BoxCollider2D box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
                if (box != null)
                {
                    zone.transform.localPosition = new Vector3(0f, 0f, zone.transform.localPosition.z);
                    box.offset = Vector2.zero;
                    box.size = new Vector2(1.2f, 1.0f);
                }
                else Debug.LogWarning("[CT] Hand_05: не нашёл зону напарника (LevelConfig.PartnerSeat)");

                WaiterRush rush = root.GetComponent<WaiterRush>();
                if (rush == null) rush = root.AddComponent<WaiterRush>();
                SerializedObject so = new SerializedObject(rush);
                SerializedProperty sprite = so.FindProperty("waiterSprite");
                if (sprite != null) sprite.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(WaitressPath);
                SerializedProperty scale = so.FindProperty("waiterScale");
                if (scale != null) scale.floatValue = 0.9f;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (root.GetComponentsInChildren<RicochetObstacle>(true).Length == 0)
                    Debug.LogWarning("[CT] Hand_05: нет стаканов — официанткам нечего переставлять");

                PrefabUtility.SaveAsPrefabAsset(root, HandPath);
                Debug.Log("[CT] Hand_05: зона напарника 1.2×1.0, WaiterRush с официанткой (масштаб 0.9). Проверь: Shift+5 в Play.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
