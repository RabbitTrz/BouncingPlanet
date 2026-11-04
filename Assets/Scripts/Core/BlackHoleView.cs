using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 带纸边黑洞的 7 帧循环动画。只做表现，不参与出生点/死亡判定。
    /// 精灵可能使用非中心 pivot，这里按 bounds 自动把画面中心对齐到原点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlackHoleView : MonoBehaviour
    {
        private const string SpritePath = "Image/Hole/hole_paper";

        private GameConfig config;
        private SpriteRenderer body;
        private Sprite[] frames;
        private int frameIndex;
        private float frameTimer;
        private float diameterOverride = -1f;
        private bool initialized;

        /// <summary>成功加载并创建返回 true；失败时调用方可回退到程序化黑洞。</summary>
        public bool Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            if (config == null)
                return false;

            Sprite[] loaded = Resources.LoadAll<Sprite>(SpritePath);
            if (loaded == null || loaded.Length == 0)
            {
                Debug.LogWarning(
                    "[BlackHoleView] 未找到 hole_paper 精灵，保留程序化黑洞。");
                return false;
            }

            frames = loaded;
            Array.Sort(frames, (a, b) =>
                string.CompareOrdinal(a.name, b.name));

            GameObject visualObject = new GameObject("BlackHoleVisual");
            visualObject.transform.SetParent(transform, false);
            body = visualObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = -10;
            body.sprite = frames[0];

            ApplyDiameter();
            initialized = true;
            return true;
        }

        /// <summary>无尽压力表现使用：覆盖默认视觉直径。</summary>
        public void SetVisualDiameter(float diameter)
        {
            diameterOverride = Mathf.Max(0.01f, diameter);
            ApplyDiameter();
        }

        /// <summary>无尽压力表现使用：把黑洞放到世界坐标位置（相对 worldRoot）。</summary>
        public void SetLocalWorldPosition(Vector2 position)
        {
            transform.localPosition = new Vector3(position.x, position.y, 0f);
        }

        public void ClearPressureVisual()
        {
            diameterOverride = -1f;
            transform.localPosition = Vector3.zero;
            ApplyDiameter();
        }

        private void Update()
        {
            if (!initialized || config == null || frames == null ||
                frames.Length == 0)
            {
                return;
            }

            float fps = Mathf.Max(0.1f, config.blackHoleAnimationFps);
            float frameDuration = 1f / fps;
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                int steps = Mathf.FloorToInt(frameTimer / frameDuration);
                frameTimer -= steps * frameDuration;
                frameIndex = (frameIndex + steps) % frames.Length;
                body.sprite = frames[frameIndex];
            }

            ApplyDiameter();
        }

        private void ApplyDiameter()
        {
            if (body == null || body.sprite == null || config == null)
                return;

            Bounds bounds = body.sprite.bounds;
            float sourceSize = Mathf.Max(0.0001f,
                Mathf.Max(bounds.size.x, bounds.size.y));
            float diameter = diameterOverride >= 0f
                ? diameterOverride
                : config.blackHoleVisualDiameter;
            float scale = Mathf.Max(0.01f, diameter) / sourceSize;
            body.transform.localScale = new Vector3(scale, scale, 1f);
            body.transform.localPosition = new Vector3(
                -bounds.center.x * scale,
                -bounds.center.y * scale,
                0f);
        }
    }
}
