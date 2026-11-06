using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 选关元素沿星环从屏幕外滑入目标位置。
    /// 玩家贴纸会锁住拖动，直到自己的入场动画结束。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiOrbitArrival : MonoBehaviour
    {
        public Vector2 orbitCenter = new Vector2(-650f, 25f);
        public float startAngleDegrees = 130f;
        public float delay = 0f;
        public float duration = 0.62f;
        [Range(0.1f, 1f)] public float startScale = 0.65f;

        private RectTransform rect;
        private CanvasGroup group;
        private DragSticker sticker;
        private Vector2 targetPosition;
        private Vector3 targetScale;
        private Coroutine routine;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (rect != null)
                return;
            rect = (RectTransform)transform;
            targetPosition = rect.anchoredPosition;
            targetScale = rect.localScale;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            sticker = GetComponent<DragSticker>();
        }

        public void Play()
        {
            EnsureInitialized();
            Stop();
            routine = StartCoroutine(Run());
        }

        private void OnDisable()
        {
            Stop();
            if (group != null)
                group.alpha = 1f;
            sticker?.SetInputLocked(false);
        }

        private void Stop()
        {
            if (routine == null)
                return;
            StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator Run()
        {
            Vector2 center = orbitCenter;
            Vector2 delta = targetPosition - center;
            float radius = Mathf.Max(0.001f, delta.magnitude);
            float endAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float startAngle = startAngleDegrees;
            float startRadians = startAngle * Mathf.Deg2Rad;
            rect.anchoredPosition = center +
                new Vector2(Mathf.Cos(startRadians), Mathf.Sin(startRadians)) *
                radius;
            rect.localScale = targetScale * startScale;
            if (group != null)
                group.alpha = 0f;
            sticker?.SetInputLocked(true);

            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            float length = Mathf.Max(0.05f, duration);
            while (elapsed < length)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / length);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float angle = Mathf.Lerp(startAngle, endAngle, eased) *
                              Mathf.Deg2Rad;
                rect.anchoredPosition = center +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                rect.localScale = targetScale *
                    Mathf.Lerp(startScale, 1f, eased);
                if (group != null)
                    group.alpha = eased;
                yield return null;
            }

            rect.anchoredPosition = targetPosition;
            rect.localScale = targetScale;
            if (group != null)
                group.alpha = 1f;
            sticker?.SetInputLocked(false);
            routine = null;
        }
    }
}
