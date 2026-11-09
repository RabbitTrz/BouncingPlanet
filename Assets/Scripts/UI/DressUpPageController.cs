using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 换装页：左侧分层预览，右侧 2×3 板块只显示每类“没穿在身上”的两件。
    /// 拖到预览区后替换对应类别；已穿的那件会回到板块。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DressUpPageController : MonoBehaviour
    {
        public GameObject page;
        public PlayerLayeredView preview;
        public DragSticker[] stickers;
        public Button backButton;

        private Wardrobe wardrobe;
        private bool subscribed;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            EnsureWardrobe();
            Refresh();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(Close);
            if (subscribed && wardrobe != null)
                wardrobe.Changed -= Refresh;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureWardrobe();
            Refresh();
        }

        public void Close()
        {
            if (UiTransition.Instance != null)
                UiTransition.Instance.Play(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
        }

        /// <summary>Esc 时优先中断正在拖动的服装贴纸。</summary>
        public bool TryCancelAnyDrag()
        {
            if (stickers == null)
                return false;
            for (int i = 0; i < stickers.Length; i++)
            {
                DragSticker sticker = stickers[i];
                if (sticker != null && sticker.IsDragging)
                {
                    sticker.CancelDrag();
                    return true;
                }
            }
            return false;
        }

        private void EnsureWardrobe()
        {
            if (wardrobe == null)
            {
                GameBootstrap bootstrap = GameBootstrap.Instance;
                wardrobe = bootstrap != null ? bootstrap.wardrobe : null;
            }
            if (wardrobe != null && !subscribed)
            {
                wardrobe.Changed += Refresh;
                subscribed = true;
            }
        }

        private void Refresh()
        {
            EnsureWardrobe();
            if (wardrobe == null || stickers == null || stickers.Length == 0)
                return;

            int[] columnBase = { 0, 1, 2 };
            ClothingCategory[] categories =
            {
                ClothingCategory.Hair,
                ClothingCategory.Cloth,
                ClothingCategory.Decoration
            };

            int slot = 0;
            for (int c = 0; c < categories.Length; c++)
            {
                ClothingCategory category = categories[c];
                int equipped = wardrobe.GetIndex(category);
                int alternative = 0;
                for (int option = 0; option < Wardrobe.OptionCount; option++)
                {
                    if (option == equipped)
                        continue;

                    int slotIndex = columnBase[c] + alternative * 3;
                    alternative++;
                    if (slotIndex >= stickers.Length)
                        continue;

                    DragSticker sticker = stickers[slotIndex];
                    if (sticker == null)
                        continue;

                    sticker.gameObject.SetActive(true);
                    sticker.equipClothing = true;
                    sticker.clothingCategory = category;
                    sticker.clothingIndex = option;
                    sticker.restoreAfterConsume = true;
                    sticker.SetIdleSprite(wardrobe.GetSticker(category, option));
                    slot++;
                }
            }

            for (int i = 0; i < stickers.Length; i++)
            {
                if (stickers[i] == null)
                    continue;
                if (i >= slot)
                    stickers[i].gameObject.SetActive(false);
            }
        }
    }
}
