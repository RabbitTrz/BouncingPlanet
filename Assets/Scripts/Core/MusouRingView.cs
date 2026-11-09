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
        private SpriteRenderer borderRenderer;
        private Transform borderTransform;
        private Sprite borderSprite;
        private SpriteRenderer borderFillRenderer;
        private Transform borderFillTransform;
        private Material borderFillMaterial;
        private Sprite borderFillSprite;
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

            borderSprite = Resources.Load<Sprite>(
                "Raw/overdirve/overdrive_border");
            if (borderSprite != null)
            {
                GameObject borderObject = new GameObject("MusouRingBorder");
                borderObject.transform.SetParent(transform, false);
                borderRenderer = borderObject.AddComponent<SpriteRenderer>();
                borderRenderer.sprite = borderSprite;
                borderRenderer.sortingOrder = 29;
                borderRenderer.enabled = false;
                borderTransform = borderObject.transform;
            }

            borderFillSprite = Resources.Load<Sprite>(
                "Raw/overdirve/overdrive_border2");
            Shader fillShader = Resources.Load<Shader>(
                "Shaders/MusouRingFill");
            if (borderFillSprite != null && fillShader != null)
            {
                borderFillMaterial = new Material(fillShader)
                {
                    name = "MusouRingFillMaterial"
                };
                borderFillMaterial.SetFloat(
                    "_StartAngle", config.musouRingFillStartAngle);
                borderFillMaterial.SetFloat(
                    "_SpanAngle", config.musouRingFillSpanAngle);
                borderFillMaterial.SetFloat("_Clockwise", 1f);
                borderFillMaterial.SetFloat("_CenterX", 0.5f);
                borderFillMaterial.SetFloat("_CenterY", 0.5f);
                borderFillMaterial.SetFloat(
                    "_Aspect",
                    borderFillSprite.rect.width /
                    Mathf.Max(1f, borderFillSprite.rect.height));
                borderFillMaterial.SetColor("_Color", Color.white);

                GameObject fillObject = new GameObject("MusouRingFill");
                fillObject.transform.SetParent(transform, false);
                borderFillRenderer = fillObject.AddComponent<SpriteRenderer>();
                borderFillRenderer.sprite = borderFillSprite;
                borderFillRenderer.material = borderFillMaterial;
                borderFillRenderer.sortingOrder = 30;
                borderFillRenderer.enabled = false;
                borderFillTransform = fillObject.transform;
            }

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
            bool visible = config.showMusouRing && gameVisible && !state.dead &&
                           skills.MusouEnabled;
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
            bool hasBorder = borderRenderer != null && borderTransform != null;
            bool hasFillRing = hasBorder && borderFillRenderer != null &&
                               borderFillTransform != null &&
                               borderFillSprite != null;
            if (hasBorder)
            {
                UpdateBorder(center, worldAngle, radius, active,
                    progress, hasFillRing);
            }

            // 有美术边框时，用边框作为无双环本体；
            // 有填充图时用美术填充层，不再使用程序化进度弧。
            baseRing.enabled = !hasBorder;
            if (!hasBorder)
            {
                baseRing.startWidth = width;
                baseRing.endWidth = width;
                Color baseColor = color;
                baseColor.a *= config.musouRingBaseAlpha;
                baseRing.startColor = baseColor;
                baseRing.endColor = baseColor;
                SetArc(baseRing, center, up, right, radius, 1f, true);
            }

            bool showProgressLine = !hasFillRing && progress > 0.001f;
            progressRing.enabled = showProgressLine;
            if (showProgressLine)
            {
                progressRing.startWidth = width;
                progressRing.endWidth = width;
                progressRing.startColor = color;
                progressRing.endColor = color;
                SetArc(progressRing, center, up, right, radius, progress, false);
            }
        }

        private void UpdateBorder(
            Vector2 center, float worldAngle, float radius, bool active,
            float progress, bool hasFillRing)
        {
            if (borderRenderer == null || borderTransform == null ||
                borderSprite == null)
            {
                return;
            }

            borderRenderer.enabled = true;
            float imageWidth = Mathf.Max(
                0.0001f, borderSprite.bounds.size.x);
            // 边框图里圆环大约占图宽的 95%，按这个比例对齐原无双环大小。
            const float ringWidthRatio = 0.95f;
            float scale = radius * 2f *
                          Mathf.Max(0.5f, config.musouRingBorderScale) /
                          (imageWidth * ringWidthRatio);
            if (active)
                scale *= 1f + Mathf.Sin(Time.unscaledTime * 9f) * 0.02f;

            borderTransform.position = new Vector3(center.x, center.y, 0f);
            // 贴图火焰在正上方，让它对齐玩家的“径向外”。
            borderTransform.rotation = Quaternion.Euler(
                0f, 0f, worldAngle * Mathf.Rad2Deg - 90f);
            borderTransform.localScale = new Vector3(scale, scale, 1f);
            borderRenderer.color = new Color(
                1f, 1f, 1f, active ? 1f : 0.8f);

            if (borderFillRenderer != null && borderFillTransform != null &&
                borderFillSprite != null)
            {
                borderFillRenderer.enabled = hasFillRing;
                if (hasFillRing)
                {
                    // 填充图和底图同尺寸、同 Transform，进度裁剪直接用
                    // 美术像素，保证与底图严丝合缝。
                    borderFillTransform.position = borderTransform.position;
                    borderFillTransform.rotation = borderTransform.rotation;
                    borderFillTransform.localScale = borderTransform.localScale;
                    if (borderFillMaterial != null)
                    {
                        borderFillMaterial.SetFloat(
                            "_Progress", Mathf.Clamp01(progress));
                        borderFillMaterial.SetFloat(
                            "_StartAngle", config.musouRingFillStartAngle);
                        borderFillMaterial.SetFloat(
                            "_SpanAngle", config.musouRingFillSpanAngle);
                    }
                    borderFillRenderer.color = new Color(
                        1f, 1f, 1f, active ? 1f : 0.9f);
                }
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
            if (borderRenderer != null)
                borderRenderer.enabled = visible;
            if (borderFillRenderer != null)
                borderFillRenderer.enabled = visible;
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
                Destroy(ringMaterial);
            if (borderFillMaterial != null)
                Destroy(borderFillMaterial);
        }
    }
}
