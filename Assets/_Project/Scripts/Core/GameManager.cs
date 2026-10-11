using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardThrow
{
    // Единственный, кто меняет состояние игры и решает исход броска (CardThrow_CONTEXT.md, разделы 5.3, 5.4).
    // Остальные системы узнают о происходящем только через GameEvents.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Ссылки (проставлены в Main.unity)")]
        [SerializeField] private GameSettings settings;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private DealerHand dealerHand;
        [SerializeField] private AimController aimController;
        [SerializeField] private CardFlight cardFlight;

        public GameState State { get; private set; } = GameState.Title;
        public LevelConfig CurrentLevel { get; private set; }
        public bool IsTutorial => CurrentLevel != null && CurrentLevel.IsTutorial;

        private const int FirstRealLevel = 1; // уровень 0 — обучение

        // Текущая раздача
        private int currentIndex = -1;
        private Seat partnerSeat;
        private readonly List<Seat> opponents = new List<Seat>();

        // Таймер «окна внимания»
        private float timeRemaining;
        private bool warningFired;
        private float secondsLeftAtThrow;

        // Забег
        private int score;
        private int suspicion;
        private bool tutorialDone;          // поле экземпляра: живёт ровно до выхода из Play
        private ThrowOutcome lastOutcome;

        private bool IsInRun => State != GameState.Title && State != GameState.GameOver && State != GameState.Victory;

        // ---------- Жизненный цикл ----------

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (settings == null || levelManager == null || dealerHand == null || aimController == null || cardFlight == null)
            {
                Debug.LogError("[CT] GameManager: не заполнены ссылки в инспекторе (settings, levelManager, dealerHand, aimController, cardFlight)", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.CardThrown += OnCardThrown;
            GameEvents.CardFlightFinished += OnCardFlightFinished;
        }

        private void OnDisable()
        {
            GameEvents.CardThrown -= OnCardThrown;
            GameEvents.CardFlightFinished -= OnCardFlightFinished;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Все OnEnable (подписки UI) уже выполнены → экраны получат StateChanged(Title)
            SetState(GameState.Title);
        }

        private void Update()
        {
            HandleHotkeys();

            if (State == GameState.Aiming && !IsTutorial)
                TickTimer();
        }

        // ---------- Публичный API ----------

        public void StartRun()
        {
            if (!isActiveAndEnabled) return;
            CancelPending();
            ResetRunStats();

            int first = tutorialDone ? FirstRealLevel : 0;
            if (first >= levelManager.HandCount) first = 0;
            LoadLevel(first, false);
        }

        public void RestartRun()
        {
            if (!isActiveAndEnabled) return;
            Debug.Log("[CT] RestartRun");
            StartRun(); // внутри StopAllCoroutines + сброс очков и подозрения
        }

        public void SkipTutorial()
        {
            if (!isActiveAndEnabled || !IsTutorial || !IsInRun) return;
            Debug.Log("[CT] SkipTutorial");
            CancelPending();
            tutorialDone = true;
            LoadLevel(FirstRealLevel, false);
        }

        public void DebugLoadHand(int index)
        {
            if (!isActiveAndEnabled) return;
            if (index < 0 || index >= levelManager.HandCount)
            {
                Debug.LogWarning($"[CT] DebugLoadHand: нет уровня {index} (всего {levelManager.HandCount})");
                return;
            }
            Debug.Log($"[CT] DebugLoadHand({index})");
            CancelPending();
            ResetRunStats();
            LoadLevel(index, false);
        }

        // ---------- Машина состояний ----------

        // Единственное место смены состояния.
        private void SetState(GameState next)
        {
            GameState prev = State;
            State = next;
            Debug.Log($"[CT] State: {prev} -> {next}");
            GameEvents.RaiseStateChanged(next);

            // Инвариант: ввод игрока включён только в Aiming
            if (next != GameState.Aiming) SetInputEnabled(false);

            switch (next)
            {
                case GameState.Title:
                    break;

                case GameState.HandIntro:
                    StartCoroutine(EnterStateAfter(settings.handIntroDuration, GameState.Aiming));
                    break;

                case GameState.Aiming:
                    foreach (Seat seat in opponents) seat.SetLookingAway(true);
                    SetInputEnabled(true);
                    break;

                case GameState.Flying:
                case GameState.Resolving:
                    break;

                case GameState.HandResult:
                    StartCoroutine(HandResultRoutine());
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    SaveSystem.TrySetHighScore(score);
                    break;
            }
        }

        private IEnumerator EnterStateAfter(float seconds, GameState next)
        {
            yield return new WaitForSecondsRealtime(seconds); // не зависит от slow-mo
            SetState(next);
        }

        private IEnumerator HandResultRoutine()
        {
            bool tutorialSuccess = IsTutorial && lastOutcome == ThrowOutcome.Success;
            float wait = tutorialSuccess ? settings.tutorialSuccessDelay : settings.resultPopupDuration;
            yield return new WaitForSecondsRealtime(wait);
            AdvanceAfterResult();
        }

        private void AdvanceAfterResult()
        {
            if (lastOutcome != ThrowOutcome.Success)
            {
                LoadLevel(currentIndex, true); // переигровка: уровень пересоздаётся, выбор карты сохраняется
                return;
            }

            if (IsTutorial) tutorialDone = true;

            int next = currentIndex + 1;
            if (next < levelManager.HandCount) LoadLevel(next, false);
            else SetState(GameState.Victory);
        }

        // ---------- Старт уровня (порядок из 5.4) ----------

        private void LoadLevel(int index, bool isRetry)
        {
            int prevSelected = dealerHand.SelectedIndex;

            LevelConfig level = levelManager.LoadHand(index);
            if (level == null)
            {
                CurrentLevel = null;
                SetState(GameState.Title); // LevelManager уже написал ошибку
                return;
            }

            currentIndex = index;
            CurrentLevel = level;
            CacheSeats(level);

            dealerHand.SetCards(level.CardValues, isRetry ? prevSelected : -1);
            cardFlight.ResetCard();
            aimController.PreviewFraction = level.PreviewFraction;
            if (partnerSeat != null) partnerSeat.SetScore(level.PartnerStartScore);

            timeRemaining = level.TimeLimit;
            warningFired = false;
            secondsLeftAtThrow = 0f;

            GameEvents.RaiseHandStarted(index, levelManager.HandCount - 1, level.HandTitle);
            GameEvents.RaiseHintChanged(level.Hint);
            if (!level.IsTutorial) GameEvents.RaiseTimerChanged(timeRemaining, level.TimeLimit); // полная полоса на баннере

            SetState(GameState.HandIntro);
        }

        // Один раз при загрузке уровня (не в Update): напарник + список соперников этого префаба
        private void CacheSeats(LevelConfig level)
        {
            opponents.Clear();
            partnerSeat = level.PartnerSeat;

            foreach (Seat seat in level.GetComponentsInChildren<Seat>(true))
            {
                if (seat == partnerSeat) continue;
                if (seat.IsPartner)
                {
                    if (partnerSeat == null) partnerSeat = seat; // запасной вариант, если поле PartnerSeat не заполнено
                    continue;
                }
                opponents.Add(seat);
            }

            if (partnerSeat == null)
                Debug.LogWarning($"[CT] {level.name}: не найден Seat напарника (LevelConfig.PartnerSeat)", level);
        }

        // ---------- Таймер ----------

        private void TickTimer()
        {
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            GameEvents.RaiseTimerChanged(timeRemaining, CurrentLevel.TimeLimit);

            if (!warningFired && timeRemaining <= settings.timerWarningAt)
            {
                warningFired = true;
                foreach (Seat seat in opponents) seat.SetLookingAway(false, timeRemaining); // повернутся ровно к концу окна
                AudioManager.Play(SoundId.TimerWarning);
            }

            if (timeRemaining <= 0f)
            {
                secondsLeftAtThrow = 0f;
                SetState(GameState.Resolving);
                FinishThrow(ThrowOutcome.TimeOut, 0);
            }
        }

        // ---------- События ----------

        private void OnCardThrown(ThrowParams p)
        {
            if (State != GameState.Aiming) return;
            secondsLeftAtThrow = IsTutorial ? 0f : timeRemaining;
            SetState(GameState.Flying); // ввод (aim + hand) выключается при входе в любое состояние, кроме Aiming
        }

        private void OnCardFlightFinished(FlightReport report)
        {
            if (State != GameState.Flying) return; // устаревший отчёт (например, после рестарта) — игнорируем
            SetState(GameState.Resolving);
            Resolve(report);
        }

        // ---------- Исход броска ----------

        private void Resolve(FlightReport report)
        {
            ThrowOutcome outcome;
            switch (report.result)
            {
                case FlightResult.LandedInPartnerZone:
                    outcome = CurrentLevel.PartnerStartScore + report.cardValue == 21 ? ThrowOutcome.Success : ThrowOutcome.WrongValue;
                    break;
                case FlightResult.HitBreakable: outcome = ThrowOutcome.HitBreakable; break;
                case FlightResult.EnteredOpponentZone: outcome = ThrowOutcome.OpponentCaught; break;
                case FlightResult.OffTable: outcome = ThrowOutcome.OffTable; break;
                default: outcome = ThrowOutcome.StoppedOnTable; break;
            }

            int gained = 0;
            if (outcome == ThrowOutcome.Success)
            {
                if (partnerSeat != null) partnerSeat.SetScore(21);
                if (!IsTutorial)
                {
                    gained = CalculateScore(report);
                    score += gained;
                    GameEvents.RaiseScoreChanged(score);
                }
            }
            else if (outcome == ThrowOutcome.WrongValue)
            {
                if (partnerSeat != null) partnerSeat.SetScore(CurrentLevel.PartnerStartScore + report.cardValue);
            }

            FinishThrow(outcome, gained);
        }

        // Формула из раздела 2, все константы — в GameSettings
        private int CalculateScore(FlightReport r)
        {
            int seconds = Mathf.FloorToInt(secondsLeftAtThrow);
            int accuracy = Mathf.RoundToInt(settings.scoreAccuracyMax * Mathf.Clamp01(r.zoneAccuracy01));
            int spin = r.spinStep != 0 ? settings.scoreSpinBonus : 0;

            return settings.scoreBase
                 + seconds * settings.scorePerSecondLeft
                 + accuracy
                 + r.ricochets * settings.scorePerRicochet
                 + spin
                 + r.bonusPickups * settings.scorePerPickup;
        }

        // Общий хвост для броска и для TimeOut: событие → подозрение → следующее состояние
        private void FinishThrow(ThrowOutcome outcome, int gained)
        {
            lastOutcome = outcome;
            Debug.Log($"[CT] Outcome: {outcome} (+{gained})");
            GameEvents.RaiseThrowResolved(outcome, gained);

            if (outcome == ThrowOutcome.Success || IsTutorial) // в обучении провал без штрафа
            {
                SetState(GameState.HandResult);
                return;
            }

            if (outcome == ThrowOutcome.OpponentCaught) // «Шулер!» — мгновенное поражение
            {
                SetState(GameState.GameOver);
                return;
            }

            suspicion = Mathf.Min(suspicion + 1, settings.maxSuspicion);
            GameEvents.RaiseSuspicionChanged(suspicion, settings.maxSuspicion);
            SetState(suspicion >= settings.maxSuspicion ? GameState.GameOver : GameState.HandResult);
        }

        // ---------- Вспомогательное ----------

        private void ResetRunStats()
        {
            score = 0;
            suspicion = 0;
            GameEvents.RaiseScoreChanged(score);
            GameEvents.RaiseSuspicionChanged(suspicion, settings.maxSuspicion);
        }

        // Отменяет все отложенные переходы и возвращает нормальное время (на случай рестарта во время slow-mo)
        private void CancelPending()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
        }

        private void SetInputEnabled(bool value)
        {
            aimController.SetEnabled(value);
            dealerHand.SetInteractable(value);
        }

        private void HandleHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.rKey.wasPressedThisFrame) { RestartRun(); return; }
            if (kb.tabKey.wasPressedThisFrame && IsTutorial && IsInRun) { SkipTutorial(); return; }

#if UNITY_EDITOR
            if (kb.shiftKey.isPressed)
            {
                if (kb.digit0Key.wasPressedThisFrame) DebugLoadHand(0);
                else if (kb.digit1Key.wasPressedThisFrame) DebugLoadHand(1);
                else if (kb.digit2Key.wasPressedThisFrame) DebugLoadHand(2);
                else if (kb.digit3Key.wasPressedThisFrame) DebugLoadHand(3);
            }
#endif
        }
    }
}
