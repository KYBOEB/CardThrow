using System.Collections;
using UnityEngine;

namespace CardThrow
{
    // Полёт брошенной карты (CONTEXT, разделы 3 и 5.3). Владелец — Алексей.
    // Карта БЕЗ Rigidbody2D и коллайдера: в FixedUpdate двигаем transform по FlightSimulator,
    // столкновения ищем CircleCast'ом, зоны и край стола — OverlapPoint.
    public class CardFlight : MonoBehaviour
    {
        [SerializeField] private FlightSettings settings;

        [Header("Визуал")]
        [SerializeField] private SpriteRenderer cardRenderer;
        [SerializeField, Tooltip("Лица карт: card_2 … card_11 (индекс = номинал − 2)")]
        private Sprite[] faces;
        [SerializeField] private Sprite crumpledSprite;

        [Header("Тонкие настройки")]
        [SerializeField, Tooltip("Сколько секунд не замечаем фишку, которую только что задели")]
        private float pushIgnoreTime = 0.2f;
        [SerializeField, Tooltip("Страховка: дольше этого карта не летит")]
        private float maxFlightTime = 12f;
        [SerializeField, Tooltip("Пауза на анимацию перед отчётом об итоге (реальные секунды)")]
        private float finishDelay = 0.35f;

        public bool IsFlying { get; private set; }

        private FlightState state;
        private int spinStep;
        private int cardValue;
        private int ricochets;

        private Vector3 homePosition;       // где карта лежит до броска (точка броска крупье)
        private Sprite currentFace;
        private Collider2D ignoredCollider;
        private float ignoreUntil;
        private bool finishing;              // итог уже решён, идёт короткая анимация
        private bool raisingOwnHit;          // чтобы не принять собственное событие за цепную реакцию
        private Coroutine finishRoutine;

        private readonly Collider2D[] overlapBuffer = new Collider2D[8];
        private ContactFilter2D zoneFilter;
        private ContactFilter2D tableFilter;

        private void Awake()
        {
            homePosition = transform.position;
            if (cardRenderer == null) cardRenderer = GetComponent<SpriteRenderer>();
            if (cardRenderer != null) currentFace = cardRenderer.sprite;

            if (settings == null)
            {
                Debug.LogError("[CT] CardFlight: не задан FlightSettings (меню CardThrow → Алексей → Настроить префаб Card)", this);
                return;
            }
            zoneFilter = MakeTriggerFilter(settings.zoneMask);
            tableFilter = MakeTriggerFilter(settings.tableMask);
        }

        private void OnEnable()
        {
            GameEvents.CardSelected += OnCardSelected;
            GameEvents.CardHitObstacle += OnAnyObstacleHit;
        }

        private void OnDisable()
        {
            GameEvents.CardSelected -= OnCardSelected;
            GameEvents.CardHitObstacle -= OnAnyObstacleHit;
        }

        // ---------- Публичный API (контракт CONTEXT 5.3) ----------

        public void Launch(ThrowParams p)
        {
            if (settings == null) return;

            StopFinishRoutine();
            state = FlightSimulator.Launch(p, settings);
            spinStep = p.spinStep;
            cardValue = p.cardValue;
            ricochets = 0;
            ignoredCollider = null;
            finishing = false;

            transform.position = new Vector3(state.pos.x, state.pos.y, homePosition.z);
            ShowFace();
            IsFlying = true;
        }

        public void ResetCard()
        {
            StopFinishRoutine();
            IsFlying = false;
            finishing = false;
            ignoredCollider = null;

            transform.position = homePosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            ShowFace();
        }

