using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 玩家周围的无双环：未激活时显示积分进度，激活时显示剩余时间。
    /// 纯表现层，不修改技能/分数状态。
    /// </summary>
    [DefaultExecutionOrder(1600)]
    [DisallowMultipleComponent]
    public sealed class MusouRingView : MonoBehaviour
    {
        private const int Segments = 64;

        public GameBootstrap bootstrap;

        private GameConfig config;
        private PlayerController player;
        private PlayerSkills skills;
        private GameFlow flow;
        private LineRenderer baseRing;
        private LineRenderer progressRing;
        private Material ringMaterial;
        private bool initialized;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.gameConfig == null ||
                bootstrap.player == null || bootstrap.skills == null)
            {
                Debug.LogWarning("[MusouRingView] Missing bootstrap/config/player/skills, skipped.");
                return;
            }

            config = bootstrap.gameConfig;
            player = bootstrap.player;
            skills = bootstrap.skills;
            flow = bootstrap.gameFlow;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                ringMaterial = new Material(shader)
                {
                    name = "MusouRingMaterial"
                };
            }

            baseRing = CreateLine("MusouRingBase", true, Segments);
            progressRing = CreateLine("MusouRingProgress", false, Segments + 1);
            initialized = true;
            SetVisible(false);
        }

        private void Start()
        {
            if (!initialized)
                Initialize(GameBootstrap.Instance);
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            if (flow == null && bootstrap != null)
                flow = bootstrap.gameFlow;
            if (skills == null && bootstrap != null)
                skills = bootstrap.skills;

            if (config == null || player == null || skills == null)
            {
                SetVisible(false);
                return;
            }

            PlayerState state = player.State;
            bool gameVisible = flow == null ||
                flow.State == GameFlow.FlowState.Playing ||
                flow.State == GameFlow.FlowState.Paused;
            bool visible = config.showMusouRing && gameVisible && !state.dead;
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            bool active = skills.IsMusouActive;
            float progress;
            if (active)
            {
                progress = Mathf.Clamp01(
                    skills.MusouRemaining / Mathf.Max(0.0001f, config.musouDuration));
            }
            else
            {
                progress = config.musouScoreThreshold > 0
                    ? Mathf.Clamp01(skills.MusouScore / (float)config.musouScoreThreshold)
                    : 0f;
            }

            Vector2 center = state.WorldPosition;
            float worldAngle = state.WorldAngle;
            Vector2 up = new Vector2(Mathf.Cos(worldAngle), Mathf.Sin(worldAngle));
            Vector2 right = new Vector2(up.y, -up.x);
            float radius = Mathf.Max(0.001f, config.musouRingRadius);
            float pulse = active ? 1f + Mathf.Sin(Time.unscaledTime * 9f) * 0.12f : 1f;
            float width = Mathf.Max(0.0001f, config.musouRingWidth) * pulse;

            Color color = active ? config.musouRingActiveColor : config.musouRingChargeColor;

            baseRing.enabled = true;
            baseRing.startWidth = width;
            baseRing.endWidth = width;
            Color baseColor = color;
            baseColor.a *= config.musouRingBaseAlpha;
            baseRing.startColor = baseColor;
            baseRing.endColor = baseColor;
            SetArc(baseRing, center, up, right, radius, 1f, true);

            bool showProgress = progress > 0.001f;
            progressRing.enabled = showProgress;
            if (showProgress)
            {
                progressRing.startWidth = width;
                progressRing.endWidth = width;
                progressRing.startColor = color;
                progressRing.endColor = color;
                SetArc(progressRing, center, up, right, radius, progress, false);
            }
        }

        private LineRenderer CreateLine(string lineName, bool loop, int positionCount)
        {
            GameObject go = new GameObject(lineName);
            go.transform.SetParent(transform, false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = positionCount;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.sortingOrder = 30;
            if (ringMaterial != null)
                line.material = ringMaterial;
            line.enabled = false;
            return line;
        }

        private static void SetArc(LineRenderer line, Vector2 center, Vector2 up,
            Vector2 right, float radius, float fraction, bool fullCircle)
        {
            int count = fullCircle
                ? Segments
                : Mathf.Max(2, Mathf.CeilToInt(Segments * Mathf.Clamp01(fraction)) + 1);
            if (line.positionCount != count)
                line.positionCount = count;

            float span = fullCircle ? 1f : Mathf.Clamp01(fraction);
            for (int i = 0; i < count; i++)
            {
                float t = fullCircle
                    ? i / (float)Segments
                    : (count <= 1 ? 0f : i / (float)(count - 1)) * span;
                float angle = t * Mathf.PI * 2f;
                Vector2 offset = up * Mathf.Cos(angle) + right * Mathf.Sin(angle);
                line.SetPosition(i, new Vector3(
                    center.x + offset.x * radius,
                    center.y + offset.y * radius,
                    0f));
            }
        }

        private void SetVisible(bool visible)
        {
            if (baseRing != null)
                baseRing.enabled = visible;
            if (progressRing != null)
                progressRing.enabled = visible;
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
                Destroy(ringMaterial);
        }
    }
}
