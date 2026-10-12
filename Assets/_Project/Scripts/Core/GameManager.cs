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
        // Текущая раздача — последняя часть обучения (после неё начинается игра на время)
        public bool IsLastTutorialHand => IsTutorial && currentIndex + 1 >= levelManager.TutorialCount;

        private const int TutorialLevel = 0;   // индекс первой раздачи обучения
        private const int Blackjack = 21;

        // Первая обычная раздача = число раздач обучения в начале списка (Hand_00, Hand_00b, Hand_00c → 3)
        private int FirstRealLevel => Mathf.Min(levelManager.TutorialCount, Mathf.Max(0, levelManager.HandCount - 1));

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
        private bool tutorialDone;          // поле экземпляра: сбрасывается при каждом новом запуске Play
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
                enabled = false; // Start/Update/OnEnable не вызовутся — вместо потока NullReference одна понятная ошибка
                return;
            }

            // Частая ошибка: в поле перетащили префаб из окна Project, а не объект из Hierarchy.
            // Тогда игра вызывает методы у файла-префаба, который не живёт в сцене, и ввод «молча» не работает.
            if (!IsSceneObject(dealerHand) || !IsSceneObject(aimController) || !IsSceneObject(cardFlight))
            {
                Debug.LogError("[CT] GameManager: поля Dealer Hand / Aim Controller / Card Flight указывают на ПРЕФАБ в окне Project. " +
                               "Перетащи объекты Dealer и Card из окна Hierarchy.", this);
                enabled = false;
            }
        }

        private static bool IsSceneObject(Component c) => c.gameObject.scene.IsValid();

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
            LoadBackgroundLevel();
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

            score = 0;
            suspicion = 0;
            GameEvents.RaiseScoreChanged(score);
            GameEvents.RaiseSuspicionChanged(suspicion, settings.maxSuspicion);

            int first = tutorialDone ? FirstRealLevel : TutorialLevel;
            if (first >= levelManager.HandCount) first = TutorialLevel;
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

        // Отладка (Shift+0 — обучение, Shift+1..5 — уровень N): сбрасывает подозрение, очки не трогает.
        // index — индекс в списке LevelManager (с учётом раздач обучения)
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
            suspicion = 0;
            GameEvents.RaiseSuspicionChanged(suspicion, settings.maxSuspicion);
            LoadLevel(index, false);
        }

        // ---------- Машина состояний ----------

        // Единственное место смены состояния.
        private void SetState(GameState next)
        {
            GameState prev = State;
            State = next;
            Debug.Log(prev == next ? $"[CT] State: {next}" : $"[CT] State: {prev} -> {next}");
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

            int next = currentIndex + 1;
            if (next < levelManager.HandCount) LoadLevel(next, false);
            else SetState(GameState.Victory);
        }

        // ---------- Загрузка уровня ----------

        // Порядок из 5.4
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
            if (!level.IsTutorial) tutorialDone = true; // дошли до обычной раздачи (успехом, Tab или отладкой) — обучение пройдено

            dealerHand.SetCards(level.CardValues, isRetry ? prevSelected : -1);
            cardFlight.ResetCard();
            aimController.PreviewFraction = level.PreviewFraction;
            if (!isRetry) aimController.ResetSpin(); // новый уровень — дуга 0; переигровка — дуга как была
            if (partnerSeat != null) partnerSeat.SetScore(level.PartnerStartScore);

            timeRemaining = level.TimeLimit;
            warningFired = false;
            secondsLeftAtThrow = 0f;

            // Контракт HandStarted: 0 = обучение (все его части), дальше «Уровень 1…N» без учёта раздач обучения
            int tutorials = levelManager.TutorialCount;
            int levelNumber = level.IsTutorial ? 0 : index - tutorials + 1;
            GameEvents.RaiseHandStarted(levelNumber, levelManager.HandCount - tutorials, level.HandTitle);
            GameEvents.RaiseHintChanged(level.Hint);
            if (!level.IsTutorial) GameEvents.RaiseTimerChanged(timeRemaining, level.TimeLimit); // полная полоса на баннере

            SetState(GameState.HandIntro);
        }

        // Фон под титульным экраном: стол уровня 1 без баннера, подсказки и таймера; ввод выключен
        private void LoadBackgroundLevel()
        {
            int index = levelManager.HandCount > FirstRealLevel ? FirstRealLevel : TutorialLevel;
            LevelConfig level = levelManager.LoadHand(index);
            if (level == null) return;

            currentIndex = index;
            CurrentLevel = level;
            CacheSeats(level);

            dealerHand.SetCards(level.CardValues);
            cardFlight.ResetCard();
            if (partnerSeat != null) partnerSeat.SetScore(level.PartnerStartScore);
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
                    outcome = CurrentLevel.PartnerStartScore + report.cardValue == Blackjack ? ThrowOutcome.Success : ThrowOutcome.WrongValue;
                    break;
                case FlightResult.HitBreakable: outcome = ThrowOutcome.HitBreakable; break;
                case FlightResult.EnteredOpponentZone: outcome = ThrowOutcome.OpponentCaught; break;
                case FlightResult.OffTable: outcome = ThrowOutcome.OffTable; break;
                default: outcome = ThrowOutcome.StoppedOnTable; break;
            }

            int gained = 0;
            if (outcome == ThrowOutcome.Success)
            {
                if (partnerSeat != null) partnerSeat.SetScore(Blackjack);
                if (!IsTutorial)
                {
                    gained = CalculateScore(report);
                    score += gained;
                    GameEvents.RaiseScoreChanged(score);

                    // Награда за чистую раздачу: одно сердце назад. Ошибка на уровне 1 не обрекает весь забег,
                    // но три промаха подряд по-прежнему — поражение (плейтест 12.10)
                    if (suspicion > 0 && settings.suspicionHealOnSuccess > 0)
                    {
                        suspicion = Mathf.Max(0, suspicion - settings.suspicionHealOnSuccess);
                        GameEvents.RaiseSuspicionChanged(suspicion, settings.maxSuspicion);
                    }
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

            if (outcome == ThrowOutcome.Success || IsTutorial) // в обучении любой провал — без штрафа и без GameOver
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

        // ---------- Горячие клавиши ----------
        // В редакторе Input System получает клавиатуру только когда окно Game в фокусе,
        // поэтому всё, что здесь есть, дублируется кнопками UI (StartRun/RestartRun).

        private void HandleHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

#if UNITY_EDITOR
            if (kb.shiftKey.isPressed && HandleDebugHotkeys(kb)) return;
#endif

            if (State == GameState.Title)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    StartRun();
                return; // R и Tab на титульном экране не работают
            }

            if (kb.rKey.wasPressedThisFrame) { RestartRun(); return; }
            if (kb.tabKey.wasPressedThisFrame) SkipTutorial(); // внутри проверка: только во время обучения
        }

