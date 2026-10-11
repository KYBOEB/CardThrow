using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow
{
    // Титульный экран: логотип, 3 строки правил, большая кнопка «Играть (Space)», рекорд.
    // Виден только в GameState.Title. Space/Enter обрабатывает GameManager; кнопка — для мыши
    // (в редакторе клавиатура работает только после клика по окну Game).
    public class TitleScreen : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private UiTexts texts;

        [Header("UI")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text[] ruleLabels;      // 3 строки правил
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text playLabel;
        [SerializeField] private TMP_Text highScoreLabel;
        [SerializeField] private float fadeDuration = 0.25f;

        [Header("Кнопка «дышит», чтобы её заметили")]
        [SerializeField] private float buttonPulseAmount = 0.04f;
        [SerializeField] private float buttonPulseFrequency = 1.2f;

        private bool visible;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (texts == null) Debug.LogError("[CT] TitleScreen: не назначен UiTexts", this);
            UiAnim.SetVisible(group, false); // покажется на StateChanged(Title) из GameManager.Start
            ApplyTexts();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
        }

        // Только визуал (лёгкая пульсация кнопки), без Find/GetComponent
        private void Update()
        {
            if (!visible || playButton == null) return;
            float k = (Mathf.Sin(Time.unscaledTime * buttonPulseFrequency * 2f * Mathf.PI) + 1f) * 0.5f;
            playButton.transform.localScale = Vector3.one * (1f + buttonPulseAmount * k);
        }

        private void OnStateChanged(GameState state)
        {
            bool show = state == GameState.Title;
            if (show == visible) return;
            visible = show;

            if (show) RefreshHighScore();
            else if (playButton != null) playButton.transform.localScale = Vector3.one;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(UiAnim.Fade(group, show ? 1f : 0f, fadeDuration));
        }

        private void OnPlayClicked()
        {
            AudioManager.Play(SoundId.UiClick);
            if (GameManager.Instance != null) GameManager.Instance.StartRun();
        }

        private void ApplyTexts()
        {
            if (texts == null) return;
            if (ruleLabels != null)
                for (int i = 0; i < ruleLabels.Length; i++)
                    if (ruleLabels[i] != null)
                        ruleLabels[i].text = texts.rulesLines != null && i < texts.rulesLines.Length ? texts.rulesLines[i] : "";
            if (playLabel != null) playLabel.text = texts.playButton;
        }

        private void RefreshHighScore()
        {
            if (highScoreLabel == null || texts == null) return;
            int best = SaveSystem.HighScore;
            highScoreLabel.gameObject.SetActive(best > 0); // «Рекорд: 0» на первом запуске не показываем
            highScoreLabel.text = string.Format(texts.highScoreFormat, UiTexts.FormatNumber(best));
        }
    }
}
