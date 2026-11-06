using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 静态贴纸的三帧待机摆动，和可拖动贴纸使用同一频率。
    /// 标题这种不参与拖拽的元素可以直接挂这个组件。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class StickerSway : MonoBehaviour
    {
        public float angle = 3f;
        public float frequency = 1.43f;
        [Min(1)] public int steps = 3;
        [Tooltip("小于 0 时随机相位。")]
        public int stepOffset = -1;

        private RectTransform rect;
        private Quaternion baseRotation;
        private int offset;

        private void Awake()
        {
            rect = (RectTransform)transform;
            baseRotation = rect.localRotation;
            offset = stepOffset >= 0
                ? stepOffset
                : Random.Range(0, Mathf.Max(1, steps));
        }

        private void OnDisable()
        {
            if (rect != null)
                rect.localRotation = baseRotation;
        }

        private void Update()
        {
            if (rect == null)
                return;

            int count = Mathf.Max(1, steps);
            if (Mathf.Abs(angle) < 0.001f || frequency <= 0.001f)
            {
                rect.localRotation = baseRotation;
                return;
            }

            int step = Mathf.FloorToInt(
                Time.unscaledTime * frequency * count + offset) % count;
            if (step < 0)
                step += count;

            float pose = count == 3
                ? step - 1
                : Mathf.Sin(step / (float)count * Mathf.PI * 2f);
            rect.localRotation = baseRotation *
                Quaternion.Euler(0f, 0f, pose * angle);
        }
    }
}
