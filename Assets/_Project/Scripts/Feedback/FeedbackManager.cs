using TMPro;
using UnityEngine;

namespace CardThrow
{
    // «Сок» игры (game feel): тряска камеры, стоп-кадр на ударе, зум-толчок на успехе, брызги частиц, всплывающие очки.
    // Только СЛУШАЕТ GameEvents и ничего не решает — исход броска, очки и состояние по-прежнему у GameManager.
    // Частицы — свой пул из спрайтов (без ParticleSystem и без аллокаций в игре): каждый эффект — пара строк настройки,
    // а время — unscaledDeltaTime, поэтому эффекты живут и во время стоп-кадра.
    // Владелец — Данила (передано Алексеем 12.10, задача A5).
    public class FeedbackManager : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private UiTexts texts;
        [SerializeField] private TMP_FontAsset popupFont;

        [Header("Спрайты частиц (Art/Sprites/VFX)")]
        [SerializeField] private Sprite shardSprite;
        [SerializeField] private Sprite foamSprite;
        [SerializeField] private Sprite sparkleSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite dustSprite;
        [SerializeField] private Sprite starSprite;

        [Header("Тряска камеры (ед. мира)")]
        [SerializeField] private float shakeSmall = 0.06f;
        [SerializeField] private float shakeMedium = 0.15f;
        [SerializeField] private float shakeStrong = 0.32f;
        [SerializeField] private float shakeDuration = 0.25f;

        [Header("Стоп-кадр (hit-stop)")]
        [Tooltip("Время игры замедляется до этой доли на короткий миг удара")]
        [SerializeField, Range(0f, 1f)] private float hitStopScale = 0.05f;
        [SerializeField] private float hitStopBreakable = 0.08f;
        [SerializeField] private float hitStopRicochet = 0.035f;

        [Header("Зум-толчок на успехе")]
        [SerializeField, Range(0f, 0.2f)] private float zoomPunch = 0.06f;
        [SerializeField] private float zoomDuration = 0.35f;

        [Header("Частицы")]
        [SerializeField] private int poolSize = 96;
        [SerializeField] private string sortingLayer = "FX";
        [SerializeField] private Color glassSparkColor = new Color(0.7f, 0.9f, 1f);
        [SerializeField] private Color dustColor = new Color(1f, 1f, 1f, 0.6f);

        [Header("Всплывающие очки")]
        [SerializeField] private float popupFontSize = 7f;
        [SerializeField] private float popupRise = 1.2f;
        [SerializeField] private float popupDuration = 1.1f;
        [SerializeField] private Color popupColor = new Color(1f, 0.82f, 0.3f);

        private const int PopupPoolSize = 4;

        private struct Particle
        {
            public SpriteRenderer sr;
            public Vector2 vel;
            public float life, maxLife, spin, drag, size0, size1;
            public Color color;
            public bool active;
        }

        private struct Popup
        {
            public TextMeshPro text;
            public Vector3 start;
            public float life;
            public bool active;
        }

        private Particle[] particles;
        private Popup[] popups;
        private int nextParticle, nextPopup;

        private Camera cam;
        private Vector3 camBasePos;
        private float shakeAmp, shakeLeft;
        private float zoomBaseSize, zoomLeft;

        private bool hitStopActive;
        private float hitStopUntil, savedTimeScale = 1f;

        private Vector2 lastFinalPos;

        // ---------- Жизненный цикл ----------

