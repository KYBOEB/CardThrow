using System.Collections;
using UnityEngine;

namespace CardThrow
{
    // Общие UI-анимации (корутины). Все — на Time.unscaledDeltaTime: slow-mo (Time.timeScale < 1)
    // не замедляет интерфейс. Запускает их сам экран: StartCoroutine(UiAnim.Fade(...)).
    public static class UiAnim
    {
        // Плавно меняет прозрачность группы. Кнопки кликабельны только у видимой группы.
        public static IEnumerator Fade(CanvasGroup group, float to, float duration)
        {
            if (group == null) yield break;
            SetInteractive(group, to > 0.5f); // сразу: исчезающая кнопка не должна ловить клик

            float from = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        // Мгновенно показать/скрыть (без кадра анимации)
        public static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            SetInteractive(group, visible);
        }


        // Короткий «тычок»: 1 → 1+amount → 1 (синус-полуволна)
        public static IEnumerator Punch(Transform target, float amount, float duration)
        {
            if (target == null) yield break;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI);
                target.localScale = Vector3.one * (1f + amount * k);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private static void SetInteractive(CanvasGroup group, bool value)
        {
            group.interactable = value;
            group.blocksRaycasts = value;
        }
    }
}
