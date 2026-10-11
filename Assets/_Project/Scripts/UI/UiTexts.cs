using UnityEngine;

namespace CardThrow
{
    // Все тексты интерфейса (CardThrow_CONTEXT.md, 5.3). Ассет: Data/UiTexts.asset, значения заполняет Егор.
    // Как и остальные SO-настройки, хранит чистые данные → public-поля допустимы.
    // ВАЖНО: дефолты ниже применяются только к НОВЫМ полям уже существующего ассета (Unity дописывает
    // отсутствующие поля значениями по умолчанию при загрузке). Уже сохранённые тексты обучения не меняются.
    [CreateAssetMenu(menuName = "CardThrow/Ui Texts", fileName = "UiTexts")]
    public class UiTexts : ScriptableObject
    {
        public const string PartnerScoreToken = "{score}"; // заменяется на стартовый счёт напарника

        // ===================== ОБУЧЕНИЕ (D1b, не менять имена полей) =====================

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

        // ===================== ТИТУЛЬНЫЙ ЭКРАН =====================

        [Header("Титульный экран")]
        [Tooltip("Ровно 3 строки правил — читаются за 10 секунд")]
        [TextArea(1, 2)]
        public string[] rulesLines =
        {
            "Ты — крупье-шулер. Пока игроки отвернулись, метни карту напарнику в золоте.",
            "У него должно стать ровно 21. Бутылки и чужие карты не трогай.",
            "Три промаха — и охрана выведет тебя из казино."
        };
        public string playButton = "Играть (Space)";
        [Tooltip("{0} — рекорд")]
        public string highScoreFormat = "Рекорд: {0}";

        // ===================== HUD =====================

        [Header("HUD")]
        [Tooltip("Нижняя строка управления — видна всю игру")]
        [TextArea(1, 2)]
        public string controlsLine = "A / D — карта   •   ЛКМ: тянуть назад и отпустить — бросок   •   Колесо или Q / E — закрут   •   ПКМ / Esc — отмена   •   R — заново";
        public string tutorialLabel = "Обучение";
        [Tooltip("{0} — номер уровня, {1} — всего уровней")]
        public string levelFormat = "Уровень {0}/{1}";
        [Tooltip("{0} — номер, {1} — всего, {2} — название раздачи")]
        public string levelWithTitleFormat = "Уровень {0}/{1} — {2}";
        [Tooltip("{0} — номинал выбранной карты")]
        public string cardFormat = "Карта: {0}";
        [Tooltip("{0} — закрут со знаком: 0, +2, −1")]
        public string spinFormat = "Закрут {0}";
        [Tooltip("{0} — очки")]
        public string scoreFormat = "Выигрыш: {0}";
        [Tooltip("{0} — очки за бросок")]
        public string scoreGainedFormat = "+{0}";
        public string restartButton = "Заново (R)";

        // ===================== ИТОГ БРОСКА (ResultPopup) =====================
        // Для каждого ThrowOutcome — варианты заголовка и подзаголовка, выбирается случайный.

        [Header("Итог: Success")]
        public string[] successTitles = { "BLACKJACK!", "Двадцать одно!", "Чистая работа!" };
        public string[] successSubtitles = { "Напарник даже не моргнул", "Ловкость рук — и никакого мошенничества", "Казино ничего не заметило" };

        [Header("Итог: WrongValue")]
        [Tooltip("Если сумма у напарника > 21 (ResultPopup выбирает сам)")]
        public string wrongValueBustTitle = "Перебор!";
        [Tooltip("Если сумма у напарника < 21")]
        public string wrongValueShortTitle = "Недобор!";
        [Tooltip("Запасные заголовки, если сумму посчитать не удалось")]
        public string[] wrongValueTitles = { "Не та карта!" };
        public string[] wrongValueSubtitles = { "Считать надо до двадцати одного", "Напарник смотрит на тебя с укором" };

        [Header("Итог: HitBreakable")]
        public string[] hitBreakableTitles = { "Дзынь!", "Вдребезги!" };
        public string[] hitBreakableSubtitles = { "Весь стол обернулся на звон", "Эта бутылка стоила дороже карты" };

        [Header("Итог: OpponentCaught")]
        public string[] opponentCaughtTitles = { "Шулер!", "Попался!" };
        public string[] opponentCaughtSubtitles = { "Карта прилетела не тому игроку" };

