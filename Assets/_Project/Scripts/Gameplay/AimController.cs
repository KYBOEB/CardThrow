using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CardThrow
{
    // Прицел-рогатка и закрут (CONTEXT, раздел 2 «Управление»). Владелец — Алексей.
    // ЛКМ зажать и тянуть НАЗАД → направление и сила; отпустить → бросок.
    // Колесо мыши / Q / E → закрут. ПКМ / Esc → отмена прицела.
    public class AimController : MonoBehaviour
    {
        [SerializeField] private CardFlight cardFlight;
        [SerializeField] private DealerHand dealerHand;
        [SerializeField] private Transform launchPoint;
        [SerializeField, Tooltip("Если пусто — найдётся в сцене автоматически")]
        private TrajectoryPreview preview;
        [SerializeField] private FlightSettings settings;

        [Header("Прицел")]
        [SerializeField, Tooltip("Куда смотрит «вперёд» (к игрокам): 0° = вправо вдоль стола")]
        private float forwardAngleDeg = 0f;
        [SerializeField, Tooltip("Насколько можно отклонить бросок от «вперёд» в каждую сторону")]
        private float maxAimAngle = 75f;
        [SerializeField, Tooltip("На сколько карта отъезжает назад в пальцах при 100% силы")]
        private float cardPullback = 0.6f;

        [Header("Закрут")]
        [SerializeField, Tooltip("Пауза между шагами колеса: трекпад шлёт много мелких событий")]
        private float scrollCooldown = 0.1f;

        public float PreviewFraction { get; set; } = 1f;

        private Camera cam;
        private bool inputEnabled;
        private bool aiming;
        private Vector2 dragStartWorld;
        private float lastPower = -1f;
        private int spinStep;          // НЕ сбрасываем в SetEnabled: при переигровке дуга сохраняется, на новом уровне — ResetSpin
        private float nextScrollTime;

        private void Awake()
        {
            cam = Camera.main;
            if (preview == null) preview = FindAnyObjectByType<TrajectoryPreview>();
            if (cardFlight == null) cardFlight = FindAnyObjectByType<CardFlight>();
            if (settings == null) Debug.LogError("[CT] AimController: не задан FlightSettings (меню CardThrow → Алексей → 4)", this);
        }

        // ---------- Публичный API ----------

        // Новая раздача (не переигровка) начинается с прямого броска: дуга → 0. Вызывает GameManager.LoadLevel.
        public void ResetSpin() => SetSpin(0);

        public void SetEnabled(bool value)
        {
            inputEnabled = value;
            if (!value) CancelAim(); // выключили посреди прицела — снимаем прицел и прячем превью
        }

        // ---------- Ввод ----------

        private void Update()
        {
            if (!inputEnabled || settings == null) return;
            Mouse mouse = Mouse.current;
            Keyboard kb = Keyboard.current;

            HandleSpin(mouse, kb);

            if (mouse == null) return;

            if (!aiming)
            {
                if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
                {
                    aiming = true;
                    dragStartWorld = MouseWorld(mouse);
                    lastPower = -1f;
                }
                return;
            }

            // Отмена: ПКМ или Esc.
            if (mouse.rightButton.wasPressedThisFrame || (kb != null && kb.escapeKey.wasPressedThisFrame))
            {
                CancelAim();
                return;
            }

            ThrowParams p = BuildParams(MouseWorld(mouse));

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (p.power01 < settings.minPower) CancelAim(); // слишком слабо — считаем, что передумал
                else Throw(p);
                return;
            }

            UpdateAim(p);
        }

        private void HandleSpin(Mouse mouse, Keyboard kb)
        {
            int delta = 0;
            if (kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) delta += 1;
                if (kb.eKey.wasPressedThisFrame) delta -= 1;
            }

            // Колесо: один шаг на «событие», независимо от величины (Windows ±120, трекпад Mac — мелкие значения).
            if (mouse != null && Time.unscaledTime >= nextScrollTime)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    delta += scroll > 0f ? 1 : -1;
                    nextScrollTime = Time.unscaledTime + scrollCooldown;
                }
            }

            if (delta != 0) SetSpin(spinStep + delta);
        }

        private void SetSpin(int value)
        {
            int clamped = Mathf.Clamp(value, -settings.maxSpinStep, settings.maxSpinStep);
            if (clamped == spinStep) return;
            spinStep = clamped;
            GameEvents.RaiseSpinChanged(spinStep); // на это опираются HUD и обучение
        }

        // ---------- Прицел ----------

        private ThrowParams BuildParams(Vector2 mouseWorld)
        {
            Vector2 drag = dragStartWorld - mouseWorld; // тянем назад → летит вперёд
            Vector2 forward = FlightSimulator.Rotate(Vector2.right, forwardAngleDeg * Mathf.Deg2Rad);

            // Ограничиваем угол «вперёд ± maxAimAngle», чтобы нельзя было бросить в себя.
            Vector2 dir = forward;
            if (drag.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Clamp(Vector2.SignedAngle(forward, drag), -maxAimAngle, maxAimAngle);
                dir = FlightSimulator.Rotate(forward, angle * Mathf.Deg2Rad);
            }

            return new ThrowParams
            {
                origin = LaunchOrigin(),
                direction = dir,
                power01 = Mathf.Clamp01(drag.magnitude / settings.maxDragDistance),
                spinStep = spinStep,
                cardValue = dealerHand != null ? dealerHand.SelectedValue : 0
            };
        }

        private void UpdateAim(ThrowParams p)
        {
            if (!Mathf.Approximately(p.power01, lastPower))
            {
                lastPower = p.power01;
                GameEvents.RaiseAimChanged(p.power01);
            }

            if (preview != null)
            {
                if (p.power01 >= settings.minPower) preview.Show(p, PreviewFraction);
                else preview.Hide();
            }

            if (cardFlight != null)
            {
                float angleDeg = Mathf.Atan2(p.direction.y, p.direction.x) * Mathf.Rad2Deg - 90f;
                cardFlight.SetAimPose(p.origin - p.direction * (p.power01 * cardPullback), angleDeg);
            }
        }

        private void Throw(ThrowParams p)
        {
            aiming = false;
            inputEnabled = false;
            if (preview != null) preview.Hide();
            GameEvents.RaiseAimChanged(0f);

            // Порядок важен: сначала событие (GameManager → Flying), потом полёт.
            GameEvents.RaiseCardThrown(p);
            if (cardFlight != null) cardFlight.Launch(p);
        }

        private void CancelAim()
        {
            if (!aiming) return;
            aiming = false;
            if (preview != null) preview.Hide();
            GameEvents.RaiseAimChanged(0f);
            if (cardFlight != null && !cardFlight.IsFlying) cardFlight.ResetCard(); // карта обратно в пальцы
        }

        // ---------- Вспомогательное ----------

        private Vector2 LaunchOrigin() => launchPoint != null ? (Vector2)launchPoint.position : (Vector2)transform.position;

        private Vector2 MouseWorld(Mouse mouse)
        {
            if (cam == null) cam = Camera.main;
            Vector2 screen = mouse.position.ReadValue();
            return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
