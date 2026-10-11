using UnityEngine;

namespace CardThrow
{
    // ЗАГЛУШКА (Алексей, промпт A4): стопка фишек — динамический Rigidbody2D, разлетается от карты.
    public class PushableObstacle : Obstacle
    {
        public override ObstacleKind Kind => ObstacleKind.Pushable;

        public override void OnCardHit(Vector2 point, Vector2 cardVelocity)
        {
            // TODO A4: импульс в Rigidbody2D, звук фишек
        }
    }
}