        [Header("Итог: OffTable")]
        public string[] offTableTitles = { "Мимо стола!", "Улетела!" };
        public string[] offTableSubtitles = { "Карта на полу — гости переглядываются", "Слишком сильно, маэстро" };

        [Header("Итог: StoppedOnTable")]
        public string[] stoppedOnTableTitles = { "Не долетела!", "Застряла!" };
        public string[] stoppedOnTableSubtitles = { "Карта лежит у всех на виду", "Чуть точнее — и никто бы не понял" };

        [Header("Итог: TimeOut")]
        public string[] timeOutTitles = { "Вас заметили!", "Время вышло!" };
        public string[] timeOutSubtitles = { "Соперники повернулись раньше броска", "Шулер, который медлит, — уже не шулер" };

        // ===================== ЭКРАНЫ КОНЦА =====================

        [Header("Экран поражения (GameOver)")]
        public string gameOverTitle = "Вас раскрыли!";
        [Tooltip("Причина: карта влетела к сопернику")]
        public string gameOverCaughtReason = "Карта ушла чужому игроку — охрана уже идёт";
        [Tooltip("Причина: подозрение достигло максимума")]
        public string gameOverSuspicionReason = "Слишком много промахов — вас вычислили";

        [Header("Экран победы (Victory)")]
        public string victoryTitle = "Смена окончена";
        public string victorySubtitle = "Казино так ничего и не поняло";

        [Header("Экраны конца — общее")]
        [Tooltip("{0} — итоговые очки")]
        public string finalScoreFormat = "Выигрыш: {0}";
        public string newRecord = "Новый рекорд!";
        public string playAgainButton = "Ещё раз (R)";

        // ===================== API =====================

        // 12450 → «12 450». Обычный пробел (а не неразрывный) — точно есть в шрифте
        public static string FormatNumber(int value) =>
            value.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        public string GetTitle(ThrowOutcome o) => Pick(TitlesFor(o));
        public string GetSubtitle(ThrowOutcome o) => Pick(SubtitlesFor(o));

        private string[] TitlesFor(ThrowOutcome o)
        {
            switch (o)
            {
                case ThrowOutcome.Success: return successTitles;
                case ThrowOutcome.WrongValue: return wrongValueTitles;
                case ThrowOutcome.HitBreakable: return hitBreakableTitles;
                case ThrowOutcome.OpponentCaught: return opponentCaughtTitles;
                case ThrowOutcome.OffTable: return offTableTitles;
                case ThrowOutcome.StoppedOnTable: return stoppedOnTableTitles;
                case ThrowOutcome.TimeOut: return timeOutTitles;
                default: return null;
            }
        }

        private string[] SubtitlesFor(ThrowOutcome o)
        {
            switch (o)
            {
                case ThrowOutcome.Success: return successSubtitles;
                case ThrowOutcome.WrongValue: return wrongValueSubtitles;
                case ThrowOutcome.HitBreakable: return hitBreakableSubtitles;
                case ThrowOutcome.OpponentCaught: return opponentCaughtSubtitles;
                case ThrowOutcome.OffTable: return offTableSubtitles;
                case ThrowOutcome.StoppedOnTable: return stoppedOnTableSubtitles;
                case ThrowOutcome.TimeOut: return timeOutSubtitles;
                default: return null;
            }
        }

        // Пустой массив → пустая строка (UI просто ничего не покажет), без исключений
        private static string Pick(string[] variants)
        {
            if (variants == null || variants.Length == 0) return "";
            return variants[Random.Range(0, variants.Length)];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tutorialSteps == null || tutorialSteps.Length != 3)
                Debug.LogWarning("[CT] UiTexts: tutorialSteps должно содержать ровно 3 строки", this);
            if (rulesLines == null || rulesLines.Length != 3)
                Debug.LogWarning("[CT] UiTexts: rulesLines должно содержать ровно 3 строки", this);

            foreach (ThrowOutcome o in System.Enum.GetValues(typeof(ThrowOutcome)))
            {
                string[] titles = TitlesFor(o);
                if (titles == null || titles.Length == 0)
                    Debug.LogWarning($"[CT] UiTexts: нет ни одного заголовка для исхода {o}", this);
            }
        }
#endif
    }
}
