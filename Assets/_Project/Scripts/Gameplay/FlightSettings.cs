using UnityEngine;

namespace CardThrow
{
    // Параметры модели полёта карты: общие для реального полёта и превью траектории.
    [CreateAssetMenu(menuName = "CardThrow/Flight Settings", fileName = "FlightSettings")]
    public class FlightSettings : ScriptableObject
    {
        [Header("Бросок")]
        public float maxSpeed = 21f;          // бросок на 100% ≈ 18 ед. (стол 19 ед. вдоль броска)
        [Range(0f, 1f)] public float minPower = 0.12f;
        public float maxDragDistance = 2.0f;

        [Header("Торможение")]
        public float linearDrag = 0.8f;
        public float friction = 2.5f;
        public float stopSpeed = 0.15f;

        [Header("Дуга")]
        public float curvePerStep = 0.35f;
        public float spinDecay = 0.3f;
        public int maxSpinStep = 3;

        [Header("Столкновения")]
        public float cardRadius = 0.25f;
        [Range(0f, 1f)] public float ricochetRestitution = 0.85f;
        [Range(0f, 1f)] public float pushSpeedLoss = 0.35f;
        public LayerMask obstacleMask;
        public LayerMask zoneMask;
        public LayerMask tableMask;

        [Header("Превью")]
        public int maxPreviewSteps = 400;
    }
}
