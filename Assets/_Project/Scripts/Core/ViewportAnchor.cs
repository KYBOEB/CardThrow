using UnityEngine;

namespace CardThrow
{
    // Держит декор у края экрана при любом соотношении сторон (16:9, 16:10, широкое окно Game).
    // Позиция = точка вьюпорта камеры (0..1) + сдвиг в мировых единицах. Пересчёт — только когда меняется
    // размер окна (не каждый кадр), поэтому тряска и зум камеры из FeedbackManager декор не двигают.
    public class ViewportAnchor : MonoBehaviour
    {
        [Tooltip("Точка экрана: (0,0) — левый нижний угол, (1,1) — правый верхний, (0,0.5) — середина левого края")]
        [SerializeField] private Vector2 viewport = new Vector2(0f, 1f);
        [Tooltip("Сдвиг от этой точки, ед. Плюс X — вправо, плюс Y — вверх")]
        [SerializeField] private Vector2 offset;

        private int lastWidth, lastHeight;

        private void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Vector3 p = cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 0f));
            transform.position = new Vector3(p.x + offset.x, p.y + offset.y, transform.position.z);
        }
    }
}
