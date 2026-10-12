using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardThrow
{
    // Интерактивное обучение.
    // Ничего не симулирует и не решает исход: только слушает те же GameEvents, что и вся игра,
    // и показывает подсказку (HintChanged), иконки управления и стрелку-указатель.
    //
    // Обучение — 3 раздачи подряд, приём задаёт LevelConfig.TutorialStage:
    //   Throw    — выбрать карту → бросок (стрелка на зону напарника);
    //   Spin     — выбрать карту → бросок; как только игрок тянет карту без дуги — подсказка про дугу
    //              (колесо / Q / E) и стрелка на стакан;
    //   Ricochet — выбрать карту → бросок с рикошетом от пепельницы (стрелка на пепельницу). Что без отскока
    //              не пройти — обеспечивает раскладка стола, а не if в коде.
    // Текущий шаг = первый невыполненный, поэтому при переигровке (карта и дуга сохранены) шаги пропускаются сами.
    public class TutorialController : MonoBehaviour
    {
        private const int StepCard = 0, StepSpin = 1, StepThrow = 2, StepCount = 3;

        [Header("Данные")]
        [SerializeField] private UiTexts texts;

        [Header("UI обучения (Canvas/TutorialPanel)")]
        [Tooltip("Корень всего UI обучения — выключается вне уровня 0")]
        [SerializeField] private GameObject tutorialRoot;
        [SerializeField] private GameObject[] step1Icons;   // клавиши A, D
        [SerializeField] private GameObject[] step2Icons;   // колесо мыши, Q, E
        [SerializeField] private GameObject[] step3Icons;   // мышь «оттяг»
        [SerializeField] private RectTransform arrow;       // Image tut_arrow
        [SerializeField] private TMP_Text skipHintLabel;
        [Tooltip("Необязательно: кнопка на подсказке пропуска (пропуск мышью)")]
        [SerializeField] private Button skipButton;

        [Header("Стрелка")]
        [Tooltip("Смещение стрелки от напарника, в единицах Canvas (1920×1080). Стрелка всегда смотрит на цель")]
        [SerializeField] private Vector2 partnerArrowOffset = new Vector2(0f, -150f);
        [SerializeField] private Vector2 bottleArrowOffset = new Vector2(0f, 130f);
        [SerializeField] private Vector2 glassArrowOffset = new Vector2(0f, 130f);
        [Tooltip("Куда «смотрит» спрайт tut_arrow без поворота, градусы (−90 = вниз, 90 = вверх)")]
        [SerializeField] private float arrowSpriteAngle = -90f;
        [SerializeField] private float arrowBobAmplitude = 18f;
        [SerializeField] private float arrowBobSpeed = 6f;

        [Header("Ряд иконок рядом с крупье")]
        [Tooltip("Ряд иконок (Icons). Если задан Icons Anchor — ряд каждый кадр ставится рядом с ним")]
        [SerializeField] private RectTransform iconsRow;
        [Tooltip("Объект в мире, к которому привязан ряд иконок (Dealer). Пусто — ряд стоит, где его поставили в Canvas")]
        [SerializeField] private Transform iconsAnchor;
        [Tooltip("Смещение ряда от крупье, в единицах Canvas. Pivot ряда — левый край")]
        [SerializeField] private Vector2 iconsOffset = new Vector2(60f, 150f);

        [Header("Анимация иконки «оттяг»")]
        [SerializeField] private RectTransform dragIcon;    // та же иконка, что в step3Icons
        [SerializeField] private float dragIconDistance = 30f;
        [SerializeField] private float dragIconSpeed = 3f;

        // Наблюдаемое состояние ввода — обновляется ВСЕГДА (CardSelected приходит до HandStarted)
        private int lastCardValue;
        private int lastSpin;
        private bool aiming;       // игрок сейчас тянет карту (AimChanged > 0)

        // Состояние обучения
        private bool tutorialActive;   // текущий уровень — обучение и забег идёт
        private bool guiding;          // показываем шаги (HandIntro/Aiming до броска)
        private int currentStep = -1;
        private string pendingFailHint = "";

        // Цели стрелки (кэш на уровень)
        private Transform partnerTarget;
        private Transform bottleTarget;
        private Transform glassTarget;
        private Zone partnerZone;
        private TutorialStage stage = TutorialStage.Spin;
        private Transform arrowTarget;
        private Vector2 arrowOffset;

        // Итог броска: ThrowResolved и CardFlightFinished могут прийти в любом порядке подписчиков
        private Vector2 throwOrigin;
        private bool hasFlightReport;
        private FlightReport lastReport;
        private bool waitingForReport;

        // Кэш UI
        private Camera worldCamera;
        private Camera canvasCamera;
        private RectTransform arrowParent;
        private RectTransform iconsParent;
        private Vector2 dragIconBasePos;

        // ---------- Жизненный цикл ----------

        private void Awake()
        {
            if (texts == null) Debug.LogError("[CT] TutorialController: не назначен UiTexts", this);

            worldCamera = Camera.main; // один раз; у Main Camera должен быть тег MainCamera
            if (worldCamera == null) Debug.LogWarning("[CT] TutorialController: нет камеры с тегом MainCamera — стрелка не будет видна", this);
            if (arrow != null)
            {
                arrowParent = arrow.parent as RectTransform;
                Canvas canvas = arrow.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    canvasCamera = canvas.worldCamera; // для Overlay камера = null
            }
            if (iconsRow != null) iconsParent = iconsRow.parent as RectTransform;
            if (dragIcon != null) dragIconBasePos = dragIcon.anchoredPosition;
            if (skipHintLabel != null && texts != null) skipHintLabel.text = texts.tutorialSkipHint;

            HideAll();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.HandStarted += OnHandStarted;
            GameEvents.CardSelected += OnCardSelected;
            GameEvents.SpinChanged += OnSpinChanged;
            GameEvents.AimChanged += OnAimChanged;
            GameEvents.CardThrown += OnCardThrown;
            GameEvents.CardFlightFinished += OnCardFlightFinished;
            GameEvents.ThrowResolved += OnThrowResolved;
            if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.HandStarted -= OnHandStarted;
            GameEvents.CardSelected -= OnCardSelected;
            GameEvents.SpinChanged -= OnSpinChanged;
            GameEvents.AimChanged -= OnAimChanged;
            GameEvents.CardThrown -= OnCardThrown;
            GameEvents.CardFlightFinished -= OnCardFlightFinished;
            GameEvents.ThrowResolved -= OnThrowResolved;
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkipClicked);
        }

        // Только визуал: позиция стрелки и анимации. Без Find/GetComponent.
        private void LateUpdate()
        {
            if (!tutorialActive) return;

            float t = Time.unscaledTime; // slow-mo не замедляет подсказки
            UpdateArrow(t);
            UpdateIcons();

            if (dragIcon != null && dragIcon.gameObject.activeInHierarchy)
            {
                float pull = (1f - Mathf.Cos(t * dragIconSpeed)) * 0.5f; // 0..1: плавно тянем назад и отпускаем
                dragIcon.anchoredPosition = dragIconBasePos + Vector2.down * (pull * dragIconDistance);
            }
        }

        // ---------- События игры ----------

        private void OnHandStarted(int levelNumber, int totalLevels, string title)
        {
            GameManager gm = GameManager.Instance;
            tutorialActive = gm != null && gm.IsTutorial;
            guiding = false;
            currentStep = -1;
            hasFlightReport = false;
            waitingForReport = false;

            HideAll();
            if (!tutorialActive) { pendingFailHint = ""; return; }

            CacheTargets(gm.CurrentLevel);
            if (tutorialRoot != null) tutorialRoot.SetActive(true);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Title || state == GameState.GameOver || state == GameState.Victory)
            {
                tutorialActive = false;
                guiding = false;
                pendingFailHint = "";
                HideAll();
                return;
            }

            // Шаги показываем на входе в Aiming, а не в HandIntro: во время баннера (handIntroDuration) ввод выключен,
            // и иконка «нажми A», на которую нельзя нажать, выглядит как баг. Aiming приходит после HintChanged(level.Hint),
            // поэтому наша подсказка его не перетирает.
            if (tutorialActive && state == GameState.Aiming && !guiding && currentStep == -1)
            {
                guiding = true;
                currentStep = -1;
                RefreshStep();
                pendingFailHint = ""; // подсказка по прошлому провалу живёт только до первого показа
            }
        }

        private void OnCardSelected(int index, int value)
        {
            lastCardValue = value;
            if (guiding) RefreshStep();
        }

        private void OnSpinChanged(int spinStep)
        {
            lastSpin = spinStep;
            if (guiding) RefreshStep();
        }

        private void OnAimChanged(float power01)
        {
            bool now = power01 > 0f;
            if (now == aiming) return; // событие приходит каждый кадр оттяга — реагируем только на начало/конец
            aiming = now;
            if (guiding) RefreshStep();
        }

        private void OnCardThrown(ThrowParams p)
        {
            aiming = false;
            // Карту и дугу НЕ берём из p: их уже прислали CardSelected/SpinChanged,
            // а отладочный бросок (Shift+S/F) шлёт spinStep = 0 и сбил бы шаг при переигровке.
            throwOrigin = p.origin;
            hasFlightReport = false;
            waitingForReport = false;

            if (!tutorialActive) return;
            guiding = false;
            currentStep = -1;
            ShowStepVisuals(-1);              // во время полёта — чистый стол
            GameEvents.RaiseHintChanged("");
        }

        private void OnCardFlightFinished(FlightReport report)
        {
            lastReport = report;
            hasFlightReport = true;

            if (waitingForReport) // ThrowResolved уже пришёл раньше нас
            {
                waitingForReport = false;
                ShowResult(ThrowOutcome.StoppedOnTable);
            }
        }

        private void OnThrowResolved(ThrowOutcome outcome, int scoreGained)
        {
            if (!tutorialActive) return;

            if (outcome == ThrowOutcome.StoppedOnTable && !hasFlightReport)
            {
                waitingForReport = true; // финальная позиция ещё не известна — дождёмся CardFlightFinished
                return;
            }
            ShowResult(outcome);
        }

        private void OnSkipClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.SkipTutorial();
        }

        // ---------- Шаги ----------

        private void RefreshStep()
        {
            int step;
            if (!IsCardStepDone()) step = StepCard;
            else if (NeedsSpinHint()) step = StepSpin;
            else step = StepThrow;
            if (step == currentStep) return;
            currentStep = step;

            string hint = GetStepText(step);
            if (step == StepThrow && !string.IsNullOrEmpty(pendingFailHint))
                hint = pendingFailHint + "\n" + hint; // переигровка: напоминаем, что пошло не так

            Debug.Log($"[CT] Tutorial step {step + 1}/{StepCount}");
            GameEvents.RaiseHintChanged(hint);
            ShowStepVisuals(step);
        }

        // Подсказка про дугу: на столе есть стакан, игрок тянет карту (в части «Рикошет» — не нужна).
        // Появляется, пока дуги нет, и держится до отпускания ЛКМ, чтобы игрок видел, как менять дугу дальше.
        // Сброс — aiming = false (бросок или отмена).
        private bool NeedsSpinHint() =>
            aiming && bottleTarget != null && stage != TutorialStage.Ricochet &&
            (lastSpin == 0 || currentStep == StepSpin);

        private bool IsCardStepDone()
        {
            LevelConfig level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
            if (level == null) return true;

            int correct = level.CorrectCardIndex;
            if (correct < 0) return true; // уровень настроен неверно — LevelConfig.OnValidate уже предупредил
            return lastCardValue == level.CardValues[correct];
        }

        private string GetStepText(int step)
        {
            if (texts == null || texts.tutorialSteps == null || step >= texts.tutorialSteps.Length) return "";

            string s = texts.tutorialSteps[step];
            if (step == StepThrow && stage == TutorialStage.Ricochet && !string.IsNullOrEmpty(texts.tutorialRicochetStep))
                s = texts.tutorialRicochetStep;
            // В раздаче нет стакана — подсказка про дугу без упоминания препятствия
            if (step == StepSpin && bottleTarget == null && !string.IsNullOrEmpty(texts.tutorialSpinNoObstacle))
                s = texts.tutorialSpinNoObstacle;
            LevelConfig level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
            if (level != null) s = s.Replace(UiTexts.PartnerScoreToken, level.PartnerStartScore.ToString());
            return s;
        }

        private void ShowResult(ThrowOutcome outcome)
        {
            if (texts == null) return;

            if (outcome == ThrowOutcome.Success)
            {
                pendingFailHint = "";
                GameEvents.RaiseHintChanged(GetSuccessText());
                return;
            }

            pendingFailHint = GetFailText(outcome);
            GameEvents.RaiseHintChanged(pendingFailHint);
        }

        // Промежуточные части обучения подводят к следующему приёму; последняя — к игре на время.
        // Последняя = следующая раздача уже не обучение (так работает и с одной, и с тремя частями).
        private string GetSuccessText()
        {
            GameManager gm = GameManager.Instance;
            bool last = gm == null || gm.IsLastTutorialHand;
            if (!last && stage == TutorialStage.Throw && !string.IsNullOrEmpty(texts.tutorialSuccessThrow)) return texts.tutorialSuccessThrow;
            if (!last && stage == TutorialStage.Spin && !string.IsNullOrEmpty(texts.tutorialSuccessSpin)) return texts.tutorialSuccessSpin;
            return texts.tutorialSuccess;
        }

        private string GetFailText(ThrowOutcome outcome)
        {
            switch (outcome)
            {
                case ThrowOutcome.OffTable: return texts.tutorialFailOffTable;
                case ThrowOutcome.HitBreakable: return texts.tutorialFailBreakable;
                case ThrowOutcome.OpponentCaught: return texts.tutorialFailOpponent;
                case ThrowOutcome.WrongValue: return texts.tutorialFailWrongValue;
                case ThrowOutcome.StoppedOnTable: return GetMissText();
                default: return "";
            }
        }

        // Недолёт / перелёт / мимо: сравниваем дальность броска с дальностью до центра зоны напарника.
        // Если дальность «попала» в зону (±радиус зоны), а карта всё равно не в зоне — промах вбок.
        private string GetMissText()
        {
            if (partnerZone == null || !hasFlightReport) return texts.tutorialFailUndershoot;

            float thrown = Vector2.Distance(throwOrigin, lastReport.finalPosition);
            float needed = Vector2.Distance(throwOrigin, partnerZone.Center);
            float tolerance = partnerZone.Radius;

            if (Mathf.Abs(thrown - needed) <= tolerance) return texts.tutorialFailSideways;
            return thrown < needed ? texts.tutorialFailUndershoot : texts.tutorialFailOvershoot;
        }

        // ---------- Визуал ----------

        private void CacheTargets(LevelConfig level)
        {
            partnerTarget = null; bottleTarget = null; glassTarget = null; partnerZone = null;
            if (level == null) return;
            stage = level.TutorialStage;

            Seat partner = level.PartnerSeat;
            if (partner != null)
            {
                // Стрелка указывает на ЗОНУ карт напарника — туда и нужно бросать (фигура сидит за бортиком)
                partnerTarget = partner.Zone != null ? partner.Zone.transform : partner.transform;
                partnerZone = partner.Zone;
            }
            else Debug.LogWarning($"[CT] Tutorial: в {level.name} не заполнен LevelConfig.PartnerSeat", level);

            BreakableObstacle bottle = level.GetComponentInChildren<BreakableObstacle>();
            if (bottle != null) bottleTarget = bottle.transform;
            RicochetObstacle glass = level.GetComponentInChildren<RicochetObstacle>();
            if (glass != null) glassTarget = glass.transform;
        }

        private void ShowStepVisuals(int step)
        {
            SetActiveAll(step1Icons, step == StepCard);
            SetActiveAll(step2Icons, step == StepSpin);
            SetActiveAll(step3Icons, step == StepThrow);

            // Куда показывает стрелка: шаг «карта» и бросок в части «Бросок» — зона напарника;
            // дуга — стакан; бросок в части «Рикошет» — пепельница; иначе стрелки нет
            arrowTarget = null;
            if (step == StepCard || (step == StepThrow && stage == TutorialStage.Throw)) { arrowTarget = partnerTarget; arrowOffset = partnerArrowOffset; }
            else if (step == StepSpin) { arrowTarget = bottleTarget; arrowOffset = bottleArrowOffset; }
            else if (step == StepThrow && stage == TutorialStage.Ricochet) { arrowTarget = glassTarget; arrowOffset = glassArrowOffset; }

            if (arrow != null) arrow.gameObject.SetActive(arrowTarget != null);
            UpdateArrow(Time.unscaledTime); // без кадра в старой позиции
            UpdateIcons();
        }

        private void UpdateArrow(float time)
        {
            if (arrow == null || arrowParent == null || arrowTarget == null) return;
            if (!WorldToCanvas(arrowTarget.position, arrowParent, out Vector2 targetLocal)) return;

            // Стрелка стоит в точке «цель + смещение» и смотрит на цель; покачивается вдоль этого направления
            Vector2 toTarget = arrowOffset.sqrMagnitude > 0.01f ? -arrowOffset.normalized : Vector2.down;
            float bob = (Mathf.Sin(time * arrowBobSpeed) + 1f) * 0.5f * arrowBobAmplitude;

            arrow.localPosition = targetLocal + arrowOffset + toTarget * bob;
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            arrow.localRotation = Quaternion.Euler(0f, 0f, angle - arrowSpriteAngle);
        }

        // Иконки стоят рядом с крупье при любом размере окна Game (как и стрелка — через экранные координаты)
        private void UpdateIcons()
        {
            if (iconsRow == null || iconsParent == null || iconsAnchor == null) return;
            if (WorldToCanvas(iconsAnchor.position, iconsParent, out Vector2 local))
                iconsRow.localPosition = local + iconsOffset;
        }

        // Мир → экран (Camera.main) → локальные координаты UI-родителя (RectTransformUtility)
        private bool WorldToCanvas(Vector3 world, RectTransform parent, out Vector2 local)
        {
            local = Vector2.zero;
            if (worldCamera == null) return false;
            Vector3 screen = worldCamera.WorldToScreenPoint(world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, canvasCamera, out local);
        }

        private void HideAll()
        {
            ShowStepVisuals(-1);
            if (tutorialRoot != null) tutorialRoot.SetActive(false);
        }

        private static void SetActiveAll(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (GameObject go in objects)
                if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
