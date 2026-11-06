using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>弹窗按钮的悬停放大、悬浮/点击音效和点击粒子。</summary>
    [DisallowMultipleComponent]
    public sealed class UiButtonEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform visual;
        public UiBurstEffect clickBurst;
        public float hoverScale = 1.08f;

        private Vector3 baseScale;
        private bool hovering;
        private Button button;

        private void Awake()
        {
            if (visual == null)
                visual = transform as RectTransform;
            if (visual != null)
                baseScale = visual.localScale;
            button = GetComponent<Button>();
            if (button != null)
                button.transition = Selectable.Transition.None;
        }

        private void OnDisable()
        {
            hovering = false;
            if (visual != null)
                visual.localScale = baseScale;
        }

        private void Update()
        {
            if (visual == null)
                return;
            Vector3 target = baseScale * (hovering ? hoverScale : 1f);
            visual.localScale = Vector3.Lerp(
                visual.localScale, target,
                1f - Mathf.Exp(-Time.unscaledDeltaTime * 16f));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            GameBootstrap.Instance?.audioService?.PlayStickerHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            GameBootstrap.Instance?.audioService?.PlayStickerClick();
            clickBurst?.Burst();
        }
    }
}
