using System.Collections;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>弹窗面板的 Q 弹出现/缩小关闭动画。</summary>
    [DisallowMultipleComponent]
    public sealed class PopupScaleIn : MonoBehaviour
    {
        public GameObject closeRoot;
        public float openDuration = 0.30f;
        public float closeDuration = 0.16f;
        [Range(0.1f, 1f)] public float startScale = 0.72f;

        private Vector3 baseScale;
        private Coroutine routine;
        private bool closing;

        private void Awake()
        {
            baseScale = transform.localScale;
            if (closeRoot == null)
                closeRoot = gameObject;
        }

        private void OnEnable()
        {
            if (closing)
                return;
            StopRoutine();
            transform.localScale = baseScale * startScale;
            routine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (closing || closeRoot == null || !closeRoot.activeInHierarchy)
                return;
            StopRoutine();
            closing = true;
            routine = StartCoroutine(AnimateClose());
        }

        private void OnDisable()
        {
            StopRoutine();
            closing = false;
            transform.localScale = baseScale;
        }

        private void StopRoutine()
        {
            if (routine == null)
                return;
            StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator AnimateOpen()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, openDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = EaseOutBack(t);
                transform.localScale = baseScale *
                    Mathf.Lerp(startScale, 1f, ease);
                yield return null;
            }
            transform.localScale = baseScale;
            routine = null;
        }

        private IEnumerator AnimateClose()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, closeDuration);
            float from = baseScale.x > 0.0001f
                ? transform.localScale.x / baseScale.x
                : 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - (1f - t) * (1f - t);
                transform.localScale = baseScale *
                    Mathf.Lerp(from, startScale * 0.8f, ease);
                yield return null;
            }

            closing = false;
            routine = null;
            transform.localScale = baseScale;
            if (closeRoot != null)
                closeRoot.SetActive(false);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
