using UnityEngine;

namespace CardThrow
{
    // ЗАГЛУШКА (Алексей, промпты A2/A4): бутылка, карточный домик, поднос.
    public class BreakableObstacle : Obstacle
    {
        public override ObstacleKind Kind => ObstacleKind.Breakable;
        public bool IsBroken { get; private set; }

        public override void OnCardHit(Vector2 point, Vector2 cardVelocity)
        {
            if (IsBroken) return;
            IsBroken = true;
            // TODO A2/A4: смена спрайта на разбитый, частицы, звук
        }
    }
}
