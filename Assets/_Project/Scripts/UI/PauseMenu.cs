using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CardThrow
{
    // Пауза: Esc (если игрок не тянет карту — тогда Esc отменяет прицел, как раньше) или P.
    // Останавливает время (Time.timeScale = 0 → стоят таймер раздачи, полёт карты, анимации), глушит звуки
    // (музыка играет дальше — см. AudioManager), выключает прицел и выбор карты через AimController.SetEnabled / DealerHand.SetInteractable.
    // В меню: «Продолжить», «Выйти из игры» (GameQuit) и ползунок громкости музыки. «Заново» — клавиша R (её ловит GameManager).
    // Работает на unscaled-времени, потому что во время паузы timeScale = 0.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Громкость музыки 0…1")]
        [SerializeField] private Slider musicSlider;
        [Tooltip("Кнопка «Выйти» на титульном экране (необязательно)")]
        [SerializeField] private Button titleQuitButton;

        private AimController aim;
        private DealerHand dealer;
        private bool paused;
        private bool playerAiming;      // игрок тянет карту — Esc принадлежит прицелу
        private int aimEndFrame = -1;   // кадр, в котором прицел сняли (тем же Esc) — паузу в этом кадре не открываем
        private float savedTimeScale = 1f;

        public bool IsPaused => paused;

        private void Awake()
        {
            aim = FindAnyObjectByType<AimController>();
            dealer = FindAnyObjectByType<DealerHand>();
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume);
            Show(false);
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.AimChanged += OnAimChanged;
            Hook(resumeButton, Resume);
            Hook(quitButton, GameQuit.Quit);
            Hook(titleQuitButton, GameQuit.Quit);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(AudioManager.SetMusicVolume);
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.AimChanged -= OnAimChanged;
            Unhook(resumeButton, Resume);
            Unhook(quitButton, GameQuit.Quit);
            Unhook(titleQuitButton, GameQuit.Quit);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(AudioManager.SetMusicVolume);
            if (paused) Resume(); // не оставляем игру замороженной при выходе из Play
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            bool toggle = kb.pKey.wasPressedThisFrame || (kb.escapeKey.wasPressedThisFrame && !playerAiming && Time.frameCount != aimEndFrame);
            if (!toggle) return;
            if (paused) Resume();
            else if (CanPause()) Pause();
        }

        private void LateUpdate()
        {
            // Стоп-кадр FeedbackManager мог вернуть timeScale — пауза главнее
            if (paused && Time.timeScale != 0f) Time.timeScale = 0f;
        }

        private static bool CanPause()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return false;
            GameState s = gm.State;
            return s != GameState.Title && s != GameState.GameOver && s != GameState.Victory;
        }

        public void Pause()
        {
            if (paused || !CanPause()) return;
            paused = true;
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (aim != null) aim.SetEnabled(false);
            if (dealer != null) dealer.SetInteractable(false);
            Show(true);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = savedTimeScale;
            AudioListener.pause = false;
            PlayerPrefs.Save(); // громкость музыки — на диск один раз, а не на каждый сдвиг ползунка
            bool aiming = GameManager.Instance != null && GameManager.Instance.State == GameState.Aiming;
            if (aim != null) aim.SetEnabled(aiming);
            if (dealer != null) dealer.SetInteractable(aiming);
            Show(false);
        }

        // Смена состояния во время паузы (R, конец раздачи) — снимаем паузу, иначе корутины GameManager встанут
        private void OnStateChanged(GameState state)
        {
            if (paused) Resume();
        }

        private void OnAimChanged(float power01)
        {
            bool now = power01 > 0f;
            if (playerAiming && !now) aimEndFrame = Time.frameCount;
            playerAiming = now;
        }

        private void Show(bool value)
        {
            if (group == null) return;
            group.alpha = value ? 1f : 0f;
            group.interactable = value;
            group.blocksRaycasts = value;
        }

        private static void Hook(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.AddListener(a); }
        private static void Unhook(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.RemoveListener(a); }
    }
}
