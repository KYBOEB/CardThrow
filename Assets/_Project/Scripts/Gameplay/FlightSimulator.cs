using UnityEngine;

namespace CardThrow
{
    // Состояние летящей карты в один момент времени.
    public struct FlightState
    {
        public Vector2 pos;   // позиция центра карты (ед. мира)
        public Vector2 vel;   // скорость (ед/с)
        public float spin;    // кривизна дуги (рад/с): на сколько радиан в секунду поворачивается вектор скорости
        public float time;    // сколько секунд карта уже летит

        public bool IsStopped => vel == Vector2.zero;
    }

    // Что произошло за шаг с учётом столкновений.
    public enum StepEvent { None, Ricochet, HitBreakable, HitPushable }

    // Детерминированная модель полёта карты.
    // Launch/Step — чистая математика. StepWithCollisions добавляет столкновения (только ЧИТАЕТ физику).
    // Эти функции вызывают и CardFlight (в FixedUpdate), и TrajectoryPreview (в цикле),
    // поэтому превью траектории совпадает с реальным полётом.
    public static class FlightSimulator
    {
        // Отступ от препятствия после отскока: CircleCast, начатый внутри коллайдера, сразу «попадает» в него.
        private const float RicochetSkin = 0.02f;

        // Общий буфер для CircleCast — без выделения памяти на каждом шаге.
        private static readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

        // Начальное состояние броска.
        public static FlightState Launch(ThrowParams p, FlightSettings s)
        {
            Vector2 dir = p.direction.sqrMagnitude > 0f ? p.direction.normalized : Vector2.right;
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

        // Один шаг интегрирования длиной dt (обычно Time.fixedDeltaTime = 0.02 с). Без столкновений.
        public static FlightState Step(FlightState st, FlightSettings s, float dt)
        {
            float speed = st.vel.magnitude;

            // 1. Остановка: слишком медленно — карта легла.
            if (speed <= s.stopSpeed)
            {
                st.vel = Vector2.zero;
                return st;
            }

            // 2. Дуга: поворачиваем направление скорости, модуль не меняется.
            Vector2 dir = st.vel / speed;
            dir = Rotate(dir, st.spin * dt);

            // 3. Торможение: сопротивление (пропорционально скорости) + трение сукна (постоянное).
            speed = Mathf.Max(0f, speed - (s.linearDrag * speed + s.friction) * dt);

            // 4. Перемещение.
            st.vel = dir * speed;
            st.pos += st.vel * dt;

            // 5. Дуга постепенно затухает.
            st.spin *= Mathf.Max(0f, 1f - s.spinDecay * dt);
            st.time += dt;
            return st;
        }

        // Шаг с учётом препятствий. Перед перемещением «протягиваем» круг радиуса карты по пути за этот шаг
        // (CircleCast). Нет попадания — обычный Step. Попадание:
        //   Ricochet  — отражаем скорость от нормали, теряем часть энергии, кривизна дуги ×0.5;
        //   Breakable / Pushable — ставим карту в точку касания и сообщаем вызывающему.
        // Метод НИЧЕГО не ломает и не толкает: побочные эффекты делает только CardFlight.
        // ignore — коллайдер, который сейчас пропускаем (фишка, которую карта только что задела).
        public static StepEvent StepWithCollisions(ref FlightState st, FlightSettings s, float dt, Collider2D ignore,
            out Vector2 hitPoint, out Obstacle obstacle, out Collider2D hitCollider)
        {
            hitPoint = Vector2.zero;
            obstacle = null;
            hitCollider = null;

            float speed = st.vel.magnitude;
            if (speed > s.stopSpeed && s.obstacleMask.value != 0)
            {
                Vector2 dir = st.vel / speed;
                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(s.obstacleMask);
                filter.useTriggers = false;

                int count = Physics2D.CircleCast(st.pos, s.cardRadius, dir, filter, castHits, speed * dt);

                // Игнорируем не только сам коллайдер, но и весь предмет, которому он принадлежит:
                // стопка фишек — это несколько коллайдеров, а скорость карта должна потерять один раз.
                Obstacle ignoreObstacle = ignore != null ? ignore.GetComponentInParent<Obstacle>() : null;

                // Берём ближайшее попадание, пропуская игнорируемый предмет.
                int best = -1;
                float bestDistance = float.MaxValue;
                Obstacle bestObstacle = null;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D h = castHits[i];
                    if (h.collider == null || h.collider == ignore) continue;
                    Obstacle o = h.collider.GetComponentInParent<Obstacle>();
                    if (o == null || (ignoreObstacle != null && o == ignoreObstacle)) continue;
                    // Карта уже внутри пепельницы (distance = 0) — не отражаем, иначе она «застрянет», дёргаясь туда-обратно.
                    if (o.Kind == ObstacleKind.Ricochet && h.distance <= 0f) continue;
                    if (h.distance < bestDistance)
                    {
                        bestDistance = h.distance;
                        best = i;
                        bestObstacle = o;
                    }
                }

                if (best >= 0)
                {
                    RaycastHit2D hit = castHits[best];
                    hitPoint = hit.point;
                    hitCollider = hit.collider;
                    obstacle = bestObstacle;
                    st.time += dt;

                    switch (bestObstacle.Kind)
                    {
                        case ObstacleKind.Ricochet:
                            st.pos = hit.centroid + hit.normal * RicochetSkin;
                            st.vel = Vector2.Reflect(st.vel, hit.normal) * s.ricochetRestitution;
                            st.spin *= 0.5f;
                            return StepEvent.Ricochet;
                        case ObstacleKind.Breakable:
                            st.pos = hit.centroid;
                            return StepEvent.HitBreakable;
                        default:
                            st.pos = hit.centroid;
                            return StepEvent.HitPushable;
                    }
                }
            }

            st = Step(st, s, dt);
            return StepEvent.None;
        }

        // Поворот вектора на угол (радианы). Положительный угол = против часовой стрелки,
        // поэтому положительная дуга уводит карту влево относительно направления полёта.
        public static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians);
            float sn = Mathf.Sin(radians);
            return new Vector2(v.x * c - v.y * sn, v.x * sn + v.y * c);
        }
    }
}
