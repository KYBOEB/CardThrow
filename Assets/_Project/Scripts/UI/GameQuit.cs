using UnityEngine;

namespace CardThrow
{
    // Выход из игры: в редакторе останавливает Play, в сборке закрывает приложение.
    public static class GameQuit
    {
        public static void Quit()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
