using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>UI 图集循环播放，用于选关页黑洞等静态动画。</summary>
    [DisallowMultipleComponent]
    public sealed class UiSpriteAnimator : MonoBehaviour
    {
        public Image target;
        public Sprite[] frames;
        public float fps = 10f;

        private int index;
        private float timer;

        private void Awake()
        {
            if (target == null)
                target = GetComponent<Image>();
        }

        private void OnEnable()
        {
            index = 0;
            timer = 0f;
            if (GameBootstrap.Instance != null &&
                GameBootstrap.Instance.gameConfig != null)
            {
                float configured =
                    GameBootstrap.Instance.gameConfig.blackHoleAnimationFps;
                if (configured > 0f)
                    fps = configured;
            }
            Apply();
        }

        private void Update()
        {
            if (target == null || frames == null || frames.Length == 0)
                return;

            float rate = Mathf.Max(0.1f, fps);
            float frameDuration = 1f / rate;
            timer += Time.unscaledDeltaTime;
            if (timer < frameDuration)
                return;

            int steps = Mathf.FloorToInt(timer / frameDuration);
            timer -= steps * frameDuration;
            index = (index + steps) % frames.Length;
            Apply();
        }

        private void Apply()
        {
            if (target != null && frames != null && frames.Length > 0)
                target.sprite = frames[index % frames.Length];
        }
    }
}
