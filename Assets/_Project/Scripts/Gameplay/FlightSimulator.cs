using UnityEngine;

namespace CardThrow
{
    public struct FlightState
    {
        public Vector2 pos;
        public Vector2 vel;
        public float spin;   // рад/с
        public float time;

        public bool IsStopped => vel == Vector2.zero;
    }

    // ЗАГЛУШКА (владелец — Алексей). Чистые функции без коллизий; формула — CONTEXT, раздел 3.
    // Используется и CardFlight (FixedUpdate), и TrajectoryPreview (цикл превью).
    public static class FlightSimulator
    {
        public static FlightState Launch(ThrowParams p, FlightSettings s)
        {
            // TODO (Алексей, промпт A1)
            return new FlightState { pos = p.origin };
        }

        public static FlightState Step(FlightState st, FlightSettings s, float dt)
        {
            // TODO (Алексей, промпт A1)
            return st;
        }
    }
}
