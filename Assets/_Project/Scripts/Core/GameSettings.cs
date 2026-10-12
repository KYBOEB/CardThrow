using UnityEngine;

namespace CardThrow
{
    // Настройки правил и очков. Значения балансирует Егор прямо в ассете Data/GameSettings.asset.
    // В ScriptableObject-настройках public-поля допустимы: это чистые данные.
    [CreateAssetMenu(menuName = "CardThrow/Game Settings", fileName = "GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Подозрение")]
        [Min(1)] public int maxSuspicion = 3;
        [Tooltip("Чистый бросок (BLACKJACK) снимает столько подозрения — «восстанавливает сердце». 0 — не восстанавливать")]
        [Min(0)] public int suspicionHealOnSuccess = 1;

        [Header("Тайминги, секунды")]
        public float handIntroDuration = 1.2f;
        public float resultPopupDuration = 1.5f;
        public float timerWarningAt = 3f;
        [Tooltip("Пауза после успеха в обучении перед уровнем 1")]
        public float tutorialSuccessDelay = 2f;

        [Header("Очки")]
        public int scoreBase = 1000;
        public int scorePerSecondLeft = 50;
        public int scoreAccuracyMax = 500;
        public int scorePerRicochet = 300;
        public int scoreSpinBonus = 200;
        public int scorePerPickup = 250;
    }
}
