using UnityEngine;

namespace CardThrow
{
    // ЗАГЛУШКА (владелец — Данила). Полную реализацию генерирует промпт D1.
    // Публичный API ниже менять нельзя — на него опираются UI и другие системы.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameSettings settings;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private DealerHand dealerHand;
        [SerializeField] private AimController aimController;
        [SerializeField] private CardFlight cardFlight;

        public GameState State { get; private set; } = GameState.Title;
        public LevelConfig CurrentLevel { get; private set; }
        public bool IsTutorial => CurrentLevel != null && CurrentLevel.IsTutorial;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartRun() { /* TODO D1 */ }
        public void RestartRun() { /* TODO D1 */ }
        public void SkipTutorial() { /* TODO D1 */ }
        public void DebugLoadHand(int index) { /* TODO D1 */ }
    }
}