        // Поза карты в пальцах крупье при оттяге (вызывает AimController). Во время полёта игнорируется.
        public void SetAimPose(Vector2 position, float angleDeg)
        {
            if (IsFlying || finishing) return;
            transform.position = new Vector3(position.x, position.y, homePosition.z);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        // ---------- Полёт ----------

        private void FixedUpdate()
        {
            if (!IsFlying) return;

            float dt = Time.fixedDeltaTime;
            Collider2D ignore = Time.time < ignoreUntil ? ignoredCollider : null;

            StepEvent ev = FlightSimulator.StepWithCollisions(ref state, settings, dt, ignore,
                out Vector2 hitPoint, out Obstacle obstacle, out Collider2D hitCollider);

            transform.position = new Vector3(state.pos.x, state.pos.y, homePosition.z);

            switch (ev)
            {
                case StepEvent.Ricochet:
                    ricochets++;
                    RaiseOwnHit(hitPoint, ObstacleKind.Ricochet);
                    break;

                case StepEvent.HitPushable:
                    obstacle.OnCardHit(hitPoint, state.vel);
                    state.vel *= 1f - settings.pushSpeedLoss;
                    ignoredCollider = hitCollider;
                    ignoreUntil = Time.time + pushIgnoreTime;
                    RaiseOwnHit(hitPoint, ObstacleKind.Pushable);
                    break;

                case StepEvent.HitBreakable:
                    obstacle.OnCardHit(hitPoint, state.vel);
                    RaiseOwnHit(hitPoint, ObstacleKind.Breakable);
                    Finish(FlightResult.HitBreakable, 0f);
                    return;
            }

            if (!IsFlying) return; // мог завершиться цепной реакцией во время OnCardHit

            // Зона соперника — проверяем на каждом шаге: залетел центр — сразу пойман.
            if (FindZone(state.pos, ZoneType.Opponent) != null)
            {
                Finish(FlightResult.EnteredOpponentZone, 0f);
                return;
            }

            // Край стола.
            if (!IsOnTable(state.pos))
            {
                Finish(FlightResult.OffTable, 0f);
                return;
            }

            // Остановка: зона напарника засчитывается только здесь (пролёт сквозь неё — не попадание).
            if (state.IsStopped || state.time >= maxFlightTime)
            {
                Zone partner = FindZone(state.pos, ZoneType.Partner);
                if (partner != null)
                {
                    float radius = Mathf.Max(0.01f, partner.Radius);
                    float accuracy = 1f - Mathf.Clamp01(Vector2.Distance(state.pos, partner.Center) / radius);
                    Finish(FlightResult.LandedInPartnerZone, accuracy);
                }
                else
                {
                    Finish(FlightResult.StoppedOnTable, 0f);
                }
            }
        }

        // ---------- Итог ----------

        private void Finish(FlightResult result, float accuracy)
        {
            if (finishing) return; // защита от двойного отчёта
            IsFlying = false;
            finishing = true;

            FlightReport report = new FlightReport
            {
                result = result,
                finalPosition = state.pos,
                ricochets = ricochets,
                spinStep = spinStep,
                cardValue = cardValue,
                zoneAccuracy01 = accuracy,
                bonusPickups = 0
            };
            finishRoutine = StartCoroutine(FinishRoutine(report));
        }

        // Короткая заглушка визуала (полноценный game feel — задача A5), затем отчёт GameManager'у.
        private IEnumerator FinishRoutine(FlightReport report)
        {
            if (report.result == FlightResult.HitBreakable && crumpledSprite != null && cardRenderer != null)
                cardRenderer.sprite = crumpledSprite;

            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < finishDelay)
            {
                t += Time.unscaledDeltaTime; // не зависит от слоу-мо и hit-stop
                float k = Mathf.Clamp01(t / finishDelay);
                if (report.result == FlightResult.OffTable)
                {
                    // «Упала на пол»: уменьшается и тает.
                    transform.localScale = startScale * Mathf.Lerp(1f, 0.6f, k);
                    if (cardRenderer != null) cardRenderer.color = new Color(1f, 1f, 1f, 1f - k);
                }
                yield return null;
            }

            finishRoutine = null;
            GameEvents.RaiseCardFlightFinished(report);
        }

        // ---------- События ----------

        private void OnCardSelected(int index, int value)
        {
            int i = value - 2;
            if (faces != null && i >= 0 && i < faces.Length && faces[i] != null) currentFace = faces[i];
            if (!IsFlying && !finishing) ShowFace();
        }

        // Цепная реакция: бутылку разбила разлетевшаяся фишка, пока карта ещё летит → бросок провален.
        private void OnAnyObstacleHit(Vector2 point, ObstacleKind kind)
        {
            if (raisingOwnHit || !IsFlying) return;
            if (kind == ObstacleKind.Breakable) Finish(FlightResult.HitBreakable, 0f);
        }

        private void RaiseOwnHit(Vector2 point, ObstacleKind kind)
        {
            raisingOwnHit = true;
            GameEvents.RaiseCardHitObstacle(point, kind);
            raisingOwnHit = false;
        }

        // ---------- Вспомогательное ----------

        private Zone FindZone(Vector2 point, ZoneType type)
        {
            int count = Physics2D.OverlapPoint(point, zoneFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Zone z = overlapBuffer[i].GetComponent<Zone>();
                if (z != null && z.Type == type) return z;
            }
            return null;
        }

        private bool IsOnTable(Vector2 point)
        {
            if (settings.tableMask.value == 0) return true; // маска не задана — край стола не проверяем
            return Physics2D.OverlapPoint(point, tableFilter, overlapBuffer) > 0;
        }

        private static ContactFilter2D MakeTriggerFilter(LayerMask mask)
        {
            ContactFilter2D f = new ContactFilter2D();
            f.SetLayerMask(mask);
            f.useTriggers = true;
            return f;
        }

        private void ShowFace()
        {
            if (cardRenderer == null) return;
            cardRenderer.enabled = true;
            cardRenderer.sprite = currentFace;
            cardRenderer.color = Color.white;
        }

        private void StopFinishRoutine()
        {
            if (finishRoutine != null) StopCoroutine(finishRoutine);
            finishRoutine = null;
        }
    }
}
