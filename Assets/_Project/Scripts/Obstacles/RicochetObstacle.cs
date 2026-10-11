namespace CardThrow
{
    // Гранёный стакан: неразрушим, карта отскакивает. Отскок считает CardFlight. (Алексей, промпт A2)
    public class RicochetObstacle : Obstacle
    {
        public override ObstacleKind Kind => ObstacleKind.Ricochet;
    }
}
