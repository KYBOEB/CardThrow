using UnityEngine;

namespace CardThrow
{
    // Все тексты интерфейса (CardThrow_CONTEXT.md, 5.3). Ассет: Data/UiTexts.asset, значения заполняет Егор.
    // Как и остальные SO-настройки, хранит чистые данные → public-поля допустимы.
    // Секции для HUD / попапов / экранов конца добавляются сюда же отдельными промптами.
    [CreateAssetMenu(menuName = "CardThrow/Ui Texts", fileName = "UiTexts")]
    public class UiTexts : ScriptableObject
    {
        public const string PartnerScoreToken = "{score}"; // заменяется на стартовый счёт напарника

        [Header("Обучение — шаги (ровно 3)")]
        [Tooltip("0: выбор карты (можно {score}), 1: закрут, 2: бросок")]
        [TextArea(2, 3)]
        public string[] tutorialSteps =
        {
            "Напарник в золоте. У него {score} — дай ему 21: выбери карту клавишами A / D",
            "Бутылку задевать нельзя. Закрути карту: колесо мыши или Q / E",
            "Зажми ЛКМ и тяни назад, как рогатку. Точки — путь карты. Отпусти!"
        };

        [Header("Обучение — итог броска")]
        [TextArea(1, 2)] public string tutorialSuccess = "Отлично! Дальше — на время, пока соперники отвернулись";
        [TextArea(1, 2)] public string tutorialFailUndershoot = "Недолёт! Тяни сильнее";
        [TextArea(1, 2)] public string tutorialFailOvershoot = "Перелёт! Тяни чуть слабее";
        [TextArea(1, 2)] public string tutorialFailSideways = "Мимо! Поправь прицел или закрут";
        [TextArea(1, 2)] public string tutorialFailOffTable = "Слишком сильно — карта улетела со стола";
        [TextArea(1, 2)] public string tutorialFailBreakable = "Бутылка! Закрути сильнее";
        [TextArea(1, 2)] public string tutorialFailOpponent = "Чужие карты трогать нельзя!";
        [TextArea(1, 2)] public string tutorialFailWrongValue = "Нужна карта, дающая ровно 21";

        [Header("Обучение — прочее")]
        public string tutorialSkipHint = "Tab — пропустить";

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tutorialSteps == null || tutorialSteps.Length != 3)
                Debug.LogWarning("[CT] UiTexts: tutorialSteps должно содержать ровно 3 строки", this);
        }
#endif
    }
}
