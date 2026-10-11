using UnityEngine;

namespace CardThrow
{
    public enum GameState { Title, HandIntro, Aiming, Flying, Resolving, HandResult, GameOver, Victory }

    // Физический итог полёта (решает CardFlight)
    public enum FlightResult { LandedInPartnerZone, HitBreakable, EnteredOpponentZone, OffTable, StoppedOnTable }

    // Игровой итог броска (решает GameManager)
    public enum ThrowOutcome { Success, WrongValue, HitBreakable, OpponentCaught, OffTable, StoppedOnTable, TimeOut }

    public enum ObstacleKind { Ricochet, Breakable, Pushable }
    public enum ZoneType { Partner, Opponent }

    public struct ThrowParams
    {
        public Vector2 origin;
        public Vector2 direction;   // нормализован
        public float power01;       // 0..1
        public int spinStep;        // -3..3, + = дуга влево
        public int cardValue;       // номинал брошенной карты
    }

    public struct FlightReport
    {
        public FlightResult result;
        public Vector2 finalPosition;
        public int ricochets;
        public int spinStep;
        public int cardValue;
        public float zoneAccuracy01; // 1 = центр зоны напарника, 0 = край (только для LandedInPartnerZone)
        public int bonusPickups;
    }
}
