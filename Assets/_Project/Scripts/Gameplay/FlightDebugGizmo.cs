using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CardThrow
{
    // Отладочный инструмент для песочницы (не для игры, в Main не добавлять).
    // В окне Scene рисует путь карты из позиции этого объекта — с отскоками от стаканов (голубые точки)
    // и крестиком на хрупком предмете или фишке. В Play: Space — бросить настоящую карту с этими же
    // параметрами (сравни с линией!), R — перезапустить сцену (разбитое восстановится).
    public class FlightDebugGizmo : MonoBehaviour
    {
        [SerializeField] private FlightSettings settings;
        [SerializeField, Tooltip("Карта в песочнице (для броска по Space)")]
        private CardFlight card;
        [SerializeField, Tooltip("Направление броска в градусах: 0 = вправо (к напарникам), 90 = вверх")]
        private float angleDeg = 0f;
        [SerializeField, Range(0f, 1f)] private float power01 = 1f;
        [SerializeField, Range(-3, 3), Tooltip("+ = дуга влево, − = вправо (относительно полёта)")]
        private int spinStep = 0;
        [SerializeField, Tooltip("Учитывать препятствия на линии (как будет в превью)")]
        private bool withCollisions = true;
        [SerializeField] private Color pathColor = Color.yellow;

        private void OnEnable() { GameEvents.CardFlightFinished += OnFlightFinished; }
        private void OnDisable() { GameEvents.CardFlightFinished -= OnFlightFinished; }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame && card != null && !card.IsFlying)
            {
                ThrowParams p = MakeParams(angleDeg, power01, spinStep);
                card.ResetCard();
                card.Launch(p);
            }
            if (kb.rKey.wasPressedThisFrame) ReloadScene();
        }

        // Песочницы нет в Build Settings, поэтому в редакторе перезагружаем сцену по пути.
        private static void ReloadScene()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
        }

        private void OnFlightFinished(FlightReport r)
        {
            Debug.Log($"[CT] Итог полёта: {r.result}, рикошетов {r.ricochets}, точность {r.zoneAccuracy01:0.00}, " +
                      $"точка ({r.finalPosition.x:0.00}; {r.finalPosition.y:0.00})");
        }

        private void OnDrawGizmos()
        {
            if (settings == null) return;
            if (!Application.isPlaying) Physics2D.SyncTransforms(); // в редакторе: учесть передвинутые препятствия

            FlightState st = FlightSimulator.Launch(MakeParams(angleDeg, power01, spinStep), settings);
            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < settings.maxPreviewSteps && !st.IsStopped; i++)
            {
                Vector2 prev = st.pos;
                if (withCollisions)
                {
                    StepEvent ev = FlightSimulator.StepWithCollisions(ref st, settings, dt, null,
                        out Vector2 hitPoint, out _, out _);
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(prev, st.pos);

                    if (ev == StepEvent.Ricochet)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(hitPoint, 0.12f);
                    }
                    else if (ev == StepEvent.HitBreakable || ev == StepEvent.HitPushable)
                    {
                        DrawCross(st.pos, 0.25f, Color.red);
                        return;
                    }
                }
                else
                {
                    st = FlightSimulator.Step(st, settings, dt);
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(prev, st.pos);
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(st.pos, settings.cardRadius);
        }

        private static void DrawCross(Vector2 c, float r, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(c + new Vector2(-r, -r), c + new Vector2(r, r));
            Gizmos.DrawLine(c + new Vector2(-r, r), c + new Vector2(r, -r));
        }

        // Правый клик по компоненту в инспекторе → «Print distances». Таблица для геймдизайнера.
        [ContextMenu("Print distances")]
        private void PrintDistances()
        {
            if (settings == null)
            {
                Debug.LogWarning("[CT] FlightDebugGizmo: не задан FlightSettings");
                return;
            }

            var sb = new StringBuilder("[CT] Дальность броска (закрут 0):\n");
            float[] powers = { 0.25f, 0.5f, 0.75f, 1f };
            foreach (float p in powers)
            {
                Simulate(MakeParams(0f, p, 0), out float dist, out float time, out _);
                sb.AppendLine($"  сила {p * 100f,3:0}%  →  {dist,5:0.00} ед. за {time:0.00} с");
            }

            sb.AppendLine("Поворот траектории при силе 100%:");
            for (int s = 1; s <= settings.maxSpinStep; s++)
            {
                Simulate(MakeParams(0f, 1f, s), out _, out _, out float turn);
                sb.AppendLine($"  закрут {s}  →  {turn:0} градусов (влево)");
            }
            Debug.Log(sb.ToString());
        }

        // Полёт без препятствий до остановки: длина пути, время и угол между начальным и конечным направлением.
        private void Simulate(ThrowParams p, out float distance, out float time, out float turnDeg)
        {
            FlightState st = FlightSimulator.Launch(p, settings);
            Vector2 startDir = p.direction;
            Vector2 lastDir = startDir;
            float dt = Time.fixedDeltaTime;
            distance = 0f;

            for (int i = 0; i < settings.maxPreviewSteps && !st.IsStopped; i++)
            {
                Vector2 prev = st.pos;
                st = FlightSimulator.Step(st, settings, dt);
                distance += Vector2.Distance(prev, st.pos);
                if (!st.IsStopped) lastDir = st.vel.normalized;
            }

            time = st.time;
            turnDeg = Vector2.SignedAngle(startDir, lastDir);
        }

        private ThrowParams MakeParams(float angle, float power, int spin)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new ThrowParams
            {
                origin = transform.position,
                direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)),
                power01 = power,
                spinStep = spin,
                cardValue = 0
            };
        }
    }
}
