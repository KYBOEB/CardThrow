using UnityEngine;

namespace CardThrow
{
    // Создаёт префаб текущей раздачи под LevelRoot. Переигровка = удалить и создать заново,
    // поэтому разбитые предметы и разлетевшиеся фишки восстанавливаются автоматически.
    public class LevelManager : MonoBehaviour
    {
        [Tooltip("По порядку: сначала раздачи обучения (IsTutorial), потом уровни. Например Hand_00, Hand_00b, Hand_00c, Hand_01…")]
        [SerializeField] private LevelConfig[] hands;
        [SerializeField] private Transform levelRoot;

        private LevelConfig current;

        public int HandCount => hands != null ? hands.Length : 0;
        public LevelConfig Current => current;

        // Сколько раздач в начале списка — обучение. Первая обычная раздача = этот индекс.
        public int TutorialCount
        {
            get
            {
                int n = 0;
                while (n < HandCount && hands[n] != null && hands[n].IsTutorial) n++;
                return n;
            }
        }

        public LevelConfig LoadHand(int index)
        {
            if (current != null)
            {
                current.gameObject.SetActive(false); // сразу убираем коллайдеры из физики
                Destroy(current.gameObject);         // а сам объект удалится в конце кадра
                current = null;
            }

            if (index < 0 || index >= HandCount || hands[index] == null)
            {
                Debug.LogError($"[CT] LevelManager: нет раздачи с индексом {index}");
                return null;
            }

            current = Instantiate(hands[index], levelRoot);
            current.name = hands[index].name;
            return current;
        }
    }
}
