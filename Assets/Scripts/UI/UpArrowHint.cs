using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 关卡开始时在玩家附近显示向上箭头，上下往复数秒后隐藏。
    /// 教程弹窗会先暂停游戏，因此等真正进入 Playing 后再开始计时。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpArrowHint : MonoBehaviour
    {
        public float duration = 3f;
        public float bobDistance = 30f;
        public float bobSpeed = 1.6f;
        public float fadeDuration = 0.2f;

        private RectTransform rect;
        private CanvasGroup group;
        private GameFlow flow;
        private Vector2 home;
        private bool pending;
        private bool showing;
        private float timer;

        private void Awake()
        {
            rect = transform as RectTransform;
            home = rect != null ? rect.anchoredPosition : Vector2.zero;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        private void OnEnable()
        {
            Attach();
        }

        private void OnDisable()
        {
            Detach();
            pending = false;
            showing = false;
            ResetVisual();
        }

        private void Update()
        {
            Attach();
            if (flow == null)
                return;

            if (flow.State != GameFlow.FlowState.Playing)
            {
                if (showing)
                {
                    showing = false;
                    ResetVisual();
                }
                return;
            }

            if (pending)
                Begin();

            if (!showing)
                return;

            timer += Time.unscaledDeltaTime;
            if (timer >= Mathf.Max(0.01f, duration))
            {
                showing = false;
                ResetVisual();
                return;
            }

            float fade = Mathf.Max(0.01f, fadeDuration);
            float alpha = Mathf.Clamp01(Mathf.Min(
                timer / fade, (duration - timer) / fade));
            group.alpha = alpha;

            if (rect != null)
            {
                float offset = Mathf.Sin(
                    timer * Mathf.Max(0.01f, bobSpeed) * Mathf.PI * 2f) *
                    bobDistance;
                rect.anchoredPosition = home + Vector2.up * offset;
            }
        }

        private void Attach()
        {
            if (flow != null || group == null)
                return;
            GameBootstrap bootstrap = GameBootstrap.Instance;
            flow = bootstrap != null ? bootstrap.gameFlow : null;
            if (flow == null)
                return;
            flow.RunStarted += OnRunStarted;
        }

        private void Detach()
        {
            if (flow == null)
                return;
            flow.RunStarted -= OnRunStarted;
            flow = null;
        }

        private void OnRunStarted(LevelConfig level)
        {
            pending = true;
            showing = false;
            group.alpha = 0f;
            if (rect != null)
                rect.anchoredPosition = home;
        }

        private void Begin()
        {
            pending = false;
            showing = true;
            timer = 0f;
            group.alpha = 0f;
            if (rect != null)
                rect.anchoredPosition = home;
        }

        private void ResetVisual()
        {
            if (group != null)
                group.alpha = 0f;
            if (rect != null)
                rect.anchoredPosition = home;
        }
    }
}
