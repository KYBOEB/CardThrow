using UnityEngine;
using UnityEngine.InputSystem;

namespace CardThrow
{
    // ЗАГЛУШКА (владелец — Алексей, промпт A3).
    // Пока что: Space = бросок вдоль стола вправо на 60% силы. Позволяет тестировать игровой цикл без настоящего прицела.
    public class AimController : MonoBehaviour
    {
        [SerializeField] private CardFlight cardFlight;
        [SerializeField] private DealerHand dealerHand;
        [SerializeField] private Transform launchPoint;

        private bool inputEnabled;

        public float PreviewFraction { get; set; } = 1f;

        public void SetEnabled(bool value)
        {
            inputEnabled = value;
        }

        private void Update()
        {
            if (!inputEnabled || Keyboard.current == null) return;
            if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

            ThrowParams p = new ThrowParams
            {
                origin = launchPoint != null ? (Vector2)launchPoint.position : Vector2.zero,
                direction = Vector2.right,
                power01 = 0.6f,
                spinStep = 0,
                cardValue = dealerHand != null ? dealerHand.SelectedValue : 0
            };
            inputEnabled = false;
            GameEvents.RaiseCardThrown(p);
            if (cardFlight != null) cardFlight.Launch(p);
        }
    }
}
