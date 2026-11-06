using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 贴纸拖动：三帧待机摆动、悬浮抖动、点击粒子/音效，
    /// 拖入判定区后旋转缩小并被吸入，再触发对应页面动作。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DragSticker : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public StickerDropTarget dropTarget;
        public StickerDropTarget[] dropTargets;
        public StickerAction action;
        public UiBurstEffect clickBurst;
        public Sprite idleSprite;
        public Sprite dragSprite;
        public Sprite consumeSprite;

        [Header("Idle Sway")]
        public float idleSwayAngle = 5f;
        public float idleSwayFrequency = 1.43f;
        [Min(1)] public int idleSwaySteps = 3;

        [Header("Hover / Drag")]
        public float hoverScale = 1.06f;
        public float dragScale = 1.1f;
        public float hoverShake = 3f;
        public float shakeSpeed = 28f;
        public float returnDuration = 0.18f;

        [Header("Consume")]
        public float consumeDuration = 0.48f;
        public float consumeSpin = 720f;

        private RectTransform rect;
        private RectTransform parentRect;
        private CanvasGroup group;
        private Image image;
        private Vector2 homePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Vector2 dragOffset;
        private int swayStepOffset;
        private bool dragging;
        private bool hovering;
        private bool returning;
        private bool consumed;
        private bool inputLocked;
        private float hoverTime;
        private Coroutine returnRoutine;
        private Coroutine consumeRoutine;

        private void Awake()
        {
            rect = (RectTransform)transform;
            parentRect = rect.parent as RectTransform;
            homePosition = rect.anchoredPosition;
            baseScale = rect.localScale;
            baseRotation = rect.localRotation;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            image = GetComponent<Image>();
            if (image != null && idleSprite != null)
                image.sprite = idleSprite;
            swayStepOffset = Random.Range(0, Mathf.Max(1, idleSwaySteps));
        }

        private void OnDisable()
        {
            StopReturn();
            StopConsume();
            consumed = false;
            dragging = false;
            hovering = false;
            returning = false;
            inputLocked = false;
            SetStateSprite(idleSprite);
            if (rect != null)
            {
                rect.anchoredPosition = homePosition;
                rect.localScale = baseScale;
                rect.localRotation = baseRotation;
            }
            if (group != null)
                group.alpha = 1f;
            if (image != null)
                image.raycastTarget = true;
        }

        private void Update()
        {
            if (consumed || inputLocked)
                return;

            if (dragging || returning)
            {
                rect.localRotation = baseRotation;
                return;
            }

            ApplyIdleSway();

            float dt = Time.unscaledDeltaTime;
            if (hovering)
            {
                hoverTime += dt;
                float sx = Mathf.Sin(hoverTime * shakeSpeed) * hoverShake;
                float sy = Mathf.Cos(hoverTime * shakeSpeed * 0.7f) *
                           hoverShake * 0.35f;
                rect.anchoredPosition = homePosition + new Vector2(sx, sy);
                float scale = Mathf.Lerp(rect.localScale.x, hoverScale,
                    1f - Mathf.Exp(-dt * 12f));
                rect.localScale = Vector3.one * scale;
            }
            else
            {
                rect.anchoredPosition = Vector2.Lerp(
                    rect.anchoredPosition, homePosition,
                    1f - Mathf.Exp(-dt * 18f));
                rect.localScale = Vector3.Lerp(
                    rect.localScale, baseScale,
                    1f - Mathf.Exp(-dt * 18f));
            }
        }

        private void ApplyIdleSway()
        {
            int steps = Mathf.Max(1, idleSwaySteps);
            if (Mathf.Abs(idleSwayAngle) < 0.001f ||
                idleSwayFrequency <= 0.001f)
            {
                rect.localRotation = baseRotation;
                return;
            }

            int step = Mathf.FloorToInt(
                Time.unscaledTime * idleSwayFrequency * steps +
                swayStepOffset) % steps;
            if (step < 0)
                step += steps;

            float pose = steps == 3
                ? step - 1
                : Mathf.Sin(step / (float)steps * Mathf.PI * 2f);
            rect.localRotation = baseRotation *
                Quaternion.Euler(0f, 0f, pose * idleSwayAngle);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (consumed || inputLocked)
                return;
            PlayClick();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!dragging && !returning && !consumed && !inputLocked)
            {
                hovering = true;
                PlayHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (consumed || inputLocked)
                return;
            StopReturn();
            hovering = false;
            dragging = true;
            SetStateSprite(dragSprite);
            if (parentRect == null)
                parentRect = rect.parent as RectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, eventData.pressEventCamera,
                    out Vector2 local))
            {
                dragOffset = rect.anchoredPosition - local;
            }
            else
            {
                dragOffset = Vector2.zero;
            }

            rect.SetAsLastSibling();
            rect.localRotation = baseRotation;
            rect.localScale = baseScale * dragScale;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || inputLocked)
                return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, eventData.pressEventCamera,
                    out Vector2 local))
            {
                rect.anchoredPosition = local + dragOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            dragging = false;
            StickerDropTarget acceptedTarget = FindDropTarget(
                eventData.position, eventData.pressEventCamera);
            if (acceptedTarget != null)
            {
                PlayConsume();
                StartConsume(acceptedTarget);
            }
            else
            {
                StartReturn();
            }
        }

        private StickerDropTarget FindDropTarget(
            Vector2 screenPosition, Camera eventCamera)
        {
            if (dropTargets != null)
            {
                for (int i = 0; i < dropTargets.Length; i++)
                {
                    StickerDropTarget candidate = dropTargets[i];
                    if (candidate != null &&
                        candidate.Contains(screenPosition, eventCamera))
                    {
                        return candidate;
                    }
                }
            }

            if (dropTarget != null &&
                dropTarget.Contains(screenPosition, eventCamera))
            {
                return dropTarget;
            }
            return null;
        }

        public void Restore()
        {
            if (rect == null)
                return;
            StopReturn();
            StopConsume();
            consumed = false;
            dragging = false;
            hovering = false;
            returning = false;
            inputLocked = false;
            SetStateSprite(idleSprite);
            rect.anchoredPosition = homePosition;
            rect.localScale = baseScale;
            rect.localRotation = baseRotation;
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            if (image != null)
                image.raycastTarget = true;
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (group != null)
            {
                group.blocksRaycasts = !locked;
                group.interactable = !locked;
            }
            if (!locked && image != null)
                image.raycastTarget = true;
        }

        private void SetStateSprite(Sprite sprite)
        {
            if (image != null && sprite != null)
                image.sprite = sprite;
        }

        private void StartConsume(StickerDropTarget target)
        {
            StopReturn();
            StopConsume();
            SetStateSprite(consumeSprite);
            consumeRoutine = StartCoroutine(ConsumeRoutine(target));
        }

        private IEnumerator ConsumeRoutine(StickerDropTarget target)
        {
            consumed = true;
            hovering = false;
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            if (image != null)
                image.raycastTarget = false;

            Vector2 from = rect.anchoredPosition;
            Vector3 fromScale = rect.localScale;
            Quaternion fromRotation = rect.localRotation;
            Vector2 targetPosition = GetTargetLocalPosition(target);
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, consumeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                rect.anchoredPosition = Vector2.Lerp(from, targetPosition, eased);
                float scale = Mathf.Lerp(fromScale.x, 0.03f, eased);
                rect.localScale = new Vector3(scale, scale, 1f);
                rect.localRotation = fromRotation *
                    Quaternion.Euler(0f, 0f, consumeSpin * eased);
                if (group != null)
                    group.alpha = 1f - eased;
                yield return null;
            }

            if (group != null)
                group.alpha = 0f;
            consumeRoutine = null;
            target?.Accept(action);
        }

        private Vector2 GetTargetLocalPosition(StickerDropTarget target)
        {
            if (target == null || target.area == null ||
                parentRect == null)
            {
                return Vector2.zero;
            }
            Vector3 world = target.area.TransformPoint(
                target.area.rect.center);
            Vector3 local = parentRect.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        private void StartReturn()
        {
            StopReturn();
            SetStateSprite(idleSprite);
            returnRoutine = StartCoroutine(ReturnHome());
        }

        private void StopReturn()
        {
            if (returnRoutine == null)
                return;
            StopCoroutine(returnRoutine);
            returnRoutine = null;
            returning = false;
        }

        private void StopConsume()
        {
            if (consumeRoutine == null)
                return;
            StopCoroutine(consumeRoutine);
            consumeRoutine = null;
        }

        private IEnumerator ReturnHome()
        {
            returning = true;
            Vector2 from = rect.anchoredPosition;
            Vector3 fromScale = rect.localScale;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, returnDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                rect.anchoredPosition = Vector2.Lerp(from, homePosition, eased);
                rect.localScale = Vector3.Lerp(fromScale, baseScale, eased);
                yield return null;
            }

            rect.anchoredPosition = homePosition;
            rect.localScale = baseScale;
            rect.localRotation = baseRotation;
            returning = false;
            returnRoutine = null;
        }

        private void PlayHover()
        {
            GameBootstrap.Instance?.audioService?.PlayStickerHover();
        }

        private void PlayClick()
        {
            GameBootstrap.Instance?.audioService?.PlayStickerClick();
            clickBurst?.Burst();
        }

        private void PlayConsume()
        {
            GameBootstrap.Instance?.audioService?.PlayStickerConsume();
        }
    }
}