#if UNITY_EDITOR
        private bool HandleDebugHotkeys(Keyboard kb)
        {
            if (kb.digit0Key.wasPressedThisFrame) { DebugLoadHand(TutorialLevel); return true; }
            if (kb.digit1Key.wasPressedThisFrame) { DebugLoadLevelNumber(1); return true; }
            if (kb.digit2Key.wasPressedThisFrame) { DebugLoadLevelNumber(2); return true; }
            if (kb.digit3Key.wasPressedThisFrame) { DebugLoadLevelNumber(3); return true; }
            if (kb.digit4Key.wasPressedThisFrame) { DebugLoadLevelNumber(4); return true; }
            if (kb.digit5Key.wasPressedThisFrame) { DebugLoadLevelNumber(5); return true; }
            if (kb.sKey.wasPressedThisFrame) { DebugFakeFlight(true); return true; }
            if (kb.fKey.wasPressedThisFrame) { DebugFakeFlight(false); return true; }
            return false;
        }

        // Shift+N → «Уровень N» как в HUD (раздачи обучения не считаются)
        private void DebugLoadLevelNumber(int number) => DebugLoadHand(levelManager.TutorialCount + number - 1);

        // Shift+S — «карта легла к напарнику, номинал правильный»; Shift+F — «задела бутылку».
        // Идёт через те же события, что и настоящий бросок, поэтому проверяет весь конвейер: UI, обучение, звук.
        private void DebugFakeFlight(bool success)
        {
            if (CurrentLevel == null || (State != GameState.Aiming && State != GameState.Flying)) return;

            int correct = CurrentLevel.CorrectCardIndex;
            int value = success
                ? (correct >= 0 ? CurrentLevel.CardValues[correct] : Blackjack - CurrentLevel.PartnerStartScore)
                : dealerHand.SelectedValue;

            Debug.Log($"[CT] DEBUG fake flight: {(success ? "Success" : "HitBreakable")}");

            if (State == GameState.Aiming)
            {
                GameEvents.RaiseCardThrown(new ThrowParams
                {
                    origin = Vector2.zero,
                    direction = Vector2.up,
                    power01 = 0.5f,
                    spinStep = 0,
                    cardValue = value
                }); // → OnCardThrown → Flying
            }
            else
            {
                cardFlight.ResetCard(); // настоящий полёт идёт — останавливаем, чтобы не пришёл второй отчёт
            }

            Vector2 finalPos = (success && partnerSeat != null && partnerSeat.Zone != null) ? partnerSeat.Zone.Center : Vector2.zero;
            GameEvents.RaiseCardFlightFinished(new FlightReport
            {
                result = success ? FlightResult.LandedInPartnerZone : FlightResult.HitBreakable,
                finalPosition = finalPos,
                ricochets = 0,
                spinStep = 0,
                cardValue = value,
                zoneAccuracy01 = success ? 1f : 0f,
                bonusPickups = 0
            }); // → OnCardFlightFinished → Resolve
        }
#endif
    }
}
