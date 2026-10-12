using UnityEngine;

namespace CardThrow
{
    // Мост «игровые события → общие звуки» (CardThrow_CONTEXT.md 5.3). Только слушает GameEvents
    // и вызывает AudioManager.Play / PlayLoop. Звуки разрушения (бутылка, домик) играют сами
    // препятствия — здесь Breakable намеренно не озвучиваем, иначе будет двойной звук.
    public class SoundEventsBridge : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;     // timerWarningAt — с какой секунды тикать
        [Tooltip("Глушить музыку на экранах GameOver/Victory, чтобы был слышен джингл. Вернётся на Title/Aiming")]
        [SerializeField] private bool stopMusicOnEnd = true;

        private GameState state = GameState.Title;
        private float lastPower;
        private int lastTickSecond = -1;

        private void Awake()
        {
            if (settings == null) Debug.LogError("[CT] SoundEventsBridge: не назначен GameSettings", this);
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.CardSelected += OnCardSelected;
            GameEvents.AimChanged += OnAimChanged;
            GameEvents.CardThrown += OnCardThrown;
            GameEvents.CardHitObstacle += OnCardHitObstacle;
            GameEvents.ThrowResolved += OnThrowResolved;
            GameEvents.TimerChanged += OnTimerChanged;
            GameEvents.HandStarted += OnHandStarted;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.CardSelected -= OnCardSelected;
            GameEvents.AimChanged -= OnAimChanged;
            GameEvents.CardThrown -= OnCardThrown;
            GameEvents.CardHitObstacle -= OnCardHitObstacle;
            GameEvents.ThrowResolved -= OnThrowResolved;
            GameEvents.TimerChanged -= OnTimerChanged;
            GameEvents.HandStarted -= OnHandStarted;
        }

        private void OnStateChanged(GameState next)
        {
            state = next;
            switch (next)
            {
                case GameState.Title:
                case GameState.Aiming:
                    // PlayLoop не перезапускает уже играющий луп → безопасно звать на каждом Aiming
                    AudioManager.PlayLoop(SoundId.MusicLoop);
                    AudioManager.PlayLoop(SoundId.AmbienceLoop);
                    break;
                case GameState.GameOver:
                    if (stopMusicOnEnd) AudioManager.StopLoop(SoundId.MusicLoop);
                    AudioManager.Play(SoundId.GameOver);
                    break;
                case GameState.Victory:
                    if (stopMusicOnEnd) AudioManager.StopLoop(SoundId.MusicLoop);
                    AudioManager.Play(SoundId.Victory);
                    break;
            }
            if (next != GameState.Aiming) lastPower = 0f;
        }

        // DealerHand шлёт CardSelected и при раздаче (SetCards) — озвучиваем только нажатия игрока
        private void OnCardSelected(int index, int value)
        {
            if (state == GameState.Aiming) AudioManager.Play(SoundId.CardSelect);
        }

        // «Щипок» карты — только в момент начала прицеливания (0 → >0), а не каждый кадр оттяга
        private void OnAimChanged(float power01)
        {
            if (lastPower <= 0f && power01 > 0f) AudioManager.Play(SoundId.CardPinch);
            lastPower = power01;
        }

        private void OnCardThrown(ThrowParams p)
        {
            lastPower = 0f;
            AudioManager.Play(SoundId.CardThrow);
        }

        private void OnCardHitObstacle(Vector2 point, ObstacleKind kind)
        {
            switch (kind)
            {
                case ObstacleKind.Ricochet: AudioManager.Play(SoundId.GlassClink); break;
                // Pushable и Breakable: звук играет само препятствие (PushableObstacle / BreakableObstacle) —
                // здесь не дублируем (QA/Triage, Артас 12.10: фишки звучали дважды)
            }
        }

        private void OnThrowResolved(ThrowOutcome outcome, int gained)
        {
            switch (outcome)
            {
                case ThrowOutcome.Success:
                    AudioManager.Play(SoundId.Success);
                    AudioManager.Play(SoundId.CoinRain);
                    break;
                case ThrowOutcome.OpponentCaught:
                    AudioManager.Play(SoundId.Caught);
                    break;
                default:
                    AudioManager.Play(SoundId.CrowdAngry);
                    break;
            }
        }

        private void OnHandStarted(int levelNumber, int totalLevels, string title) => lastTickSecond = -1;

        // Тик раз в целую секунду на последних timerWarningAt секундах: 3, 2, 1.
        // Сам момент входа в предупреждение озвучивает GameManager (TimerWarning).
        private void OnTimerChanged(float remaining, float total)
        {
            if (settings == null) return;
            if (remaining > settings.timerWarningAt) { lastTickSecond = -1; return; } // ещё рано / новая раздача
            if (remaining <= 0f) return;                                             // время вышло — звучит исход

            int second = Mathf.CeilToInt(remaining);
            if (second == lastTickSecond) return;
            lastTickSecond = second;
            AudioManager.Play(SoundId.TimerTick);
        }
    }
}
