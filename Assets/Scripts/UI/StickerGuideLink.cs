using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>把一张贴纸的悬停/按下状态映射到一条引导箭头。</summary>
    [DisallowMultipleComponent]
    public sealed class StickerGuideLink : MonoBehaviour
    {
        public DragSticker sticker;
        public GuideLineAnimator line;

        private bool hovering;
        private bool pressed;
        private bool subscribed;

        public void Bind(DragSticker source, GuideLineAnimator target)
        {
            Unsubscribe();
            sticker = source;
            line = target;
            hovering = false;
            pressed = false;
            Subscribe();
            Apply();
        }

        public void Hide()
        {
            hovering = false;
            pressed = false;
            Apply();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || sticker == null)
                return;
            subscribed = true;
            sticker.HoverChanged += OnHoverChanged;
            sticker.PressChanged += OnPressChanged;
        }

        private void Unsubscribe()
        {
            if (!subscribed || sticker == null)
                return;
            subscribed = false;
            sticker.HoverChanged -= OnHoverChanged;
            sticker.PressChanged -= OnPressChanged;
        }

        private void OnHoverChanged(bool value)
        {
            hovering = value;
            Apply();
        }

        private void OnPressChanged(bool value)
        {
            pressed = value;
            Apply();
        }

        private void Apply()
        {
            if (line != null)
                line.SetState(hovering || pressed, pressed);
        }
    }
}
