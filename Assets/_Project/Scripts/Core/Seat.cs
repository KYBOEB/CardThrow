using TMPro;
using UnityEngine;

namespace CardThrow
{
    // Игрок за столом: счёт над головой, поворот головы («смотрит на стол / отвернулся»), подсветка напарника.
    // D3 — читаемость стола (CONTEXT §8):
    //   • зона карт видна на сукне: напарник — золото, соперник — красный;
    //   • во время прицеливания зоны соперников пульсируют красным («сюда нельзя»);
    //   • золотой контур напарника мягко «дышит»;
    //   • когда соперник поворачивается обратно (за 3 с до конца окна внимания), его счёт краснеет и пульсирует.
    // Публичный API (CONTEXT 5.3) не изменён — добавлены только приватные поля и визуал.
    public class Seat : MonoBehaviour
    {
        [SerializeField] private string displayName = "Игрок";
        [SerializeField] private bool isPartner;
        [SerializeField] private int score = 12;
        [SerializeField] private Zone zone;

        [Header("Визуал")]
        [SerializeField] private TMP_Text scoreLabel;          // TextMeshPro (world space) над головой
        [SerializeField] private Transform head;               // спрайт головы, вращается по Z
        [SerializeField] private GameObject partnerHighlight;  // золотой контур
        [SerializeField] private float lookAwayAngle = 150f;
        [SerializeField] private float headTurnSpeed = 360f;   // градусов в секунду

