using TMPro;
using UnityEngine;

namespace CardThrow
{
    // Игрок за столом: счёт, поворот головы («смотрит на стол / отвернулся»), посадка у края стола, зона карт.
    // Зона напарника — золотая, соперника — красная; при прицеливании зоны пульсируют.
    // Соперник, поворачивающийся обратно к столу, подсвечивает свой счёт красным.
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
        [Tooltip("На сколько градусов игрок отворачивает голову. Шея человека поворачивается до ~80°")]
        [SerializeField, Range(20f, 90f)] private float headTurnAngle = 70f;
        [SerializeField] private float headTurnSpeed = 360f;   // градусов в секунду

        [Header("Посадка за столом")]
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
        [Tooltip("Слой триггера TableSurface")]
        [SerializeField] private LayerMask tableMask = 1 << 9;
        [Tooltip("Счёт рисуется поверх зоны карт игрока (его «стопка»). Сдвиг от центра зоны к игроку")]
        [SerializeField] private float labelOffsetOnZone = 0f;
        [Tooltip("Половина размера стола (19 × 9.5). По ней выбирается ближайший край — туда игрок и садится")]
        [SerializeField] private Vector2 tableHalfSize = new Vector2(9.5f, 4.75f);

        [Header("Зона на столе")]
        [Tooltip("SpriteRenderer внутри Zone (ZoneVisual). Размер подгоняется под BoxCollider2D зоны")]
        [SerializeField] private SpriteRenderer zoneVisual;
        [Tooltip("Зона не заходит на бортик: её внешний край — не ближе этого к внешнему краю стола (ед.). Лишнее обрезается и у коллайдера. " +
                 "Бортик в table_felt.png = 160 px / PPU 200 = 0.8 ед. + 0.1 зазор.")]
        [SerializeField] private float zoneRailWidth = 0.9f;
        [SerializeField] private Sprite partnerZoneSprite;     // Table/zone_partner
        [SerializeField] private Sprite opponentZoneSprite;    // Table/zone_opponent
        [Tooltip("Прозрачность зоны соперника вне прицеливания")]
        [SerializeField, Range(0f, 1f)] private float opponentIdleAlpha = 0.35f;
        [Tooltip("Прозрачность зоны соперника при прицеливании: пульс между min и 1")]
        [SerializeField, Range(0f, 1f)] private float opponentAimingMinAlpha = 0.7f;
        [SerializeField] private float zonePulseFrequency = 1.5f;   // ударов в секунду

        [Header("Напарник")]
        [SerializeField] private Color partnerLabelColor = new Color(1f, 0.82f, 0.3f);
        [SerializeField, Range(0f, 1f)] private float glowMinAlpha = 0.55f;
        [SerializeField] private float glowPulseFrequency = 0.8f;
        [Tooltip("Насколько ярок контур вокруг напарника. Главная цель — его ЗОНА карт, а не фигура: контур приглушён")]
        [SerializeField, Range(0f, 1f)] private float glowMaxAlpha = 0.35f;
        [Tooltip("Зона напарника при прицеливании пульсирует между этим значением и 1 — «кидай сюда»")]
        [SerializeField, Range(0f, 1f)] private float partnerAimingMinAlpha = 0.55f;

        [Header("Разнообразие")]
        [Tooltip("Visual/Body — спрайт тела; голова — SpriteRenderer на head")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("player_body_1..4. В одной раздаче пары «тело + голова» не повторяются")]
        [SerializeField] private Sprite[] bodySprites;
        [SerializeField] private Sprite[] headSprites;
        [Tooltip("Соперникам — разный счёт вместо одинаковых 12 (напарнику счёт задаёт раздача)")]
        [SerializeField] private bool randomOpponentScore = true;
        [Tooltip("Диапазон счёта соперника, включительно. 21 не берём — у соперника не должно быть «блэкджека»")]
        [SerializeField] private Vector2Int opponentScoreRange = new Vector2Int(12, 20);

