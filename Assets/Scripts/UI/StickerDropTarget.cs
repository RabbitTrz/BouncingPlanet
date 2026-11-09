using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>中央黑洞等拖动判定区域。</summary>
    [DisallowMultipleComponent]
    public sealed class StickerDropTarget : MonoBehaviour
    {
        public RectTransform area;
        public LevelSelectController levelSelect;
        public int levelIndex = -1;
        [Tooltip("关闭后只作为换装等自定义拖放的判定区，不再触发页面动作。")]
        public bool acceptActions = true;
        [Tooltip("未解锁关卡不接收贴纸。")]
        public bool locked;

        private void Awake()
        {
            if (area == null)
                area = transform as RectTransform;
        }

        public bool Contains(Vector2 screenPosition, Camera eventCamera)
        {
            if (locked || area == null)
                return false;
            return RectTransformUtility.RectangleContainsScreenPoint(
                area, screenPosition, eventCamera);
        }

        public void Accept(StickerAction action)
        {
            if (locked || !acceptActions)
                return;
            if (levelSelect != null && levelIndex >= 0)
            {
                levelSelect.StartLevel(levelIndex);
                return;
            }
            StartPageController.Instance?.HandleStickerDrop(action);
        }
    }
}
