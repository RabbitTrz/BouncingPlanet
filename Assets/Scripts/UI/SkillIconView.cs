using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 单个技能图标：可用/禁用、冷却/持续时间遮罩。
    /// 不显示文字，按键提示由美术后续补图。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillIconView : MonoBehaviour
    {
        public Image iconImage;
        public Image cooldownImage;
        public UiBurstEffect readyBurst;
        public bool hideWhenDisabled = true;
        public Color readyColor = Color.white;
        public Color activeColor = new Color(1f, 0.86f, 0.38f, 1f);
        public Color disabledColor = new Color(1f, 1f, 1f, 0.22f);

        /// <summary>
        /// enabled = 本关是否开放技能；
        /// remaining/total = 冷却或剩余时间；
        /// active = 无双是否正在持续。
        /// </summary>
        public void PlayReadyBurst()
        {
            if (readyBurst != null)
                readyBurst.Burst();
        }

        public void Set(bool enabled, float remaining, float total, bool active)
        {
            if (hideWhenDisabled && gameObject.activeSelf != enabled)
                gameObject.SetActive(enabled);

            if (iconImage != null)
            {
                iconImage.color = enabled
                    ? (active ? activeColor : readyColor)
                    : disabledColor;
            }

            if (!enabled || cooldownImage == null)
            {
                if (cooldownImage != null && cooldownImage.gameObject.activeSelf)
                    cooldownImage.gameObject.SetActive(false);
                return;
            }

            bool show = total > 0.0001f && remaining > 0.01f;
            if (cooldownImage.gameObject.activeSelf != show)
                cooldownImage.gameObject.SetActive(show);
            if (show)
                cooldownImage.fillAmount = Mathf.Clamp01(remaining / total);
        }
    }
}