        [Header("Посадка за столом (D3b)")]
        [Tooltip("Visual: Body + Head + PartnerGlow. Поворачивается руками к столу (к своей зоне) и отодвигается за край")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Масштаб фигуры игрока (спрайты 1.6×1.2 ед. при PPU 200)")]
        [SerializeField] private float visualScale = 1.6f;
        [Tooltip("Запасной вариант, если край стола не найден: сдвиг фигуры от точки Seat в сторону ОТ стола")]
        [SerializeField] private float pushBack = 1.65f;
        [Tooltip("Насколько руки заходят за внешний край стола (бортик ≈ 0.45 ед.). Фигура ставится от края стола, а не от точки Seat")]
        [SerializeField] private float handsOnTable = 0.6f;
        [Tooltip("Половина длины спрайта тела вдоль «руки→кресло» до масштаба: player_body 240 px / PPU 200 / 2")]
        [SerializeField] private float bodyHalfLength = 0.6f;
        [Tooltip("Слой триггера TableSurface (CONTEXT §4)")]
        [SerializeField] private LayerMask tableMask = 1 << 9;
        [Tooltip("Счёт рисуется поверх зоны карт игрока (его «стопка»). Сдвиг от центра зоны к игроку")]
        [SerializeField] private float labelOffsetOnZone = 0f;
        [Tooltip("Половина размера стола (CONTEXT §4: 19 × 9.5). По ней выбирается ближайший край — туда игрок и садится")]
        [SerializeField] private Vector2 tableHalfSize = new Vector2(9.5f, 4.75f);

        [Header("Зона на столе (D3)")]
        [Tooltip("SpriteRenderer внутри Zone (ZoneVisual). Размер подгоняется под BoxCollider2D зоны")]
        [SerializeField] private SpriteRenderer zoneVisual;
        [SerializeField] private Sprite partnerZoneSprite;     // Table/zone_partner
        [SerializeField] private Sprite opponentZoneSprite;    // Table/zone_opponent
        [Tooltip("Прозрачность зоны соперника вне прицеливания")]
        [SerializeField, Range(0f, 1f)] private float opponentIdleAlpha = 0.35f;
        [Tooltip("Прозрачность зоны соперника при прицеливании: пульс между min и 1")]
        [SerializeField, Range(0f, 1f)] private float opponentAimingMinAlpha = 0.7f;
        [SerializeField] private float zonePulseFrequency = 1.5f;   // ударов в секунду

        [Header("Напарник (D3)")]
        [SerializeField] private Color partnerLabelColor = new Color(1f, 0.82f, 0.3f);
        [SerializeField, Range(0f, 1f)] private float glowMinAlpha = 0.55f;
        [SerializeField] private float glowPulseFrequency = 0.8f;

        [Header("Тревога соперника (D3)")]
        [SerializeField] private Color alertLabelColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private float alertPulseAmount = 0.25f;
        [SerializeField] private float alertPulseFrequency = 3f;

        private float baseHeadAngle;
        private float targetHeadAngle;
        private float currentTurnSpeed;

        // D3: кэш (только Awake, без GetComponent в Update)
        private SpriteRenderer glowRenderer;
        private Color baseLabelColor = Color.white;
        private Vector3 baseLabelScale = Vector3.one;
        private bool aiming;     // игра в состоянии Aiming
        private bool alert;      // соперник повернулся обратно к столу

        public string DisplayName => displayName;
        public bool IsPartner => isPartner;
        public int Score => score;
        public Zone Zone => zone;

        private void Awake()
        {
            if (head != null) baseHeadAngle = head.localEulerAngles.z;
            targetHeadAngle = baseHeadAngle;
            currentTurnSpeed = headTurnSpeed;
            if (partnerHighlight != null)
            {
                partnerHighlight.SetActive(isPartner);
                glowRenderer = partnerHighlight.GetComponent<SpriteRenderer>();
            }

            if (scoreLabel != null)
            {
                if (isPartner) scoreLabel.color = partnerLabelColor;
                baseLabelColor = scoreLabel.color;
                baseLabelScale = scoreLabel.transform.localScale;
            }

            LayoutAtTable();
            SetupZoneVisual();
            RefreshLabel();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            // Раздача создаётся до SetState(HandIntro), но на всякий случай берём текущее состояние
            aiming = GameManager.Instance != null && GameManager.Instance.State == GameState.Aiming;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        public void SetScore(int value)
        {
            score = value;
            RefreshLabel();
        }

        public void SetLookingAway(bool away)
        {
            targetHeadAngle = baseHeadAngle + (away ? lookAwayAngle : 0f);
            currentTurnSpeed = headTurnSpeed;
            alert = false; // мгновенный поворот — это служебный сброс, а не «заметил»
        }

        // Плавный поворот за заданное время (например, за 3 с до конца «окна внимания»).
        // Поворот обратно к столу = соперник вот-вот заметит → тревога на счёте.
        public void SetLookingAway(bool away, float turnDuration)
        {
            targetHeadAngle = baseHeadAngle + (away ? lookAwayAngle : 0f);
            currentTurnSpeed = turnDuration > 0.01f ? Mathf.Abs(lookAwayAngle) / turnDuration : headTurnSpeed;
            alert = !away && !isPartner;
        }

        private void OnStateChanged(GameState state)
        {
            aiming = state == GameState.Aiming;
            if (state != GameState.Aiming && state != GameState.Flying) alert = false;
        }

        private void Update()
        {
            if (head != null)
            {
                float z = Mathf.MoveTowardsAngle(head.localEulerAngles.z, targetHeadAngle, currentTurnSpeed * Time.deltaTime);
                head.localEulerAngles = new Vector3(0f, 0f, z);
            }

            float t = Time.unscaledTime; // пульсы не замедляются в slow-mo
            UpdateZoneVisual(t);
            UpdateGlow(t);
            UpdateAlert(t);
        }

        // ---------- D3: визуал ----------

        // Фигура «садится» за стол: руки (верх спрайта, +Y) смотрят на свою зону, сама фигура — за краем.
        // Направление берём из локальной позиции Zone, поэтому Егор ставит Seat где угодно — поворот подстроится сам.
        // Seat в раздачах не повёрнут и не масштабирован (rotation 0, scale 1) — считаем в локальных координатах.
        private void LayoutAtTable()
        {
            // Направление «к столу» = нормаль ближайшего края стола (центр стола в (0,0), CONTEXT §4).
            // Не зависит от того, где стоит зона: Егор может двигать зоны к игрокам, игрок не развернётся.
            Vector2 p = transform.position;
            Vector2 dir = Mathf.Abs(p.x) / tableHalfSize.x > Mathf.Abs(p.y) / tableHalfSize.y
                ? new Vector2(-Mathf.Sign(p.x), 0f)
                : new Vector2(0f, p.y == 0f ? 1f : -Mathf.Sign(p.y));

            if (visualRoot != null)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // спрайт нарисован руками вверх
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
                visualRoot.localScale = Vector3.one * visualScale;
                visualRoot.localPosition = -dir * pushBack; // запасной вариант

                // Ищем внешний край стола: луч снаружи к Seat по направлению «к столу» попадает в триггер TableSurface.
                // Тогда любая фигура (сверху, снизу, напарник у торца) садится ровно к бортику, где бы Егор ни поставил Seat.
                Vector2 seatPos = transform.position;
                const float probe = 30f;
                RaycastHit2D hit = Physics2D.Raycast(seatPos - dir * probe, dir, probe * 2f, tableMask);
                if (hit.collider != null)
                {
                    float back = bodyHalfLength * visualScale - handsOnTable; // центр фигуры за краем стола
                    visualRoot.position = hit.point - dir * back;
                }
            }
            // Счёт — на зоне карт игрока (текст не поворачиваем, чтобы читался)
            if (scoreLabel != null && zone != null)
                scoreLabel.transform.position = zone.transform.position - (Vector3)(dir * labelOffsetOnZone);
        }

        // Спрайт зоны по роли + размер по коллайдеру (Егор меняет размер зоны в каждой раздаче)
        private void SetupZoneVisual()
        {
            if (zoneVisual == null) return;
            zoneVisual.sprite = isPartner ? partnerZoneSprite : opponentZoneSprite;
            if (zoneVisual.sprite == null || zone == null) return;

            BoxCollider2D box = zone.GetComponent<BoxCollider2D>();
            if (box == null) return;

            Vector2 spriteSize = zoneVisual.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;
            zoneVisual.transform.localPosition = box.offset;
            zoneVisual.transform.localScale = new Vector3(box.size.x / spriteSize.x, box.size.y / spriteSize.y, 1f);
        }

        private void UpdateZoneVisual(float t)
        {
            if (zoneVisual == null || isPartner) return; // зона напарника горит ровно

            float alpha = opponentIdleAlpha;
            if (aiming) alpha = Mathf.Lerp(opponentAimingMinAlpha, 1f, Pulse01(t, zonePulseFrequency));
            SetAlpha(zoneVisual, alpha);
        }

        private void UpdateGlow(float t)
        {
            if (!isPartner || glowRenderer == null) return;
            SetAlpha(glowRenderer, Mathf.Lerp(glowMinAlpha, 1f, Pulse01(t, glowPulseFrequency)));
        }

        private void UpdateAlert(float t)
        {
            if (scoreLabel == null || isPartner) return;
            if (!alert)
            {
                if (scoreLabel.color != baseLabelColor) scoreLabel.color = baseLabelColor;
                scoreLabel.transform.localScale = baseLabelScale;
                return;
            }
            scoreLabel.color = alertLabelColor;
            scoreLabel.transform.localScale = baseLabelScale * (1f + alertPulseAmount * Pulse01(t, alertPulseFrequency));
        }

        // 0..1..0 с заданной частотой
        private static float Pulse01(float time, float frequency) =>
            (Mathf.Sin(time * frequency * 2f * Mathf.PI) + 1f) * 0.5f;

        private static void SetAlpha(SpriteRenderer sr, float a)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }

        private void RefreshLabel()
        {
            if (scoreLabel != null) scoreLabel.text = score.ToString();
        }
    }
}