        [Header("Официантка отвлекает соперника")]
        [Tooltip("Characters/waitress.png. Пусто — соперник просто отворачивается, без официантки")]
        [SerializeField] private Sprite waitressSprite;
        [Tooltip("Официантка стоит СБОКУ от соперника, вдоль края стола: столько ед. от центра его фигуры. " +
                 "Фигура игрока ≈ 2.6 ед. в ширину, официантка ≈ 2 ед. → 2.4 — рядом, но не впритык")]
        [SerializeField] private float waitressSide = 2.4f;
        [Tooltip("…и на столько ед. дальше от стола, чем центр фигуры: на сукно она не заходит")]
        [SerializeField] private float waitressBack = 0.7f;
        [Tooltip("Масштаб официантки (PPU спрайта подогнан под игроков). 0.9 — чуть меньше игрока")]
        [SerializeField] private float waitressScale = 0.9f;
        [Tooltip("Затемнение и прозрачность: официантка — фон, рисуется ПОД игроками")]
        [SerializeField] private Color waitressTint = new Color(0.45f, 0.45f, 0.5f, 0.9f);
        [SerializeField] private float waitressEnterTime = 0.5f;
        [Tooltip("Карта влетела в зону этого соперника — он резко поворачивается к столу (градусов в секунду)")]
        [SerializeField] private float caughtTurnSpeed = 1440f;

        [Header("Тревога соперника")]
        [SerializeField] private Color alertLabelColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private float alertPulseAmount = 0.25f;
        [SerializeField] private float alertPulseFrequency = 3f;

        private float baseHeadAngle;
        private float turnSign = 1f;   // в какую сторону отворачиваться: всегда от крупье (к правому торцу стола)
        private float targetHeadAngle;
        private float currentTurnSpeed;

        // Кэш: заполняется в Awake, чтобы не вызывать GetComponent в Update
        private SpriteRenderer glowRenderer;
        private Color baseLabelColor = Color.white;
        private Vector3 baseLabelScale = Vector3.one;
        private bool aiming;     // игра в состоянии Aiming
        private bool alert;      // соперник повернулся обратно к столу
        private bool caught;     // карта в его зоне — «Шулер!»: тревога держится до новой раздачи
        private Collider2D zoneCollider;
        private Vector2 tableDir = Vector2.up;  // «к столу» (считается в LayoutAtTable)
        private Vector2 tableEdgePoint;         // точка края стола напротив места (луч в LayoutAtTable)
        private bool hasTableEdge;

        // Официантка: появляется, пока соперник отвернулся, уходит, когда он поворачивается обратно
        private SpriteRenderer waitress;
        private Vector2 sideDir = Vector2.right;  // куда отворачивается голова и где встаёт официантка
        private Vector3 waitressNear, waitressFar;
        private float waitressT, waitressTarget, waitressSpeed;

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

