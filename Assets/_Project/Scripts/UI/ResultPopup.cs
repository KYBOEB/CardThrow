using System.Collections;
using TMPro;
using UnityEngine;

namespace CardThrow
{
    // Итог броска по центру экрана: крупный заголовок + подзаголовок из UiTexts, «+N» при успехе.
    // Появление: масштаб 0.6 → 1.1 → 1 за 0.25 с (кривая popCurve), затем держится и гаснет.
    // Всё на unscaled time: slow-mo на успехе (FeedbackManager) не замедляет попап.
    public class ResultPopup : MonoBehaviour
    {
        private const int Blackjack = 21;

        [Header("Данные")]
        [SerializeField] private UiTexts texts;
        [SerializeField] private GameSettings settings;

        [Header("UI")]
        [SerializeField] private CanvasGroup group;          // на этом же объекте; его же масштабируем
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text gainedLabel;       // «+1 850»

        [Header("Цвета")]
        [SerializeField] private Color successColor = new Color(1f, 0.78f, 0.2f);  // золото
        [SerializeField] private Color failColor = new Color(0.95f, 0.25f, 0.22f); // красный

        [Header("Анимация")]
        [SerializeField] private AnimationCurve popCurve = new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.6f, 1.1f), new Keyframe(1f, 1f));
        [SerializeField] private float popDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [Tooltip("Показывать попап и в обучении (подсказку о причине там даёт TutorialController)")]
        [SerializeField] private bool showInTutorial = true;

        private int lastCardValue;
        private Coroutine routine;

        private void Awake()
        {
            if (texts == null) Debug.LogError("[CT] ResultPopup: не назначен UiTexts", this);
            HideNow();
        }

        private void OnEnable()
        {
            GameEvents.CardThrown += OnCardThrown;
            GameEvents.ThrowResolved += OnThrowResolved;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.CardThrown -= OnCardThrown;
            GameEvents.ThrowResolved -= OnThrowResolved;
            GameEvents.StateChanged -= OnStateChanged;
        }

        // Номинал берём из CardThrown: он всегда приходит раньше ThrowResolved
        // (а CardFlightFinished у разных подписчиков может прийти и после ThrowResolved).
        private void OnCardThrown(ThrowParams p) => lastCardValue = p.cardValue;

        private void OnThrowResolved(ThrowOutcome outcome, int scoreGained)
        {
            if (texts == null) return;
            GameManager gm = GameManager.Instance;
            if (!showInTutorial && gm != null && gm.IsTutorial) return;

            bool success = outcome == ThrowOutcome.Success;
            Color color = success ? successColor : failColor;

            if (titleLabel != null)
            {
                titleLabel.text = outcome == ThrowOutcome.WrongValue ? WrongValueTitle(gm) : texts.GetTitle(outcome);
                titleLabel.color = color;
            }
            if (subtitleLabel != null) subtitleLabel.text = texts.GetSubtitle(outcome);
            if (gainedLabel != null)
            {
                bool showGained = success && scoreGained > 0; // в обучении очков нет → «+0» не показываем
                gainedLabel.gameObject.SetActive(showGained);
                gainedLabel.text = string.Format(texts.scoreGainedFormat, UiTexts.FormatNumber(scoreGained));
                gainedLabel.color = successColor;
            }

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        // Новая раздача / рестарт / титул — убрать сразу. GameOver не трогаем:
        // экран поражения появится поверх, а попап «Шулер!» успеет прочитаться под ним.
        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Title || state == GameState.HandIntro) HideNow();
        }

        // «Перебор!» / «Недобор!» — по сумме у напарника (CONTEXT, таблица исходов)
        private string WrongValueTitle(GameManager gm)
        {
            if (gm == null || gm.CurrentLevel == null) return texts.GetTitle(ThrowOutcome.WrongValue);
            int sum = gm.CurrentLevel.PartnerStartScore + lastCardValue;
            if (sum > Blackjack) return texts.wrongValueBustTitle;
            if (sum < Blackjack) return texts.wrongValueShortTitle;
            return texts.GetTitle(ThrowOutcome.WrongValue);
        }

        private IEnumerator ShowRoutine()
        {
            if (group == null) yield break;
            Transform t = group.transform;
            UiAnim.SetVisible(group, true);
            group.blocksRaycasts = false; // попап никогда не перехватывает клики (бросок, кнопки HUD)

            // Появление: масштаб по кривой, прозрачность — за первые 40% времени
            for (float time = 0f; time < popDuration; time += Time.unscaledDeltaTime)
            {
                float k = time / popDuration;
                t.localScale = Vector3.one * popCurve.Evaluate(k);
                group.alpha = Mathf.Clamp01(k / 0.4f);
                yield return null;
            }
            t.localScale = Vector3.one * popCurve.Evaluate(1f);
            group.alpha = 1f;

            // Держим до конца HandResult (GameSettings.resultPopupDuration), минус анимации
            float total = settings != null ? settings.resultPopupDuration : 1.5f;
            float hold = Mathf.Max(0.3f, total - popDuration - fadeOutDuration);
            for (float time = 0f; time < hold; time += Time.unscaledDeltaTime) yield return null;

            // Исчезновение: гаснет и чуть уменьшается
            for (float time = 0f; time < fadeOutDuration; time += Time.unscaledDeltaTime)
            {
                float k = time / fadeOutDuration;
                group.alpha = 1f - k;
                t.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, k);
                yield return null;
            }
            routine = null; // корутина завершается сама — не останавливаем её изнутри
            HideNow();
        }

        private void HideNow()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            UiAnim.SetVisible(group, false);
            if (group != null) group.transform.localScale = Vector3.one;
        }
    }
}
