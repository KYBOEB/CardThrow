using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow
{
    // Игровой HUD (CardThrow_CONTEXT.md 5.3): ТОЛЬКО слушает GameEvents и рисует. Ничего не решает.
    // Единственное действие — кнопка «Заново» → GameManager.Instance.RestartRun() (дублирует клавишу R).
    //
    // Скрипт висит на корне HUD, который всегда активен (иначе OnDisable отпишет события).
    // Видимость — через CanvasGroup.alpha; части, которых нет в обучении, — SetActive на дочерних объектах.
    public class HUD : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private UiTexts texts;
        [SerializeField] private GameSettings settings;

        [Header("Корень")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Уровень (верхний левый угол)")]
        [SerializeField] private TMP_Text levelLabel;
        [Tooltip("Табличка-подсказка уровня под номером уровня (не закрывает игроков). В обучении скрыта")]
        [SerializeField] private CanvasGroup levelHintGroup;
        [SerializeField] private TMP_Text levelHintLabel;

        [Header("Баннер начала раздачи (по центру)")]
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private TMP_Text bannerTitle;
        [SerializeField] private TMP_Text bannerSubtitle;

        [Header("Таймер")]
        [SerializeField] private GameObject timerRoot;
        [SerializeField] private Image timerFill;            // Image Type = Filled, Horizontal, Origin Left
        [SerializeField] private TMP_Text timerLabel;        // необязательно: секунды поверх полосы
        [Tooltip("Множитель к цвету спрайта: спрайт-заглушка уже золотой, поэтому почти белый")]
        [SerializeField] private Color timerNormalColor = new Color(1f, 0.85f, 0.35f);
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.22f, 0.18f);
        [SerializeField] private float timerPulseAmount = 0.06f;
        [SerializeField] private float timerPulseFrequency = 2f; // ударов в секунду

        [Header("Подозрение (сердца-«жизни»: промах разбивает сердце справа)")]
        [SerializeField] private GameObject suspicionRoot;
        [SerializeField] private Image[] eyes;               // 3 шт. слева направо
        [Tooltip("Целое сердце (деление свободно)")]
        [SerializeField] private Sprite eyeOpenSprite;
        [Tooltip("Разбитое сердце (подозрение +1)")]
        [SerializeField] private Sprite eyeClosedSprite;
        [SerializeField] private float eyeBounceAmount = 0.45f;
        [SerializeField] private float eyeBounceDuration = 0.35f;

        [Header("Очки")]
        [SerializeField] private GameObject scoreRoot;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private float scoreCountDuration = 0.5f;

        [Header("Карта и закрут (нижний левый угол)")]
        [SerializeField] private TMP_Text cardLabel;
        [SerializeField] private RectTransform spinGroup;    // «тычок» при смене закрута
        [SerializeField] private Image spinArrow;
        [SerializeField] private TMP_Text spinLabel;
        [Tooltip("Спрайт стрелки без поворота показывает дугу ВЛЕВО (+). Если наоборот — снять галочку")]
        [SerializeField] private bool spinSpriteCurvesLeft = true;
        [SerializeField] private float spinTiltPerStep = 15f;
        [Tooltip("Иконки закрута по силе: [0] прямо, [1..3] дуга сильнее. Рисуют дугу ВЛЕВО (+); для − зеркалим. " +
                 "Если пусто — старый режим: одна стрелка с наклоном")]
        [SerializeField] private Sprite[] spinSprites;

        [Header("Подсказка над столом (только обучение)")]
        [SerializeField] private CanvasGroup hintGroup;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private float hintFadeDuration = 0.25f;

        [Header("Низ экрана")]
        [SerializeField] private TMP_Text controlsLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartLabel;

        private const float PunchDuration = 0.18f;

        private GameState state = GameState.Title;
        private bool isTutorial;
        private bool timerWarning;
        private int lastSuspicion;
        private int shownScore;
        private int targetScore;
        private Color spinArrowBaseColor = Color.white;

        private Coroutine fadeRoutine, bannerRoutine, hintRoutine, scoreRoutine, pulseRoutine;
        private Coroutine cardPunch, spinPunch, eyeRoutine;

        // ---------- Жизненный цикл ----------

        private void Awake()
        {
            if (texts == null) Debug.LogError("[CT] HUD: не назначен UiTexts", this);
            if (settings == null) Debug.LogError("[CT] HUD: не назначен GameSettings", this);

            UiAnim.SetVisible(group, false);
            UiAnim.SetVisible(bannerGroup, false);
            UiAnim.SetVisible(hintGroup, false);
            UiAnim.SetVisible(levelHintGroup, false);
            if (spinArrow != null) spinArrowBaseColor = spinArrow.color;

            if (texts != null)
            {
                if (controlsLabel != null) controlsLabel.text = texts.controlsLine;
                if (restartLabel != null) restartLabel.text = texts.restartButton;
            }
            SetScoreText(0);
            ApplySpin(0);
            ApplyTimer(1f, 1f);
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.HandStarted += OnHandStarted;
            GameEvents.HintChanged += OnHintChanged;
            GameEvents.TimerChanged += OnTimerChanged;
            GameEvents.SuspicionChanged += OnSuspicionChanged;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.CardSelected += OnCardSelected;
            GameEvents.SpinChanged += OnSpinChanged;
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.HandStarted -= OnHandStarted;
            GameEvents.HintChanged -= OnHintChanged;
            GameEvents.TimerChanged -= OnTimerChanged;
            GameEvents.SuspicionChanged -= OnSuspicionChanged;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.CardSelected -= OnCardSelected;
            GameEvents.SpinChanged -= OnSpinChanged;
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        // ---------- События ----------

        private void OnStateChanged(GameState next)
        {
            state = next;
            bool visible = IsPlayState(next);
            Restart(ref fadeRoutine, UiAnim.Fade(group, visible ? 1f : 0f, fadeDuration));

            if (!visible)
            {
                // Вне забега подсказка и баннер не должны всплыть при следующем показе HUD
                Stop(ref hintRoutine);
                Stop(ref bannerRoutine);
                UiAnim.SetVisible(hintGroup, false);
                UiAnim.SetVisible(bannerGroup, false);
                UiAnim.SetVisible(levelHintGroup, false);
            }
        }

        private void OnHandStarted(int levelNumber, int totalLevels, string title)
        {
            GameManager gm = GameManager.Instance;
            isTutorial = gm != null ? gm.IsTutorial : levelNumber == 0;

            // В обучении нет таймера, подозрения и очков — и HUD их не показывает
            SetActive(timerRoot, !isTutorial);
            SetActive(suspicionRoot, !isTutorial);
            SetActive(scoreRoot, !isTutorial);

            SetTimerWarning(false);
            ApplyTimer(1f, 1f);

            // Подсказки: в обучении — крупно над столом (ведёт TutorialController), на уровнях — табличка слева.
            // Новая раздача — старая подсказка гаснет; свою подсказку GameManager пришлёт сразу после HandStarted.
            UiAnim.SetVisible(levelHintGroup, false);
            if (!isTutorial)
            {
                Stop(ref hintRoutine);
                UiAnim.SetVisible(hintGroup, false);
            }

            if (texts == null) return;
            // «Уровень 2» без «/3»: игрок не видит, сколько осталось (UiTexts.levelFormat = «Уровень {0}»)
            string levelShort = isTutorial ? texts.tutorialLabel : string.Format(texts.levelFormat, levelNumber, totalLevels); // {1} в формате не используем, но передаём — старый текст «{0}/{1}» не сломается
            bool hasTitle = !isTutorial && !string.IsNullOrEmpty(title);

            if (levelLabel != null) levelLabel.text = levelShort;

            if (bannerTitle != null) bannerTitle.text = levelShort;
            if (bannerSubtitle != null) bannerSubtitle.text = hasTitle ? title : "";
            Restart(ref bannerRoutine, BannerRoutine());
        }

        private void OnHintChanged(string text)
        {
            if (!isTutorial)
            {
                SetLevelHint(text);
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                if (hintGroup != null) hintGroup.transform.localScale = Vector3.one; // если прервали «тычок»
                Restart(ref hintRoutine, UiAnim.Fade(hintGroup, 0f, hintFadeDuration));
                return;
            }

            if (hintLabel != null) hintLabel.text = text;
            if (hintGroup == null) return;

            bool wasVisible = hintGroup.alpha > 0.5f;
            if (wasVisible)
            {
                UiAnim.SetVisible(hintGroup, true); // могла гаснуть — возвращаем полную яркость
                Restart(ref hintRoutine, UiAnim.Punch(hintGroup.transform, 0.06f, PunchDuration)); // новая подсказка — «тычок»
            }
            else Restart(ref hintRoutine, FadeInHint());
        }

        // Табличка слева не мешает игре — висит всю раздачу (в отличие от подсказки над столом)
        private void SetLevelHint(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (levelHintLabel != null && show) levelHintLabel.text = text;
            UiAnim.SetVisible(levelHintGroup, show);
        }

        private void OnTimerChanged(float remaining, float total)
        {
            ApplyTimer(remaining, total);
            bool warning = settings != null && remaining <= settings.timerWarningAt;
            if (warning != timerWarning) SetTimerWarning(warning);
        }

        private void OnSuspicionChanged(int current, int max)
        {
            if (eyes == null) return;
            int n = eyes.Length;
            for (int i = 0; i < n; i++)
            {
                if (eyes[i] == null) continue;
                eyes[i].sprite = i >= n - current ? eyeClosedSprite : eyeOpenSprite; // разбиваются справа налево
                eyes[i].transform.localScale = Vector3.one;
            }

            // «Подпрыгивает» только что разбитое сердце; сброс в 0 — без анимации
            int broken = n - current;
            if (current > lastSuspicion && broken >= 0 && broken < n && eyes[broken] != null)
                Restart(ref eyeRoutine, UiAnim.Punch(eyes[broken].transform, eyeBounceAmount, eyeBounceDuration));
            lastSuspicion = current;
        }

        private void OnScoreChanged(int score)
        {
            targetScore = score;
            if (score <= shownScore) // рестарт забега: без досчёта вниз
            {
                Stop(ref scoreRoutine);
                SetScoreText(score);
                return;
            }
            Restart(ref scoreRoutine, CountScore());
        }

        private void OnCardSelected(int index, int value)
        {
            if (cardLabel == null || texts == null) return;
            cardLabel.text = string.Format(texts.cardFormat, value);
            if (state == GameState.Aiming) // реакция на нажатие A/D, а не на раздачу при загрузке уровня
                Restart(ref cardPunch, UiAnim.Punch(cardLabel.transform, 0.2f, PunchDuration));
        }

        private void OnSpinChanged(int step)
        {
            ApplySpin(step);
            if (spinGroup != null) Restart(ref spinPunch, UiAnim.Punch(spinGroup, 0.15f, PunchDuration));
        }

        private void OnRestartClicked()
        {
            AudioManager.Play(SoundId.UiClick);
            if (GameManager.Instance != null) GameManager.Instance.RestartRun();
        }

        // ---------- Визуал ----------

        private static bool IsPlayState(GameState s) =>
            s == GameState.HandIntro || s == GameState.Aiming || s == GameState.Flying ||
            s == GameState.Resolving || s == GameState.HandResult;

        private void ApplyTimer(float remaining, float total)
        {
            if (timerFill != null) timerFill.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
            if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        }

        private void SetTimerWarning(bool on)
        {
            timerWarning = on;
            if (timerFill != null) timerFill.color = on ? timerWarningColor : timerNormalColor;
            if (on) Restart(ref pulseRoutine, PulseTimer());
            else
            {
                Stop(ref pulseRoutine);
                if (timerRoot != null) timerRoot.transform.localScale = Vector3.one;
            }
        }

        // Пульс, пока идёт предупреждение; во время полёта (таймер стоит) — без пульса
        private IEnumerator PulseTimer()
        {
            Transform t = timerRoot != null ? timerRoot.transform : null;
            if (t == null) yield break;

            float time = 0f;
            while (timerWarning)
            {
                time += Time.unscaledDeltaTime;
                float k = state == GameState.Aiming ? Mathf.Abs(Mathf.Sin(time * timerPulseFrequency * Mathf.PI)) : 0f;
                t.localScale = Vector3.one * (1f + timerPulseAmount * k);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private void ApplySpin(int step)
        {
            if (spinLabel != null && texts != null)
            {
                string signed = step > 0 ? "+" + step : step.ToString(); // ASCII-минус: есть в любом шрифте
                spinLabel.text = string.Format(texts.spinFormat, signed);
            }

            if (spinArrow == null) return;

            // Набор иконок: сила закрута видна по изгибу дуги, направление — по зеркалу
            if (spinSprites != null && spinSprites.Length > 0)
            {
                int i = Mathf.Min(System.Math.Abs(step), spinSprites.Length - 1);
                if (spinSprites[i] != null) spinArrow.sprite = spinSprites[i];
                float flip = (step < 0 ? -1f : 1f) * (spinSpriteCurvesLeft ? 1f : -1f);
                spinArrow.rectTransform.localScale = new Vector3(flip, 1f, 1f);
                spinArrow.rectTransform.localRotation = Quaternion.identity;
                spinArrow.color = spinArrowBaseColor;
                return;
            }

            // Знак закрута → зеркало по X; величина → наклон и размер. При 0 стрелка полупрозрачная
            float mirror = (step < 0 ? -1f : 1f) * (spinSpriteCurvesLeft ? 1f : -1f);
            float size = 0.8f + 0.1f * Mathf.Abs(step);
            RectTransform rt = spinArrow.rectTransform;
            rt.localScale = new Vector3(mirror * size, size, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, step * spinTiltPerStep); // + = против часовой = влево

            Color c = spinArrowBaseColor;
            c.a = step == 0 ? 0.35f : spinArrowBaseColor.a;
            spinArrow.color = c;
        }

        private IEnumerator CountScore()
        {
            int from = shownScore;
            for (float t = 0f; t < scoreCountDuration; t += Time.unscaledDeltaTime)
            {
                float k = 1f - (1f - t / scoreCountDuration) * (1f - t / scoreCountDuration); // ease-out
                SetScoreText(Mathf.RoundToInt(Mathf.Lerp(from, targetScore, k)));
                yield return null;
            }
            SetScoreText(targetScore);
            if (scoreLabel != null) yield return UiAnim.Punch(scoreLabel.transform, 0.15f, PunchDuration);
            scoreRoutine = null;
        }

        private void SetScoreText(int value)
        {
            shownScore = value;
            if (scoreLabel != null && texts != null) scoreLabel.text = string.Format(texts.scoreFormat, UiTexts.FormatNumber(value));
        }

        private IEnumerator BannerRoutine()
        {
            if (bannerGroup == null) yield break;
            float total = settings != null ? settings.handIntroDuration : 1.2f;
            const float fadeIn = 0.15f, fadeOut = 0.25f;

            bannerGroup.transform.localScale = Vector3.one * 0.8f;
            UiAnim.SetVisible(bannerGroup, true);
            bannerGroup.alpha = 0f;

            for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeIn;
                bannerGroup.alpha = k;
                bannerGroup.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, k);
                yield return null;
            }
            bannerGroup.alpha = 1f;
            bannerGroup.transform.localScale = Vector3.one;

            float hold = Mathf.Max(0f, total - fadeIn - fadeOut);
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;

            yield return UiAnim.Fade(bannerGroup, 0f, fadeOut);
            bannerRoutine = null;
        }

        private IEnumerator FadeInHint()
        {
            if (hintGroup == null) yield break;
            hintGroup.transform.localScale = Vector3.one;
            yield return UiAnim.Fade(hintGroup, 1f, hintFadeDuration);
        }

        // ---------- Вспомогательное ----------

        private void Restart(ref Coroutine handle, IEnumerator routine)
        {
            if (handle != null) StopCoroutine(handle);
            handle = StartCoroutine(routine);
        }

        private void Stop(ref Coroutine handle)
        {
            if (handle != null) StopCoroutine(handle);
            handle = null;
        }

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value) go.SetActive(value);
        }
    }
}
