using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню CardThrow → «Уровни: обновить список раздач».
    // Заполняет LevelManager.hands в открытой сцене Main всеми префабами Prefabs/Levels/Hand_*.prefab:
    // сначала раздачи обучения (галочка Is Tutorial), потом обычные — каждая группа по имени
    // (Hand_00, Hand_00b, Hand_00c, затем Hand_01, Hand_02 …). Hand_Template и префабы без LevelConfig пропускаются.
    // Новая раздача попадает в игру одним пунктом меню, без ручного перетаскивания в инспекторе. Сцена сохраняется.
    public static class UpdateHandList
    {
        private const string LevelsDir = "Assets/_Project/Prefabs/Levels";
        private const string NamePrefix = "Hand_";
        private const string TemplateName = "Hand_Template";

        [MenuItem("CardThrow/Уровни: обновить список раздач")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            LevelManager manager = Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "В открытой сцене нет LevelManager. Открой Scenes/Main.unity.", "OK");
                return;
            }

            List<LevelConfig> tutorials = new List<LevelConfig>();
            List<LevelConfig> levels = new List<LevelConfig>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LevelsDir }))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go == null || !go.name.StartsWith(NamePrefix) || go.name == TemplateName) continue;
                LevelConfig level = go.GetComponent<LevelConfig>();
                if (level == null) continue;
                (level.IsTutorial ? tutorials : levels).Add(level);
            }
            tutorials.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            List<LevelConfig> all = new List<LevelConfig>(tutorials);
            all.AddRange(levels);
            if (all.Count == 0)
            {
                EditorUtility.DisplayDialog("CardThrow", "Не найдено ни одной раздачи Hand_* с LevelConfig в " + LevelsDir, "OK");
                return;
            }

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty hands = so.FindProperty("hands");
            if (hands == null)
            {
                Debug.LogError("[CT] У LevelManager нет поля «hands» — LevelManager.cs изменился?");
                return;
            }
            hands.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++) hands.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedProperties(); // с Undo: можно откатить Ctrl+Z

            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);

            string order = string.Join(", ", all.ConvertAll(l => l.name + (l.IsTutorial ? $" (обучение: {l.TutorialStage})" : "")));
            Debug.Log($"[CT] LevelManager: {all.Count} раздач — {order}. Сцена сохранена.");
        }
    }
}
