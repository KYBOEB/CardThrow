using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow
{
    // Экран конца забега. Один класс на два режима: в сцене два экземпляра —
    // GameOverScreen (mode = GameOver) и VictoryScreen (mode = Victory).
    // Показывает заголовок, причину/подзаголовок, итоговые очки, рекорд, «Новый рекорд!» и кнопку «Ещё раз (R)».
    // R обрабатывает GameManager; кнопка — для мыши.
    public class EndScreen : MonoBehaviour
    {
        [Header("Режим")]
        [Tooltip("GameOver или Victory — в каком состоянии игры показывать этот экран")]
        [SerializeField] private GameState mode = GameState.GameOver;

        [Header("Данные")]
        [SerializeField] private UiTexts texts;

        [Header("UI")]
        [SerializeField] private CanvasGroup group;          // корень экрана (затемнение + панель)
        [SerializeField] private RectTransform panel;        // панель, которая «выпрыгивает»
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private TMP_Text highScoreLabel;
        [SerializeField] private TMP_Text newRecordLabel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private TMP_Text playAgainLabel;

        [Header("Анимация")]
        [Tooltip("Пауза перед показом: игрок успевает увидеть, ЧТО произошло на столе (секунды реального времени)")]
        [SerializeField] private float appearDelay = 0.6f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private AnimationCurve popCurve = new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.6f, 1.1f), new Keyframe(1f, 1f));
        [SerializeField] private float popDuration = 0.3f;
        [SerializeField] private float newRecordPulseAmount = 0.12f;
        [SerializeField] private float newRecordPulseFrequency = 1.5f;

        private bool visible;
        private bool inRun;
        private int bestBeforeRun;   // рекорд на момент начала забега — от него считаем «Новый рекорд!»
        private int score;
        private ThrowOutcome lastOutcome;
        private Coroutine routine;

        private void Awake()
        {
            if (texts == null) Debug.LogError($"[CT] EndScreen ({name}): не назначен UiTexts", this);
            if (mode != GameState.GameOver && mode != GameState.Victory)
                Debug.LogError($"[CT] EndScreen ({name}): mode должен быть GameOver или Victory", this);

            bestBeforeRun = SaveSystem.HighScore;
            UiAnim.SetVisible(group, false);
            if (texts != null && playAgainLabel != null) playAgainLabel.text = texts.playAgainButton;
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.ThrowResolved += OnThrowResolved;
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.ThrowResolved -= OnThrowResolved;
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }

        // Только визуал: пульс «Новый рекорд!»
        private void Update()
        {
            if (!visible || newRecordLabel == null || !newRecordLabel.gameObject.activeSelf) return;
            float k = (Mathf.Sin(Time.unscaledTime * newRecordPulseFrequency * 2f * Mathf.PI) + 1f) * 0.5f;
            newRecordLabel.transform.localScale = Vector3.one * (1f + newRecordPulseAmount * k);
        }

        private void OnScoreChanged(int value) => score = value;
        private void OnThrowResolved(ThrowOutcome outcome, int gained) => lastOutcome = outcome;

        private void OnStateChanged(GameState state)
        {
            bool isEnd = state == GameState.GameOver || state == GameState.Victory;

            // Начало забега = первое игровое состояние после Title/GameOver/Victory.
            // Не зависит от того, в каком порядке GameManager сохраняет рекорд и шлёт StateChanged.
            if (!isEnd && state != GameState.Title && !inRun)
            {
                inRun = true;
                bestBeforeRun = SaveSystem.HighScore;
            }
            if (isEnd) inRun = false;

            if (state == mode) Show();
            else if (visible) Hide();
        }

        private void OnPlayAgainClicked()
        {
            AudioManager.Play(SoundId.UiClick);
            if (GameManager.Instance != null) GameManager.Instance.RestartRun();
        }

        // ---------- Показ ----------

        private void Show()
        {
            visible = true;
            FillTexts();
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        private void Hide()
        {
            visible = false;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(UiAnim.Fade(group, 0f, fadeDuration));
        }

        private void FillTexts()
        {
            if (texts == null) return;
            bool victory = mode == GameState.Victory;

            if (titleLabel != null) titleLabel.text = victory ? texts.victoryTitle : texts.gameOverTitle;
            if (subtitleLabel != null)
                subtitleLabel.text = victory ? texts.victorySubtitle
                    : lastOutcome == ThrowOutcome.OpponentCaught ? texts.gameOverCaughtReason
                    : texts.gameOverSuspicionReason;

            int best = Mathf.Max(SaveSystem.HighScore, score); // верно и до, и после SaveSystem.TrySetHighScore
            bool isNewRecord = score > 0 && score > bestBeforeRun;

            if (finalScoreLabel != null) finalScoreLabel.text = string.Format(texts.finalScoreFormat, UiTexts.FormatNumber(score));
            if (highScoreLabel != null) highScoreLabel.text = string.Format(texts.highScoreFormat, UiTexts.FormatNumber(best));
            if (newRecordLabel != null)
            {
                newRecordLabel.text = texts.newRecord;
                newRecordLabel.transform.localScale = Vector3.one;
                newRecordLabel.gameObject.SetActive(isNewRecord);
            }
        }

        private IEnumerator ShowRoutine()
        {
            if (group == null) yield break;
            UiAnim.SetVisible(group, false);
            for (float t = 0f; t < appearDelay; t += Time.unscaledDeltaTime) yield return null;

            UiAnim.SetVisible(group, true); // сразу кликабельно: кнопка «Ещё раз» доступна с первого кадра
            group.alpha = 0f;
            Transform p = panel != null ? panel : group.transform;

            float duration = Mathf.Max(fadeDuration, popDuration);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Clamp01(t / fadeDuration);
                p.localScale = Vector3.one * popCurve.Evaluate(Mathf.Clamp01(t / popDuration));
                yield return null;
            }
            group.alpha = 1f;
            p.localScale = Vector3.one * popCurve.Evaluate(1f);
            routine = null;
        }
    }
}
