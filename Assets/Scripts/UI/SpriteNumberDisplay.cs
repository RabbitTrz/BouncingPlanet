using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 用 0-9 的 Sprite 组成整数。digitImages 从左到右排列；
    /// 隐藏前面的空槽，配合 HorizontalLayoutGroup 自动左右对齐。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteNumberDisplay : MonoBehaviour
    {
        public Image[] digitImages;
        public Sprite[] digitSprites;

        private int lastValue = int.MinValue;

        public void SetValue(int value)
        {
            value = Mathf.Max(0, value);
            if (value == lastValue)
                return;
            lastValue = value;

            if (digitImages == null || digitImages.Length == 0)
                return;

            string text = value.ToString();
            int slots = digitImages.Length;
            int shown = Mathf.Min(text.Length, slots);
            int textStart = text.Length - shown;
            int firstActive = slots - shown;

            for (int i = 0; i < slots; i++)
            {
                Image image = digitImages[i];
                if (image == null)
                    continue;

                bool active = i >= firstActive;
                image.gameObject.SetActive(active);
                if (!active)
                    continue;

                char c = text[textStart + (i - firstActive)];
                int digit = c - '0';
                if (digitSprites != null && digit >= 0 &&
                    digit < digitSprites.Length && digitSprites[digit] != null)
                {
                    image.sprite = digitSprites[digit];
                }
            }
        }
    }
}
