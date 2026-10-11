using UnityEngine;

namespace CardThrow
{
    // Зона карт игрока на столе. Триггер-коллайдер на слое Zone.
    [RequireComponent(typeof(Collider2D))]
    public class Zone : MonoBehaviour
    {
        [SerializeField] private ZoneType type = ZoneType.Opponent;
        [SerializeField] private Seat seat;

        private Collider2D cachedCollider;

        public ZoneType Type => type;
        public Seat Seat => seat;
        public Vector2 Center => Col.bounds.center;
        public float Radius => Mathf.Min(Col.bounds.extents.x, Col.bounds.extents.y);

        private Collider2D Col => cachedCollider != null ? cachedCollider : (cachedCollider = GetComponent<Collider2D>());

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
            int layer = LayerMask.NameToLayer("Zone");
            if (layer >= 0) gameObject.layer = layer;
        }
    }
}
