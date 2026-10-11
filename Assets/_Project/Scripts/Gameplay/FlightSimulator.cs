using UnityEngine;

namespace CardThrow
{
    // Состояние летящей карты в один момент времени.
    public struct FlightState
    {
        public Vector2 pos;   // позиция центра карты (ед. мира)
        public Vector2 vel;   // скорость (ед/с)
        public float spin;    // закрут (рад/с): на сколько радиан в секунду поворачивается вектор скорости
        public float time;    // сколько секунд карта уже летит

        public bool IsStopped => vel == Vector2.zero;
    }

    // Модель полёта карты (CONTEXT, раздел 3). Владелец — Алексей.
    // Только математика: никакой физики Unity, никаких побочных эффектов.
    // Одни и те же функции вызывают CardFlight (в FixedUpdate) и TrajectoryPreview (в цикле),
    // поэтому превью траектории совпадает с реальным полётом.
    public static class FlightSimulator
    {
        // Начальное состояние броска.
        public static FlightState Launch(ThrowParams p, FlightSettings s)
        {
            Vector2 dir = p.direction.sqrMagnitude > 0f ? p.direction.normalized : Vector2.up;
            float power = Mathf.Clamp01(p.power01);
            int spinStep = Mathf.Clamp(p.spinStep, -s.maxSpinStep, s.maxSpinStep);

            return new FlightState
            {
                pos = p.origin,
                vel = dir * (power * s.maxSpeed),
                spin = spinStep * s.curvePerStep,
                time = 0f
            };
        }

        // Один шаг интегрирования длиной dt (обычно Time.fixedDeltaTime = 0.02 с).
        public static FlightState Step(FlightState st, FlightSettings s, float dt)
        {
            float speed = st.vel.magnitude;

            // 1. Остановка: слишком медленно — карта легла.
            if (speed <= s.stopSpeed)
            {
                st.vel = Vector2.zero;
                return st;
            }

            // 2. Закрут: поворачиваем направление скорости, модуль не меняется.
            Vector2 dir = st.vel / speed;
            dir = Rotate(dir, st.spin * dt);

            // 3. Торможение: сопротивление (пропорционально скорости) + трение сукна (постоянное).
            speed = Mathf.Max(0f, speed - (s.linearDrag * speed + s.friction) * dt);

            // 4. Перемещение.
            st.vel = dir * speed;
            st.pos += st.vel * dt;

            // 5. Закрут постепенно затухает.
            st.spin *= Mathf.Max(0f, 1f - s.spinDecay * dt);
            st.time += dt;
            return st;
        }

        // Поворот вектора на угол (радианы). Положительный угол = против часовой стрелки,
        // поэтому при броске вверх положительный закрут уводит карту влево.
        public static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians);
            float sn = Mathf.Sin(radians);
            return new Vector2(v.x * c - v.y * sn, v.x * sn + v.y * c);
        }
    }
}