            ApplyVariety();
            LayoutAtTable();
            SetupZoneVisual();
            PlaceScoreLabel();
            SetupWaitress();
            if (zone != null) zoneCollider = zone.GetComponent<Collider2D>();
            RefreshLabel();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.CardFlightFinished += OnCardFlightFinished;
            // Раздача создаётся до SetState(HandIntro), но на всякий случай берём текущее состояние
            aiming = GameManager.Instance != null && GameManager.Instance.State == GameState.Aiming;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.CardFlightFinished -= OnCardFlightFinished;
        }

        public void SetScore(int value)
        {
            score = value;
            RefreshLabel();
        }

        public void SetLookingAway(bool away)
        {
            targetHeadAngle = baseHeadAngle + (away ? LookAwayAngle : 0f);
            currentTurnSpeed = headTurnSpeed;
            alert = false; // мгновенный поворот — это служебный сброс, а не «заметил»
            MoveWaitress(away, waitressEnterTime);
        }

        // Плавный поворот за заданное время (например, за 3 с до конца «окна внимания»).
        // Поворот обратно к столу = соперник вот-вот заметит → тревога на счёте.
        public void SetLookingAway(bool away, float turnDuration)
        {
            targetHeadAngle = baseHeadAngle + (away ? LookAwayAngle : 0f);
            currentTurnSpeed = turnDuration > 0.01f ? headTurnAngle / turnDuration : headTurnSpeed;
            alert = !away && !isPartner;
            MoveWaitress(away, turnDuration); // официантка уходит ровно к моменту, когда он смотрит на стол
        }

        private float LookAwayAngle => turnSign * headTurnAngle;

        private void OnStateChanged(GameState state)
        {
            aiming = state == GameState.Aiming;
            if (state != GameState.Aiming && state != GameState.Flying) alert = false;
            if (state == GameState.HandIntro || state == GameState.Title) caught = false;
        }

        // Карта влетела в МОЮ зону: резко поворачиваюсь к столу, официантка исчезает, счёт горит красным
        private void OnCardFlightFinished(FlightReport report)
        {
            if (isPartner || zoneCollider == null || report.result != FlightResult.EnteredOpponentZone) return;
            if (!zoneCollider.OverlapPoint(report.finalPosition)) return;
            caught = true;
            targetHeadAngle = baseHeadAngle;
            currentTurnSpeed = caughtTurnSpeed;
            MoveWaitress(false, 0.15f);
        }

        // ---------- Официантка ----------

        private void SetupWaitress()
        {
            if (waitressSprite == null || isPartner) return;

            // Сбоку от фигуры соперника и чуть дальше от стола: «подошла к его плечу из зала».
            Vector3 figure = visualRoot != null ? visualRoot.position : transform.position - (Vector3)(tableDir * pushBack);
            waitressNear = figure + (Vector3)(sideDir * waitressSide - tableDir * waitressBack);
            waitressFar = waitressNear + (Vector3)(sideDir * 1.5f); // приходит сбоку, из зала

            GameObject go = new GameObject("Waitress");
            go.transform.SetParent(transform, true);
            Vector2 toPlayer = (Vector2)(figure - waitressNear);
            float faceDeg = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f; // спрайт нарисован «вверх»
            go.transform.rotation = Quaternion.Euler(0f, 0f, faceDeg);
            go.transform.localScale = Vector3.one * waitressScale;
            waitress = go.AddComponent<SpriteRenderer>();
            waitress.sprite = waitressSprite;
            // Поднос у неё в правой руке. Если правая рука смотрит на стол — зеркалим: поднос всегда со стороны зала,
            // а не над бортиком
            float rad = faceDeg * Mathf.Deg2Rad;
            Vector2 spriteRight = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            waitress.flipX = Vector2.Dot(spriteRight, tableDir) > 0f;
            if (bodyRenderer != null)
            {
                waitress.sortingLayerID = bodyRenderer.sortingLayerID;
                waitress.sortingOrder = bodyRenderer.sortingOrder - 1; // второй план: под телами всех игроков
            }
            ApplyWaitress();
        }

        // Направление «вбок вдоль края стола», куда смотрит голова при данном знаке поворота
        // (поворот tableDir на ±90°; +1 — против часовой, как и поворот головы)
        private Vector2 SideFor(float sign) =>
            sign > 0f ? new Vector2(-tableDir.y, tableDir.x) : new Vector2(tableDir.y, -tableDir.x);

        // Сидит ли с этой стороны сосед по тому же краю стола ближе, чем нужно официантке
        private bool SideBlocked(Vector2 side)
        {
            if (transform.parent == null) return false;
            Vector2 me = transform.position;
            foreach (Seat other in transform.parent.GetComponentsInChildren<Seat>(true))
            {
                if (other == this) continue;
                Vector2 d = (Vector2)other.transform.position - me;
                float along = Vector2.Dot(d, side);
                float across = Mathf.Abs(Vector2.Dot(d, tableDir));
                if (across < 1.5f && along > 0f && along < waitressSide + 0.9f) return true;
            }
            return false;
        }

        private void MoveWaitress(bool come, float duration)
        {
            if (waitress == null) return;
            waitressTarget = come ? 1f : 0f;
            waitressSpeed = 1f / Mathf.Max(0.05f, duration);
        }

        private void ApplyWaitress()
        {
            float k = waitressT * waitressT * (3f - 2f * waitressT); // плавный старт и остановка
            waitress.transform.position = Vector3.Lerp(waitressFar, waitressNear, k);
            Color c = waitressTint;
            c.a = waitressTint.a * k;
            waitress.color = c;
            waitress.enabled = waitressT > 0.001f;
        }

        private void Update()
        {
            if (head != null)
            {
                float z = Mathf.MoveTowardsAngle(head.localEulerAngles.z, targetHeadAngle, currentTurnSpeed * Time.deltaTime);
                head.localEulerAngles = new Vector3(0f, 0f, z);
            }

            if (waitress != null && !Mathf.Approximately(waitressT, waitressTarget))
            {
                waitressT = Mathf.MoveTowards(waitressT, waitressTarget, waitressSpeed * Time.deltaTime);
                ApplyWaitress();
            }

            float t = Time.unscaledTime; // пульсы не замедляются в slow-mo
            UpdateZoneVisual(t);
            UpdateGlow(t);
            UpdateAlert(t);
        }

        // ---------- Визуал ----------

        // Фигура «садится» за стол: руки (верх спрайта, +Y) смотрят на стол, сама фигура — за краем.
        // Seat в раздачах не повёрнут и не масштабирован (rotation 0, scale 1).
        private void LayoutAtTable()
        {
            // Направление «к столу» = нормаль ближайшего края стола (центр стола в (0,0)).
            // Не зависит от положения зоны: зону можно сдвигать, фигура не развернётся.
            Vector2 p = transform.position;
            Vector2 dir = Mathf.Abs(p.x) / tableHalfSize.x > Mathf.Abs(p.y) / tableHalfSize.y
                ? new Vector2(-Mathf.Sign(p.x), 0f)
                : new Vector2(0f, p.y == 0f ? 1f : -Mathf.Sign(p.y));

            // Поворот головы против часовой (+) уводит взгляд игрока сверху стола вправо, а игрока снизу — влево.
            // Поэтому снизу знак минус: все отворачиваются от крупье (он слева), будто смотрят в зал.
            tableDir = dir;
            turnSign = dir.y <= 0f ? 1f : -1f;
            // Официантке нужно свободное место сбоку. Если с «правильной» стороны сидит сосед, а с другой пусто —
            // отворачиваемся в другую сторону (голова всегда смотрит туда, где официантка)
            if (!isPartner && SideBlocked(SideFor(turnSign)) && !SideBlocked(SideFor(-turnSign))) turnSign = -turnSign;
            sideDir = SideFor(turnSign);

            if (visualRoot != null)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // спрайт нарисован руками вверх
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
                visualRoot.localScale = Vector3.one * visualScale;
                visualRoot.localPosition = -dir * pushBack; // запасной вариант

                // Ищем внешний край стола: луч снаружи к Seat по направлению «к столу» попадает в триггер TableSurface.
                // Тогда любая фигура (сверху, снизу, напарник у торца) садится ровно к бортику, где бы ни стоял Seat.
                Vector2 seatPos = transform.position;
                const float probe = 30f;
                RaycastHit2D hit = Physics2D.Raycast(seatPos - dir * probe, dir, probe * 2f, tableMask);
                if (hit.collider != null)
                {
                    tableEdgePoint = hit.point;
                    hasTableEdge = true;
                    float back = bodyHalfLength * visualScale - handsOnTable; // центр фигуры за краем стола
                    visualRoot.position = hit.point - dir * back;
                }
            }
            // Счёт ставится в PlaceScoreLabel() — уже после обрезки зоны по бортику
        }

        // Внешность и счёт соперника — «случайные», но стабильные: при переигровке раздачи стол не меняется.
        // Внешность — по номеру места в раздаче (комбинации не повторяются), счёт — по позиции места.
        private void ApplyVariety()
        {
            int nb = bodySprites != null ? bodySprites.Length : 0;
            int nh = headSprites != null ? headSprites.Length : 0;
            if (nb > 0 && nh > 0)
            {
                // Номер места среди соседей по раздаче (Seats/…) + сдвиг от названия раздачи.
                // i = q·nb + r → тело (r + сдвиг), голова (r + q + сдвиг): пары «тело+голова» не повторяются
                // для первых nb·nh мест (4×4 = 16), а у первых четырёх различаются и тела, и головы.
                Seat[] seats = transform.parent != null ? transform.parent.GetComponentsInChildren<Seat>(true) : new[] { this };
                int i = Mathf.Max(0, System.Array.IndexOf(seats, this));
                int shift = LevelSalt();
                int r = i % nb, q = i / nb;
                if (bodyRenderer != null) bodyRenderer.sprite = bodySprites[(r + shift) % nb];
                SpriteRenderer headRenderer = head != null ? head.GetComponent<SpriteRenderer>() : null;
                if (headRenderer != null) headRenderer.sprite = headSprites[(r + q + shift) % nh];
            }

            if (!isPartner && randomOpponentScore)
            {
                int min = Mathf.Min(opponentScoreRange.x, opponentScoreRange.y);
                int max = Mathf.Max(opponentScoreRange.x, opponentScoreRange.y);
                score = min + StableIndex(3, max - min + 1);
            }
        }

        // Сдвиг внешностей от названия раздачи («Hand_02(Clone)»): на разных уровнях за тем же местом — другой человек.
        // Своя хеш-функция, а не string.GetHashCode: результат одинаков на Windows и Mac.
        private int LevelSalt()
        {
            Transform level = transform.parent != null ? transform.parent.parent : null;
            if (level == null) return 0;
            int h = 0;
            foreach (char c in level.name) h = (h * 31 + c) & 0x7fffffff;
            return h % 97;
        }

        // Детерминированный «случайный» индекс 0..count−1 из позиции места и соли (без UnityEngine.Random,
        // чтобы не сбивать общий генератор, которым ResultPopup выбирает фразы)
        private int StableIndex(int salt, int count)
        {
            if (count <= 1) return 0;
            Vector2 p = transform.position;
            unchecked
            {
                uint h = (uint)Mathf.RoundToInt(p.x * 10f) * 73856093u ^ (uint)Mathf.RoundToInt(p.y * 10f) * 19349663u ^ (uint)salt * 83492791u;
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return (int)(h % (uint)count);
            }
        }

        // Спрайт зоны по роли + размер по коллайдеру (размер зоны свой в каждой раздаче)
        private void SetupZoneVisual()
        {
            if (zoneVisual == null) return;
            zoneVisual.sprite = isPartner ? partnerZoneSprite : opponentZoneSprite;
            if (zoneVisual.sprite == null || zone == null) return;

            BoxCollider2D box = zone.GetComponent<BoxCollider2D>();
            if (box == null) return;

            ClipZoneToTable(box);
            Vector2 spriteSize = zoneVisual.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;
            zoneVisual.transform.localPosition = box.offset;
            zoneVisual.transform.localScale = new Vector3(box.size.x / spriteSize.x, box.size.y / spriteSize.y, 1f);
        }

        // Счёт — в центре зоны карт игрока (текст не поворачиваем, чтобы читался).
        // Центр берём ПОСЛЕ ClipZoneToTable: обрезка сдвигает box.offset, а transform зоны остаётся на месте.
        private void PlaceScoreLabel()
        {
            if (scoreLabel == null || zone == null) return;
            BoxCollider2D box = zone.GetComponent<BoxCollider2D>();
            Vector3 center = box != null ? zone.transform.TransformPoint(box.offset) : zone.transform.position;
            center.z = scoreLabel.transform.position.z;
            scoreLabel.transform.position = center - (Vector3)(tableDir * labelOffsetOnZone);
        }

        // Зона, заходящая за край стола, бесполезна (карта там уже «улетела со стола») и некрасиво лежит на бортике.
        // Обрезаем её со стороны игрока до края сукна; коллайдер и картинка остаются одинаковыми.
        private void ClipZoneToTable(BoxCollider2D box)
        {
            if (!hasTableEdge) return;
            bool horizontal = Mathf.Abs(tableDir.x) > 0.5f;
            int axis = horizontal ? 0 : 1;
            float d = horizontal ? tableDir.x : tableDir.y;                // +1/−1: куда «к столу»
            Vector2 center = (Vector2)zone.transform.position + box.offset; // зона не повёрнута и без масштаба (как в префабе)
            float half = box.size[axis] * 0.5f;
            float outer = center[axis] - d * half;                         // край зоны со стороны игрока
            float limit = tableEdgePoint[axis] + d * zoneRailWidth;        // дальше этого — бортик
            float excess = (limit - outer) * d;                            // > 0 — зона залезла на бортик
            if (excess <= 0f) return;

            excess = Mathf.Min(excess, box.size[axis] - 0.4f);            // совсем в ноль не схлопываем
            Vector2 size = box.size;
            Vector2 offset = box.offset;
            size[axis] -= excess;
            offset[axis] += d * excess * 0.5f;                             // внутренний край зоны остаётся на месте
            box.size = size;
            box.offset = offset;
        }

        private void UpdateZoneVisual(float t)
        {
            if (zoneVisual == null) return;

            float alpha;
            if (isPartner) // цель броска: при прицеливании «дышит» золотом, иначе горит ровно
                alpha = aiming ? Mathf.Lerp(partnerAimingMinAlpha, 1f, Pulse01(t, zonePulseFrequency)) : 1f;
            else
                alpha = aiming ? Mathf.Lerp(opponentAimingMinAlpha, 1f, Pulse01(t, zonePulseFrequency)) : opponentIdleAlpha;
            SetAlpha(zoneVisual, alpha);
        }

        private void UpdateGlow(float t)
        {
            if (!isPartner || glowRenderer == null) return;
            SetAlpha(glowRenderer, glowMaxAlpha * Mathf.Lerp(glowMinAlpha, 1f, Pulse01(t, glowPulseFrequency)));
        }

        private void UpdateAlert(float t)
        {
            if (scoreLabel == null || isPartner) return;
            if (!alert && !caught)
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
