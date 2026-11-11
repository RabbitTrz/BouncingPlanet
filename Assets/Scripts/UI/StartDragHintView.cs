using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 开始页“拖动至此以进入下一步”的文字提示。
    /// 悬停任意贴纸时显示，离开或完成一次成功拖入后隐藏。
    /// 完成记录与关卡解锁/换装一样保存在 PlayerPrefs。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StartDragHintView : MonoBehaviour
    {
        private const string CompletedPref =
            "BouncingPlanet.StartDragHintCompleted";

        public CanvasGroup hintGroup;
        public DragSticker[] stickers;
        public float fadeSpeed = 12f;

        private Action<bool>[] handlers;
        private bool[] hovering;
        private bool completed;
        private bool suppressed;
        private float targetAlpha;

        private void Awake()
        {
            completed = PlayerPrefs.GetInt(CompletedPref, 0) != 0;
            if (hintGroup == null)
                hintGroup = GetComponent<CanvasGroup>();
            if (hintGroup == null)
                hintGroup = gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;
            hintGroup.alpha = 0f;
            targetAlpha = 0f;

            int count = stickers != null ? stickers.Length : 0;
            hovering = new bool[count];
            handlers = new Action<bool>[count];
            for (int i = 0; i < count; i++)
            {
                if (stickers[i] == null)
                    continue;
                int index = i;
                handlers[i] = value => OnHoverChanged(index, value);
                stickers[i].HoverChanged += handlers[i];
            }
        }

        private void OnDestroy()
        {
            if (stickers == null || handlers == null)
                return;
            for (int i = 0; i < stickers.Length && i < handlers.Length; i++)
            {
                if (stickers[i] != null && handlers[i] != null)
                    stickers[i].HoverChanged -= handlers[i];
            }
        }

        private void Update()
        {
            if (hintGroup == null)
                return;
            hintGroup.alpha = Mathf.MoveTowards(
                hintGroup.alpha, targetAlpha,
                Mathf.Max(0.01f, fadeSpeed) * Time.unscaledDeltaTime);
        }

        public void MarkCompleted()
        {
            completed = true;
            suppressed = false;
            PlayerPrefs.SetInt(CompletedPref, 1);
            PlayerPrefs.Save();
            SetVisible(false);
        }

        /// <summary>弹窗/切页时先收起，等下一次悬停重新触发。</summary>
        public void Suppress()
        {
            suppressed = true;
            SetVisible(false);
        }

        private void OnHoverChanged(int index, bool value)
        {
            if (hovering == null || index < 0 || index >= hovering.Length)
                return;
            hovering[index] = value;
            if (value)
                suppressed = false;
            Refresh();
        }

        private void Refresh()
        {
            if (completed || suppressed)
            {
                SetVisible(false);
                return;
            }
            bool any = false;
            for (int i = 0; i < hovering.Length; i++)
            {
                if (hovering[i])
                {
                    any = true;
                    break;
                }
            }
            SetVisible(any);
        }

        private void SetVisible(bool visible)
        {
            targetAlpha = visible && !completed && !suppressed ? 1f : 0f;
            if (hintGroup != null && !visible)
            {
                // 立即消失，避免离开悬停后还停留一帧。
                hintGroup.alpha = 0f;
            }
        }
    }
}
