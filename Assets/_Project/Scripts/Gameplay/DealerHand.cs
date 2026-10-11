using UnityEngine;
using UnityEngine.InputSystem;

namespace CardThrow
{
    // ЗАГЛУШКА с базовой логикой (владелец — Алексей, промпт A3 добавит визуал карт и анимации).
    public class DealerHand : MonoBehaviour
    {
        private int[] values = new int[0];
        private bool interactable;

        public int SelectedIndex { get; private set; }
        public int SelectedValue => (values.Length > 0) ? values[SelectedIndex] : 0;

        // selectedIndex = -1 → средняя карта; при переигровке GameManager передаёт прежний выбор
        public void SetCards(int[] cardValues, int selectedIndex = -1)
        {
            values = cardValues != null ? (int[])cardValues.Clone() : new int[0];
            SelectedIndex = (selectedIndex >= 0 && selectedIndex < values.Length) ? selectedIndex : values.Length / 2;
            if (values.Length > 0) GameEvents.RaiseCardSelected(SelectedIndex, SelectedValue);
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
        }

        private void Update()
        {
            if (!interactable || values.Length == 0 || Keyboard.current == null) return;
            Keyboard kb = Keyboard.current;
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Select(SelectedIndex - 1);
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Select(SelectedIndex + 1);
        }

        private void Select(int index)
        {
            int clamped = Mathf.Clamp(index, 0, values.Length - 1);
            if (clamped == SelectedIndex) return;
            SelectedIndex = clamped;
            GameEvents.RaiseCardSelected(SelectedIndex, SelectedValue);
        }
    }
}