        private void Awake()
        {
            cam = Camera.main; // один раз; у Main Camera тег MainCamera
            if (cam != null) camBasePos = cam.transform.position;
            else Debug.LogWarning("[CT] FeedbackManager: нет камеры с тегом MainCamera — тряски не будет", this);

            particles = new Particle[Mathf.Max(8, poolSize)];
            for (int i = 0; i < particles.Length; i++)
            {
                GameObject go = new GameObject("Fx", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                sr.sortingLayerName = sortingLayer;
                sr.enabled = false;
                particles[i].sr = sr;
            }

            popups = new Popup[PopupPoolSize];
            for (int i = 0; i < popups.Length; i++)
            {
                GameObject go = new GameObject("Popup");
                go.transform.SetParent(transform, false);
                TextMeshPro t = go.AddComponent<TextMeshPro>();
                if (popupFont != null) t.font = popupFont;
                t.fontSize = popupFontSize;
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center;
                t.rectTransform.sizeDelta = new Vector2(8f, 2f);
                t.outlineWidth = 0.2f;
                t.outlineColor = new Color32(40, 20, 0, 255);
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                mr.sortingLayerName = sortingLayer;
                mr.sortingOrder = 100; // поверх частиц
                go.SetActive(false);
                popups[i].text = t;
            }
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.CardThrown += OnCardThrown;
            GameEvents.CardHitObstacle += OnCardHitObstacle;
            GameEvents.CardFlightFinished += OnCardFlightFinished;
            GameEvents.ThrowResolved += OnThrowResolved;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.CardThrown -= OnCardThrown;
            GameEvents.CardHitObstacle -= OnCardHitObstacle;
            GameEvents.CardFlightFinished -= OnCardFlightFinished;
            GameEvents.ThrowResolved -= OnThrowResolved;
            EndHitStop();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (hitStopActive && Time.realtimeSinceStartup >= hitStopUntil) EndHitStop();
            UpdateParticles(dt);
            UpdatePopups(dt);
        }

        // Камера — в LateUpdate, после всех перемещений кадра
        private void LateUpdate()
        {
            if (cam == null) return;
            float dt = Time.unscaledDeltaTime;

            if (shakeLeft > 0f)
            {
                shakeLeft -= dt;
                float k = Mathf.Clamp01(shakeLeft / shakeDuration); // затухает к концу
                Vector2 offset = Random.insideUnitCircle * (shakeAmp * k);
                cam.transform.position = camBasePos + (Vector3)offset;
            }
            else if (cam.transform.position != camBasePos)
            {
                cam.transform.position = camBasePos;
            }

            if (zoomLeft > 0f)
            {
                zoomLeft -= dt;
                float t = 1f - Mathf.Clamp01(zoomLeft / zoomDuration);
                cam.orthographicSize = zoomBaseSize * (1f - zoomPunch * Mathf.Sin(t * Mathf.PI)); // туда и обратно
                if (zoomLeft <= 0f) cam.orthographicSize = zoomBaseSize;
            }
        }

        // ---------- События ----------

        private void OnStateChanged(GameState state)
        {
            // Новая раздача или экран — всё «сокоделие» прошлого броска гасим сразу
            if (state == GameState.HandIntro || state == GameState.Title)
            {
                EndHitStop();
                shakeLeft = 0f;
                if (zoomLeft > 0f && cam != null) cam.orthographicSize = zoomBaseSize;
                zoomLeft = 0f;
            }
        }

        private void OnCardThrown(ThrowParams p)
        {
            Burst(dustSprite, p.origin, 6, 1.5f, 0.4f, 0.5f, 1.2f, dustColor, 0f);
            Shake(shakeSmall * 0.6f);
        }

        private void OnCardHitObstacle(Vector2 point, ObstacleKind kind)
        {
            switch (kind)
            {
                case ObstacleKind.Ricochet: // звон стакана: холодные искры
                    Burst(sparkleSprite, point, 10, 4f, 0.35f, 0.6f, 0.2f, glassSparkColor, 360f);
                    Burst(starSprite, point, 3, 2f, 0.4f, 0.5f, 0.1f, Color.white, 180f);
                    Shake(shakeSmall);
                    HitStop(hitStopRicochet);
                    break;

                case ObstacleKind.Pushable: // фишки: пыль, карта летит дальше
                    Burst(dustSprite, point, 8, 2.5f, 0.45f, 0.6f, 1.3f, dustColor, 0f);
                    Shake(shakeSmall);
                    break;

                case ObstacleKind.Breakable: // бутылка/домик: осколки + пена, стоп-кадр и сильная тряска
                    Burst(shardSprite, point, 16, 6f, 0.6f, 0.9f, 0.5f, Color.white, 720f);
                    Burst(foamSprite, point, 10, 2.5f, 0.8f, 0.6f, 1.6f, Color.white, 0f);
                    Shake(shakeStrong * 0.7f);
                    HitStop(hitStopBreakable);
                    break;
            }
        }

        private void OnCardFlightFinished(FlightReport report)
        {
            lastFinalPos = report.finalPosition;
            if (report.result == FlightResult.OffTable)
                Burst(dustSprite, lastFinalPos, 6, 1.5f, 0.5f, 0.5f, 1.2f, dustColor, 0f);
        }

        private void OnThrowResolved(ThrowOutcome outcome, int gained)
        {
            switch (outcome)
            {
                case ThrowOutcome.Success: // дождь монет, звёзды, зум и «+очки»
                    Burst(coinSprite, lastFinalPos, 18, 5f, 1.0f, 0.9f, 0.7f, Color.white, 540f);
                    Burst(starSprite, lastFinalPos, 8, 3f, 0.8f, 0.8f, 0.2f, Color.white, 360f);
                    Shake(shakeSmall);
                    ZoomPunch();
                    if (gained > 0) ShowPopup(lastFinalPos, FormatGained(gained), popupColor);
                    break;

                case ThrowOutcome.OpponentCaught:
                    Shake(shakeStrong);
                    break;

                case ThrowOutcome.TimeOut:
                    Shake(shakeMedium);
                    break;

                case ThrowOutcome.WrongValue:
                case ThrowOutcome.StoppedOnTable:
                case ThrowOutcome.OffTable:
                    Shake(shakeSmall);
                    break;
                // HitBreakable уже «отыгран» в OnCardHitObstacle
            }
        }

        // ---------- Эффекты ----------

        private void Shake(float amplitude)
        {
            if (cam == null) return;
            if (shakeLeft <= 0f || amplitude >= shakeAmp) shakeAmp = amplitude; // сильный удар не гасится слабым
            shakeLeft = shakeDuration;
        }

        private void ZoomPunch()
        {
            if (cam == null || zoomPunch <= 0f) return;
            if (zoomLeft <= 0f) zoomBaseSize = cam.orthographicSize; // база — размер от CameraFitter
            zoomLeft = zoomDuration;
        }

        private void HitStop(float duration)
        {
            if (duration <= 0f || Time.timeScale <= 0f) return;
            if (!hitStopActive)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = savedTimeScale * hitStopScale;
                hitStopActive = true;
            }
            hitStopUntil = Mathf.Max(hitStopUntil, Time.realtimeSinceStartup + duration);
        }

