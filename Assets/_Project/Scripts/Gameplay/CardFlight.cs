using UnityEngine;

namespace CardThrow
{
    // ЗАГЛУШКА (владелец — Алексей, промпт A2).
    // Пока что сразу сообщает «карта остановилась на столе» — этого хватает, чтобы Данила отладил GameManager.
    public class CardFlight : MonoBehaviour
    {
        [SerializeField] private FlightSettings settings;

        public bool IsFlying { get; private set; }

        public void Launch(ThrowParams p)
        {
            IsFlying = false;
            GameEvents.RaiseCardFlightFinished(new FlightReport
            {
                result = FlightResult.StoppedOnTable,
                finalPosition = p.origin,
                spinStep = p.spinStep,
                cardValue = p.cardValue
            });
        }

        public void ResetCard()
        {
            IsFlying = false;
        }
    }
}
