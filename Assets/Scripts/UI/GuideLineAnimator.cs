using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 引导箭头：默认隐藏；显示时淡入，激活时整条沿箭头方向平移往复。
    /// 不改变箭头原本的位置/比例，只叠加位移和 CanvasGroup 透明度。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GuideLineAnimator : MonoBehaviour
    {
        public float bobAmplitude = 14f;
        public float bobFrequency = 1.3f;
        public float fadeSpeed = 12f;

        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 home;
        private Vector2 direction = Vector2.right;
        private float phase;
        private float offset;
        private float alpha;
        private bool visible;
        private bool animating;
        private bool refsReady;

        public void Configure(Vector2 dir)
        {
            EnsureRefs();
            if (dir.sqrMagnitude > 0.0001f)
                direction = dir.normalized;
        }

        public void SetState(bool show, bool animate)
        {
            EnsureRefs();
            visible = show;
            animating = show && animate;
            if (!show)
                phase = 0f;
        }

        private void Awake()
        {
            EnsureRefs();
        }

        private void EnsureRefs()
        {
            if (refsReady)
                return;
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            home = rect.anchoredPosition;
            alpha = 0f;
            group.alpha = 0f;
            refsReady = true;
        }

        private void Update()
        {
            if (!refsReady)
                EnsureRefs();

            float dt = Time.unscaledDeltaTime;
            if (animating)
                phase += dt * bobFrequency * Mathf.PI * 2f;

            float targetOffset = animating
                ? Mathf.Sin(phase) * bobAmplitude
                : 0f;
            offset = Mathf.Lerp(
                offset, targetOffset, 1f - Mathf.Exp(-dt * 18f));
            rect.anchoredPosition = home + direction * offset;

            float targetAlpha = visible ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * dt);
            group.alpha = alpha;
        }
    }
}