        private void EndHitStop()
        {
            if (!hitStopActive) return;
            hitStopActive = false;
            // Возвращаем время, только если его не поменял кто-то другой (например, GameManager при рестарте)
            if (Mathf.Approximately(Time.timeScale, savedTimeScale * hitStopScale)) Time.timeScale = savedTimeScale;
        }

        // Разлёт count частиц из точки: скорость до speed ед/с, жизнь ~life с, размер size0 → size1, вращение до spin °/с
        private void Burst(Sprite sprite, Vector2 point, int count, float speed, float life,
                           float size0, float size1, Color color, float spin)
        {
            if (sprite == null) return;
            for (int n = 0; n < count; n++)
            {
                ref Particle p = ref particles[nextParticle];
                nextParticle = (nextParticle + 1) % particles.Length; // пул по кругу: старые частицы переиспользуются

                p.vel = Random.insideUnitCircle.normalized * (speed * Random.Range(0.4f, 1f));
                p.maxLife = life * Random.Range(0.7f, 1.2f);
                p.life = 0f;
                p.spin = Random.Range(-spin, spin);
                p.drag = 3f;
                p.size0 = size0 * Random.Range(0.8f, 1.2f);
                p.size1 = size1 * p.size0 / Mathf.Max(0.01f, size0);
                p.color = color;
                p.active = true;

                p.sr.sprite = sprite;
                p.sr.color = color;
                p.sr.enabled = true;
                p.sr.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
                p.sr.transform.localScale = Vector3.one * p.size0;
            }
        }

        private void UpdateParticles(float dt)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ref Particle p = ref particles[i];
                if (!p.active) continue;

                p.life += dt;
                float t = p.life / p.maxLife;
                if (t >= 1f)
                {
                    p.active = false;
                    p.sr.enabled = false;
                    continue;
                }

                p.vel *= Mathf.Max(0f, 1f - p.drag * dt); // трение о сукно: разлёт быстро гаснет
                Transform tr = p.sr.transform;
                tr.position += (Vector3)(p.vel * dt);
                tr.Rotate(0f, 0f, p.spin * dt);
                tr.localScale = Vector3.one * Mathf.Lerp(p.size0, p.size1, t);

                Color c = p.color;
                c.a *= 1f - t * t; // держится и быстро тает в конце
                p.sr.color = c;
            }
        }

        private void ShowPopup(Vector2 point, string text, Color color)
        {
            ref Popup p = ref popups[nextPopup];
            nextPopup = (nextPopup + 1) % popups.Length;

            p.start = new Vector3(point.x, point.y + 0.4f, 0f);
            p.life = 0f;
            p.active = true;
            p.text.text = text;
            p.text.color = color;
            p.text.transform.position = p.start;
            p.text.transform.localScale = Vector3.one;
            p.text.gameObject.SetActive(true);
        }

        private void UpdatePopups(float dt)
        {
            for (int i = 0; i < popups.Length; i++)
            {
                ref Popup p = ref popups[i];
                if (!p.active) continue;

                p.life += dt;
                float t = p.life / popupDuration;
                if (t >= 1f)
                {
                    p.active = false;
                    p.text.gameObject.SetActive(false);
                    continue;
                }

                float rise = 1f - (1f - t) * (1f - t);                  // ease-out: быстро вверх, потом зависает
                float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) * 4f);
                p.text.transform.position = p.start + Vector3.up * (popupRise * rise);
                p.text.transform.localScale = Vector3.one * pop; // «выпрыгивает» 0.6 → 1.15 → 1

                Color c = p.text.color;
                c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                p.text.color = c;
            }
        }

        private string FormatGained(int gained)
        {
            string number = UiTexts.FormatNumber(gained);
            return texts != null && !string.IsNullOrEmpty(texts.scoreGainedFormat)
                ? string.Format(texts.scoreGainedFormat, number)
                : "+" + number;
        }
    }
}
