using UnityEngine;

namespace CardThrow
{
    // Рекорд хранится в PlayerPrefs.
    public static class SaveSystem
    {
        private const string HighScoreKey = "CT_HighScore";

        public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

        // true, если это новый рекорд
        public static bool TrySetHighScore(int score)
        {
            if (score <= HighScore) return false;
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
