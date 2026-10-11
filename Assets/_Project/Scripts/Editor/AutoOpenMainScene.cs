using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Жюри запускает игру в редакторе. Два страховочных механизма:
    // 1) Свежескачанный проект открывается с пустой Untitled-сценой (данные о последней сцене лежат
    //    в Library/, которой нет в репозитории) → один раз за сессию открываем Main.unity.
    // 2) Кнопка Play всегда стартует со сцены Main, даже если открыта другая сцена или префаб
    //    (EditorSceneManager.playModeStartScene). Разработчикам песочниц: меню CardThrow → «Play всегда из Main»
    //    снимает галку только на своём компьютере (EditorPrefs, не попадает в git).
    [InitializeOnLoad]
    public static class AutoOpenMainScene
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string SessionKey = "CT_AutoOpenMainSceneDone";
        private const string PlayFromMainPref = "CT_PlayFromMain";
        private const string MenuPath = "CardThrow/Play всегда из Main";

        static AutoOpenMainScene()
        {
            EditorApplication.delayCall += Init;
        }

        private static void Init()
        {
            ApplyPlayModeStartScene();
            TryOpenMainOnce();
        }

        private static void ApplyPlayModeStartScene()
        {
            SceneAsset main = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            bool playFromMain = EditorPrefs.GetBool(PlayFromMainPref, true);
            EditorSceneManager.playModeStartScene = playFromMain ? main : null;
        }

        private static void TryOpenMainOnce()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path)) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                Debug.LogWarning("[CT] Не найдена сцена " + MainScenePath);
                return;
            }
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        [MenuItem(MenuPath)]
        private static void TogglePlayFromMain()
        {
            EditorPrefs.SetBool(PlayFromMainPref, !EditorPrefs.GetBool(PlayFromMainPref, true));
            ApplyPlayModeStartScene();
        }

        [MenuItem(MenuPath, true)]
        private static bool TogglePlayFromMainValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PlayFromMainPref, true));
            return true;
        }
    }
}
