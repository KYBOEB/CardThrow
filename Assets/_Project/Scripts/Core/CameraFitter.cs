using UnityEngine;

namespace CardThrow
{
    // Подбирает orthographicSize так, чтобы стол + поля под HUD целиком помещались
    // при любом соотношении сторон (16:9, 16:10 на Mac, Free Aspect в окне Game).
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Vector2 contentSize = new Vector2(21f, 12f); // ширина и высота в единицах мира
        [SerializeField] private float minOrthoSize = 6f;

        private Camera cam;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Fit();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Fit();
        }

        private void Fit()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float sizeByHeight = contentSize.y * 0.5f;
            float sizeByWidth = contentSize.x * 0.5f / aspect;
            cam.orthographicSize = Mathf.Max(minOrthoSize, sizeByHeight, sizeByWidth);
        }
    }
}
