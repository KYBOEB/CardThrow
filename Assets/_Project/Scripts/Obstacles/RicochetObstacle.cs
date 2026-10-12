namespace CardThrow
{
    // Пепельница: неразрушима, карта отскакивает. Отскок считает FlightSimulator.
    public class RicochetObstacle : Obstacle
    {
        public override ObstacleKind Kind => ObstacleKind.Ricochet;
    }
}
