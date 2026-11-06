using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>全屏黑色渐变遮罩，用于开始/选关/关卡之间切换。</summary>
    [DisallowMultipleComponent]
    public sealed class UiTransition : MonoBehaviour
    {
        public static UiTransition Instance { get; private set; }

        public Image overlay;
        public float fadeOutDuration = 0.18f;
        public float fadeInDuration = 0.24f;

        private bool running;

        private void Awake()
        {
            Instance = this;
            if (overlay != null)
            {
                SetAlpha(0f);
                overlay.raycastTarget = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Play(Action action)
        {
            if (overlay == null)
            {
                action?.Invoke();
                return;
            }
            if (running)
            {
                action?.Invoke();
                return;
            }
            StartCoroutine(Run(action));
        }

        private IEnumerator Run(Action action)
        {
            running = true;
            overlay.raycastTarget = true;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeOutDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            SetAlpha(1f);

            action?.Invoke();
            yield return null;

            elapsed = 0f;
            duration = Mathf.Max(0.01f, fadeInDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            SetAlpha(0f);
            overlay.raycastTarget = false;
            running = false;
        }

        private void SetAlpha(float alpha)
        {
            if (overlay == null)
                return;
            Color color = overlay.color;
            color.a = alpha;
            overlay.color = color;
            overlay.enabled = alpha > 0.001f;
        }
    }
}
