using UnityEngine;

namespace CardThrow
{
    // Хрупкий предмет: бутылка, карточный домик, поднос. Владелец — Алексей (A2; эффекты добавим в A4).
    // Ломается от карты (OnCardHit) или от сильного удара фишки (цепная реакция).
    public class BreakableObstacle : Obstacle
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite brokenSprite;
        [SerializeField] private SoundId breakSound = SoundId.BottleBreak;
        [SerializeField, Tooltip("С какой скорости удар фишки разбивает предмет")]
        private float chainBreakSpeed = 2.5f;

        public override ObstacleKind Kind => ObstacleKind.Breakable;
        public bool IsBroken { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Удар карты. Событие CardHitObstacle поднимает сама карта (CardFlight).
        public override void OnCardHit(Vector2 point, Vector2 cardVelocity)
        {
            Break();
        }

        // Удар фишки (у фишек есть Rigidbody2D, поэтому Unity сообщает о столкновении).
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsBroken || collision.relativeVelocity.magnitude < chainBreakSpeed) return;

            Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            Break();
            // Цепная реакция: сообщаем всем — летящая карта засчитает провал.
            GameEvents.RaiseCardHitObstacle(point, ObstacleKind.Breakable);
        }

        private void Break()
        {
            if (IsBroken) return;
            IsBroken = true;

            if (spriteRenderer != null && brokenSprite != null) spriteRenderer.sprite = brokenSprite;
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            AudioManager.Play(breakSound);
        }
    }
}
