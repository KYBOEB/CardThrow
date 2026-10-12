using UnityEngine;

namespace CardThrow
{
    // Данные раздачи; висит на корне её префаба.
    // Обучение = несколько первых раздач в списке LevelManager с isTutorial.
    public class LevelConfig : MonoBehaviour
    {
        [Header("Уровень")]
        [Tooltip("Уровень 0: обучение — без таймера, подозрения и очков")]
        [SerializeField] private bool isTutorial;
        [Tooltip("Только для обучения: Throw — выбрать карту и кинуть; Spin — + дуга; Ricochet — рикошет от пепельницы")]
        [SerializeField] private TutorialStage tutorialStage = TutorialStage.Throw;
        [SerializeField] private string handTitle = "Уровень";
        [Tooltip("В обучении оставьте пустым: подсказки ведёт TutorialController")]
        [SerializeField, TextArea(1, 3)] private string hint = "";

        [Header("Карты")]
        [SerializeField] private Seat partnerSeat;
        [SerializeField, Range(2, 20)] private int partnerStartScore = 14;
        [Tooltip("Ровно 3 карты, ровно одна даёт напарнику 21")]
        [SerializeField] private int[] cardValues = { 5, 7, 9 };

        [Header("Сложность")]
        [SerializeField, Min(3f)] private float timeLimit = 20f;
        [SerializeField, Range(0.2f, 1f)] private float previewFraction = 1f;

        public bool IsTutorial => isTutorial;
        public TutorialStage TutorialStage => tutorialStage;
        public string HandTitle => handTitle;
        public string Hint => hint;
        public Seat PartnerSeat => partnerSeat;
        public int PartnerStartScore => partnerStartScore;
        public int[] CardValues => cardValues;
        public float TimeLimit => timeLimit;
        public float PreviewFraction => previewFraction;

        // Индекс карты, которая даёт ровно 21 (или -1, если такой нет)
        public int CorrectCardIndex => System.Array.FindIndex(cardValues, v => partnerStartScore + v == 21);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cardValues == null || cardValues.Length != 3)
            {
                Debug.LogWarning($"[CT] {name}: должно быть ровно 3 карты", this);
                return;
            }
            int correct = 0;
            foreach (int v in cardValues)
                if (partnerStartScore + v == 21) correct++;
            if (correct != 1)
                Debug.LogWarning($"[CT] {name}: правильных карт {correct}, нужна ровно 1", this);

            // DealerHand на старте выбирает среднюю карту: если она правильная, шаг «выбери карту» пропустится сам.
            // В первой части обучения (Throw) этот шаг нужен, в следующих — наоборот, удобно его пропустить.
            if (isTutorial && tutorialStage == TutorialStage.Throw && CorrectCardIndex == cardValues.Length / 2)
                Debug.LogWarning($"[CT] {name}: в части «Бросок» правильная карта не должна быть средней (иначе шаг «выбери карту» пропустится)", this);

            if (partnerSeat == null)
                Debug.LogWarning($"[CT] {name}: не заполнен PartnerSeat", this);
        }
#endif
    }
}
