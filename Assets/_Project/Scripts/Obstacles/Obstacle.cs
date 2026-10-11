using UnityEngine;

namespace CardThrow
{
    // Базовый класс препятствия. Коллайдер (не триггер) на слое Obstacle.
    public abstract class Obstacle : MonoBehaviour
    {
        public abstract ObstacleKind Kind { get; }

        // Вызывает CardFlight в момент столкновения
        public virtual void OnCardHit(Vector2 point, Vector2 cardVelocity) { }
    }
}
