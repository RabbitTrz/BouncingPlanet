using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 普通撞碎就绪提示：屏幕两侧向下滑动的白色速度线。
    /// 仅在玩家上升且径向速度达到普通撞碎阈值时显示；纯表现，不改判定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SmashSpeedLinesView : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private Texture2D lineTexture;
        private float intensity;
        private float speedFactor;

        private void Awake()
        {
            bootstrap = GameBootstrap.Instance;
            lineTexture = CreateLineTexture();
        }

        private void Update()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;

            float target = IsSmashReady(out float factor) ? 1f : 0f;
            speedFactor = factor;
            intensity = Mathf.MoveTowards(
                intensity, target, Time.unscaledDeltaTime * (target > 0f ? 6f : 4f));
        }

        private bool IsSmashReady(out float factor)
        {
            factor = 0f;
            if (bootstrap == null || bootstrap.gameConfig == null ||
                bootstrap.player == null)
            {
                return false;
            }

            GameConfig config = bootstrap.gameConfig;
            if (!config.normalSmashEnabled || !config.smashSpeedLinesEnabled)
                return false;

            GameFlow flow = bootstrap.gameFlow;
            if (flow != null && flow.State != GameFlow.FlowState.Playing)
                return false;

            PlayerState state = bootstrap.player.State;
            if (state.dead)
                return false;
            if (bootstrap.skills != null && bootstrap.skills.IsMusouActive)
                return false;

            float threshold = GetSmashReadyThreshold();
            if (threshold <= 0.01f || state.radialVelocity < threshold)
                return false;

            factor = Mathf.InverseLerp(
                threshold, threshold + 2f, state.radialVelocity);
            return true;
        }

        /// <summary>
        /// 速度线表示“当前速度至少能撞碎一种非免疫星球”，
        /// 所以取所有可撞碎星球里的最低阈值。
        /// </summary>
        private float GetSmashReadyThreshold()
        {
            GameConfig config = bootstrap != null ? bootstrap.gameConfig : null;
            if (config == null)
                return 0f;

            PlanetTypeDef[] types = bootstrap.planetTypes;
            if (types == null || types.Length == 0)
                return config.normalSmashSpeedThreshold;

            float min = float.PositiveInfinity;
            for (int i = 0; i < types.Length; i++)
            {
                PlanetTypeDef def = types[i];
                if (def == null || def.normalSmashImmune)
                    continue;

                float threshold = def.smashSpeedThreshold > 0.0001f
                    ? def.smashSpeedThreshold
                    : config.normalSmashSpeedThreshold;
                if (threshold > 0f)
                    min = Mathf.Min(min, threshold);
            }

            return float.IsInfinity(min) ? 0f : min;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (bootstrap == null || bootstrap.gameConfig == null ||
                bootstrap.player == null || intensity <= 0.001f)
            {
                return;
            }

            GameConfig config = bootstrap.gameConfig;
            if (!config.smashSpeedLinesEnabled)
                return;

            GameFlow flow = bootstrap.gameFlow;
            if (flow != null && flow.State != GameFlow.FlowState.Playing)
                return;

            DrawLines(config);
        }

        private void DrawLines(GameConfig config)
        {
            float width = Screen.width;
            float height = Screen.height;
            float sideBand = Mathf.Clamp(
                config.smashSpeedLineSideBand, 0.02f, 0.35f) * width;
            int count = Mathf.Clamp(config.smashSpeedLineCount, 0, 60);
            float baseAlpha = intensity * Mathf.Clamp01(config.smashSpeedLineAlpha);
            float speed = Mathf.Max(0f, config.smashSpeedLineSpeed) * height;
            float minLength = Mathf.Max(1f,
                config.smashSpeedLineMinLength * height / 1080f);
            float maxLength = Mathf.Max(minLength,
                config.smashSpeedLineMaxLength * height / 1080f);
            float baseWidth = Mathf.Max(0.5f,
                config.smashSpeedLineWidth * width / 1920f);

            Color old = GUI.color;
            for (int i = 0; i < count; i++)
            {
                bool left = (i & 1) == 0;
                float x = left
                    ? Hash01(i, 1) * sideBand
                    : width - Hash01(i, 1) * sideBand;
                float length = Mathf.Lerp(
                    minLength, maxLength, Hash01(i, 2));
                float lineWidth = baseWidth *
                    Mathf.Lerp(0.6f, 1.4f, Hash01(i, 3));
                float lineSpeed = speed *
                    Mathf.Lerp(0.6f, 1.5f, Hash01(i, 4));
                float phase = Hash01(i, 5);
                float total = height + length * 2f;
                float y = Mathf.Repeat(
                    Time.unscaledTime * lineSpeed + phase * total, total) - length;
                float alpha = baseAlpha *
                    Mathf.Lerp(0.35f, 1f, Hash01(i, 6));

                GUI.color = new Color(1f, 1f, 1f, alpha * 0.18f);
                GUI.DrawTexture(new Rect(
                    x - lineWidth * 1.5f, y,
                    lineWidth * 4f, length),
                    lineTexture, ScaleMode.StretchToFill, true);

                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(
                    x, y, lineWidth, length),
                    lineTexture, ScaleMode.StretchToFill, true);
            }
            GUI.color = old;
        }

        private static Texture2D CreateLineTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(1, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[size];
            for (int i = 0; i < size; i++)
            {
                float t = i / (float)(size - 1);
                float alpha = Mathf.Sin(t * Mathf.PI);
                pixels[i] = new Color32(255, 255, 255,
                    (byte)(Mathf.Clamp01(alpha) * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static float Hash01(int index, int salt)
        {
            unchecked
            {
                uint value = (uint)(index * 73856093 ^ salt * 19349663);
                value ^= value >> 13;
                value *= 1274126177u;
                value ^= value >> 16;
                return (value & 0x7fffffffu) / (float)0x7fffffff;
            }
        }

        private void OnDestroy()
        {
            if (lineTexture != null)
                Destroy(lineTexture);
        }
    }
}
