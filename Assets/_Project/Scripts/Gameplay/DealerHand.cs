using UnityEngine;
using UnityEngine.InputSystem;

namespace CardThrow
{
    // Три карты в руке крупье и выбор между ними (A/D, ←/→).
    // Выбранная карта выдвигается к столу и чуть крупнее, остальные притемнены.
    // Пока игрок целится или карта летит, выбранная карта в руке скрыта — её изображает Card.
    public class DealerHand : MonoBehaviour
    {
        [Header("Карты в руке")]
        [SerializeField, Tooltip("Ровно 3 спрайта-карты (дочерние объекты)")]
        private SpriteRenderer[] cardRenderers;
        [SerializeField, Tooltip("card_2 … card_11 (индекс = номинал − 2)")]
        private Sprite[] faces;
        [SerializeField] private Sprite cardBack;

        [Header("Рука крупье")]
        [SerializeField] private SpriteRenderer handRenderer;
        [SerializeField] private Sprite handIdle;
        [SerializeField] private Sprite handPinch;
        [SerializeField] private Sprite handPull;

        [Header("Анимация")]
        [SerializeField, Tooltip("На сколько выбранная карта выдвигается к столу")] private float selectedRaise = 0.25f;
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField, Tooltip("Время переключения, с")] private float switchTime = 0.12f;
        [SerializeField] private Color dimmedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private int[] values = new int[0];
        private bool interactable;
        private bool selectedHidden;       // прицел или полёт: карта «в пальцах», а не в веере
        private Vector3[] basePositions;

        public int SelectedIndex { get; private set; }
        public int SelectedValue => (values.Length > 0) ? values[SelectedIndex] : 0;

        private void Awake() { CacheBasePositions(); }

        private void OnEnable()
        {
            GameEvents.AimChanged += OnAimChanged;
            GameEvents.CardThrown += OnCardThrown;
        }

        private void OnDisable()
        {
            GameEvents.AimChanged -= OnAimChanged;
            GameEvents.CardThrown -= OnCardThrown;
        }

        // ---------- Публичный API ----------

        // selectedIndex = -1 → средняя карта; при переигровке GameManager передаёт прежний выбор
        public void SetCards(int[] cardValues, int selectedIndex = -1)
        {
            values = cardValues != null ? (int[])cardValues.Clone() : new int[0];
            SelectedIndex = (selectedIndex >= 0 && selectedIndex < values.Length) ? selectedIndex : values.Length / 2;
            selectedHidden = false;
            RefreshFaces();
            SnapToTargets();
            SetHandSprite(0f);
            if (values.Length > 0) GameEvents.RaiseCardSelected(SelectedIndex, SelectedValue);
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
        }

        // ---------- Ввод и анимация ----------

        private void Update()
        {
            if (interactable && values.Length > 0 && !selectedHidden && Keyboard.current != null)
            {
                Keyboard kb = Keyboard.current;
                // Веер вертикальный: индекс 0 внизу (y = −0.85), 2 — вверху. Крупье смотрит вправо, поэтому его «лево» — вверх:
                // A/← = выше по вееру (индекс +1), D/→ = ниже (индекс −1). Так A/D совпадают с тем, что игрок видит на экране.
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Select(SelectedIndex + 1);
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Select(SelectedIndex - 1);
            }
            Animate(Time.unscaledDeltaTime);
        }

        private void Select(int index)
        {
            int clamped = Mathf.Clamp(index, 0, values.Length - 1);
            if (clamped == SelectedIndex) return;
            SelectedIndex = clamped;
            RefreshFaces();
            GameEvents.RaiseCardSelected(SelectedIndex, SelectedValue);
        }

        // Плавно двигаем карты к целевым позициям/масштабам: за switchTime проходят весь путь.
        private void Animate(float dt)
        {
            if (cardRenderers == null || basePositions == null) return;
            float posStep = selectedRaise / Mathf.Max(0.01f, switchTime) * dt;
            float scaleStep = (selectedScale - 1f) / Mathf.Max(0.01f, switchTime) * dt;

            for (int i = 0; i < cardRenderers.Length; i++)
            {
                if (cardRenderers[i] == null) continue;
                Transform t = cardRenderers[i].transform;
                GetTarget(i, out Vector3 pos, out float scale);
                t.localPosition = Vector3.MoveTowards(t.localPosition, pos, posStep);
                float s = Mathf.MoveTowards(t.localScale.x, scale, scaleStep);
                t.localScale = new Vector3(s, s, 1f);
            }
        }

        private void SnapToTargets()
        {
            if (cardRenderers == null) return;
            CacheBasePositions();
            for (int i = 0; i < cardRenderers.Length; i++)
            {
                if (cardRenderers[i] == null) continue;
                GetTarget(i, out Vector3 pos, out float scale);
                cardRenderers[i].transform.localPosition = pos;
                cardRenderers[i].transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void GetTarget(int i, out Vector3 pos, out float scale)
        {
            bool selected = i == SelectedIndex;
            // «К столу» = вправо в координатах крупье (крупье слева, бросает вправо).
            pos = basePositions[i] + (selected ? Vector3.right * selectedRaise : Vector3.zero);
            scale = selected ? selectedScale : 1f;
        }

        private void RefreshFaces()
        {
            if (cardRenderers == null) return;
            for (int i = 0; i < cardRenderers.Length; i++)
            {
                SpriteRenderer r = cardRenderers[i];
                if (r == null) continue;

                bool hasCard = i < values.Length;
                r.enabled = hasCard && !(selectedHidden && i == SelectedIndex);
                if (!hasCard) continue;

                r.sprite = FaceFor(values[i]);
                r.color = i == SelectedIndex ? Color.white : dimmedColor;
                r.sortingOrder = i == SelectedIndex ? 10 : i; // выбранная поверх соседних
            }
        }

        private Sprite FaceFor(int value)
        {
            int i = value - 2;
            if (faces != null && i >= 0 && i < faces.Length && faces[i] != null) return faces[i];
            return cardBack;
        }

        private void CacheBasePositions()
        {
            if (basePositions != null || cardRenderers == null) return;
            basePositions = new Vector3[cardRenderers.Length];
            for (int i = 0; i < cardRenderers.Length; i++)
                if (cardRenderers[i] != null) basePositions[i] = cardRenderers[i].transform.localPosition;
        }

        // ---------- События ----------

        private void OnAimChanged(float power01)
        {
            SetHandSprite(power01);
            // Начали тянуть — карта «в пальцах», сняли прицел — вернулась в веер (если ещё не брошена).
            if (power01 > 0f) selectedHidden = true;
            else if (interactable) selectedHidden = false;
            RefreshFaces();
        }

        private void OnCardThrown(ThrowParams p)
        {
            selectedHidden = true; // брошенная карта ушла из руки до новой раздачи (SetCards)
            RefreshFaces();
        }

        private void SetHandSprite(float power01)
        {
            if (handRenderer == null) return;
            Sprite s = power01 > 0.6f ? handPull : power01 > 0f ? handPinch : handIdle;
            if (s != null) handRenderer.sprite = s;
        }
    }
}
