using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Камера: место под игроков за столом».
    // Ставит CameraFitter.contentSize = (24, 15): стол 19×9.5 + место для фигур игроков за бортиком
    // и для строк HUD сверху/снизу. Сохраняет сцену. Ничего больше не трогает.
    public static class FitCameraForSeats
    {
        private static readonly Vector2 ContentSize = new Vector2(24f, 15f);

        [MenuItem("CardThrow/Камера: место под игроков за столом")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            CameraFitter fitter = Object.FindAnyObjectByType<CameraFitter>(FindObjectsInactive.Include);
            if (fitter == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "В открытой сцене нет CameraFitter. Открой Scenes/Main.unity.", "OK");
                return;
            }

            SerializedObject so = new SerializedObject(fitter);
            SerializedProperty size = so.FindProperty("contentSize");
            if (size == null) { Debug.LogError("[CT] У CameraFitter нет поля contentSize"); return; }
            size.vector2Value = ContentSize;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(fitter.gameObject.scene);
            EditorSceneManager.SaveScene(fitter.gameObject.scene);
            Debug.Log($"[CT] CameraFitter.contentSize = {ContentSize}, сцена сохранена");
        }
    }
}
