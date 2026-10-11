using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Собрать LevelEditEnv (стол для Prefab Mode)».
    // Делает копию Main только со столом, крупье, камерой и светом и назначает её как
    // Project Settings → Editor → Prefab Editing Environments → Regular Environment.
    public static class BuildLevelEditEnv
    {
        private const string MainPath = "Assets/_Project/Scenes/Main.unity";
        private const string EnvPath = "Assets/_Project/Scenes/Sandbox/LevelEditEnv.unity";

        [MenuItem("CardThrow/Собрать LevelEditEnv (стол для Prefab Mode)")]
        public static void Build()
        {
            if (!File.Exists(MainPath)) { Debug.LogError("[CT] Нет Main.unity — сначала соберите сцену."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene main = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(main, EnvPath, true);          // копия, Main не трогаем

            Scene env = EditorSceneManager.OpenScene(EnvPath, OpenSceneMode.Single);
            foreach (GameObject go in env.GetRootGameObjects())
            {
                bool keep = go.name == "Main Camera" || go.name == "Table" || go.name == "Dealer" || go.name.StartsWith("Global Light");
                if (!keep) Object.DestroyImmediate(go);
            }
            EditorSceneManager.SaveScene(env);

            EditorSettings.prefabRegularEnvironment = AssetDatabase.LoadAssetAtPath<SceneAsset>(EnvPath);
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);   // возвращаемся в Main
            Debug.Log("[CT] LevelEditEnv создан и назначен как Regular Environment.");
        }
    }
}
