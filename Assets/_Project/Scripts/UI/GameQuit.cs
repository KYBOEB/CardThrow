using UnityEngine;

namespace CardThrow
{
    // Выход из игры. Жюри запускает игру в редакторе — там «выход» = остановить Play.
    // В сборке — закрыть приложение. UnityEditor упомянут только внутри #if UNITY_EDITOR (CONTEXT §6).
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
