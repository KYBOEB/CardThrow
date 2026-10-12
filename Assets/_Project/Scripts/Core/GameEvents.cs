using System;
using UnityEngine;

namespace CardThrow
{
    // Шина событий: системы общаются друг с другом только через неё.
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;            // GameManager
        public static event Action<int, int, string> HandStarted;      // GameManager: levelNumber (0 = обучение), totalLevels (3), title
        public static event Action<string> HintChanged;                // GameManager / TutorialController: подсказка над столом ("" = скрыть)
        public static event Action<float, float> TimerChanged;         // GameManager: remaining, total
        public static event Action<int, int> SuspicionChanged;         // GameManager: current, max
        public static event Action<int> ScoreChanged;                  // GameManager: total score
        public static event Action<int, int> CardSelected;             // DealerHand: index, value
        public static event Action<int> SpinChanged;                   // AimController: spinStep
        public static event Action<float> AimChanged;                  // AimController: power01 (0 = прицел снят)
        public static event Action<ThrowParams> CardThrown;            // AimController
        public static event Action<Vector2, ObstacleKind> CardHitObstacle; // CardFlight: point, kind
        public static event Action<FlightReport> CardFlightFinished;   // CardFlight
        public static event Action<ThrowOutcome, int> ThrowResolved;   // GameManager: outcome, scoreGained

        public static void RaiseStateChanged(GameState s) => StateChanged?.Invoke(s);
        public static void RaiseHandStarted(int i, int total, string title) => HandStarted?.Invoke(i, total, title);
        public static void RaiseHintChanged(string text) => HintChanged?.Invoke(text);
        public static void RaiseTimerChanged(float rem, float total) => TimerChanged?.Invoke(rem, total);
        public static void RaiseSuspicionChanged(int cur, int max) => SuspicionChanged?.Invoke(cur, max);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
        public static void RaiseCardSelected(int index, int value) => CardSelected?.Invoke(index, value);
        public static void RaiseSpinChanged(int step) => SpinChanged?.Invoke(step);
        public static void RaiseAimChanged(float p) => AimChanged?.Invoke(p);
        public static void RaiseCardThrown(ThrowParams p) => CardThrown?.Invoke(p);
        public static void RaiseCardHitObstacle(Vector2 pt, ObstacleKind k) => CardHitObstacle?.Invoke(pt, k);
        public static void RaiseCardFlightFinished(FlightReport r) => CardFlightFinished?.Invoke(r);
        public static void RaiseThrowResolved(ThrowOutcome o, int gained) => ThrowResolved?.Invoke(o, gained);

        // Сброс подписок при входе в Play Mode (если Domain Reload отключён)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            StateChanged = null; HandStarted = null; HintChanged = null; TimerChanged = null; SuspicionChanged = null;
            ScoreChanged = null; CardSelected = null; SpinChanged = null; AimChanged = null;
            CardThrown = null; CardHitObstacle = null; CardFlightFinished = null; ThrowResolved = null;
        }
    }
}
