using System.Text;
using UnityEngine;

namespace CardThrow
{
    // Отладочный инструмент для песочницы (не для игры).
    // Повесь на пустой объект: в окне Scene будет нарисован путь карты из позиции этого объекта.
    // Меняй поля в инспекторе, и линия перерисуется сразу, даже без Play.
    public class FlightDebugGizmo : MonoBehaviour
    {
        [SerializeField] private FlightSettings settings;
        [SerializeField, Tooltip("Направление броска в градусах: 90 = вверх, 0 = вправо")]
        private float angleDeg = 90f;
        [SerializeField, Range(0f, 1f)] private float power01 = 1f;
        [SerializeField, Range(-3, 3), Tooltip("+ = дуга влево, − = вправо")]
        private int spinStep = 0;
        [SerializeField] private Color pathColor = Color.yellow;

        private void OnDrawGizmos()
        {
            if (settings == null) return;

            FlightState st = FlightSimulator.Launch(MakeParams(angleDeg, power01, spinStep), settings);
            float dt = Time.fixedDeltaTime;

            Gizmos.color = pathColor;
            for (int i = 0; i < settings.maxPreviewSteps && !st.IsStopped; i++)
            {
                Vector2 prev = st.pos;
                st = FlightSimulator.Step(st, settings, dt);
                Gizmos.DrawLine(prev, st.pos);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(st.pos, settings.cardRadius);
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
                Simulate(MakeParams(90f, p, 0), out float dist, out float time, out _);
                sb.AppendLine($"  сила {p * 100f,3:0}%  →  {dist,5:0.00} ед. за {time:0.00} с");
            }

            sb.AppendLine("Поворот траектории при силе 100%:");
            for (int s = 1; s <= settings.maxSpinStep; s++)
            {
                Simulate(MakeParams(90f, 1f, s), out _, out _, out float turn);
                sb.AppendLine($"  закрут {s}  →  {turn:0} градусов (влево)");
            }
            Debug.Log(sb.ToString());
        }

        // Прогоняет полёт до остановки: длина пути, время и угол между начальным и конечным направлением.
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
