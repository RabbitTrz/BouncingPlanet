using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 钩锁表现：玩家周围白色虚线范围 + 到目标的绳子。
    /// 不参与仿真；范围半径/目标来自 PlayerSkills。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HookView : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private GameConfig config;
        private PlayerController player;
        private PlayerSkills skills;
        private GameFlow flow;
        private Transform rangeRoot;
        private LineRenderer rangeLine;
        private LineRenderer ropeLine;
        private LineRenderer aimLine;
        private LineRenderer aimArrow;
        private LineRenderer targetRing;
        private Material dashMaterial;
        private Material ropeMaterial;
        private Texture2D dashTexture;
        private bool initialized;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.gameConfig == null || bootstrap.player == null)
                return;

            config = bootstrap.gameConfig;
            player = bootstrap.player;
            skills = bootstrap.skills;
            flow = bootstrap.gameFlow;

            rangeRoot = new GameObject("HookRangeView").transform;
            rangeRoot.SetParent(transform, false);
            rangeLine = BuildRangeLine(rangeRoot);

            GameObject ropeObject = new GameObject("HookRopeView");
            ropeObject.transform.SetParent(transform, false);
            ropeLine = ropeObject.AddComponent<LineRenderer>();
            ropeLine.useWorldSpace = true;
            ropeLine.positionCount = 2;
            ropeLine.startWidth = 0.008f;
            ropeLine.endWidth = 0.008f;
            ropeLine.startColor = new Color(1f, 0.95f, 0.8f, 0.75f);
            ropeLine.endColor = ropeLine.startColor;
            ropeLine.sortingOrder = 27;
            ropeMaterial = new Material(Shader.Find("Sprites/Default"));
            ropeLine.material = ropeMaterial;
            ropeLine.enabled = false;

            aimLine = BuildAimLine("HookAimLine", 2, 28, 0.005f);
            aimArrow = BuildAimLine("HookAimArrow", 3, 29, 0.008f);
            BuildTargetRing();

            initialized = true;
        }

        private LineRenderer BuildAimLine(string objectName, int positionCount,
            int sortingOrder, float width)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = positionCount;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.sortingOrder = sortingOrder;
            line.startWidth = width;
            line.endWidth = width;
            line.material = ropeMaterial;
            line.enabled = false;
            return line;
        }

        private void BuildTargetRing()
        {
            GameObject rootObject = new GameObject("HookTargetRing");
            rootObject.transform.SetParent(transform, false);

            targetRing = rootObject.AddComponent<LineRenderer>();
            targetRing.useWorldSpace = true;
            targetRing.loop = true;
            targetRing.positionCount = 32;
            targetRing.numCornerVertices = 2;
            targetRing.numCapVertices = 2;
            targetRing.alignment = LineAlignment.View;
            targetRing.sortingOrder = 27;
            targetRing.startWidth = 0.006f;
            targetRing.endWidth = 0.006f;
            targetRing.material = ropeMaterial;
            for (int i = 0; i < targetRing.positionCount; i++)
            {
                float angle = i / (float)targetRing.positionCount * Mathf.PI * 2f;
                targetRing.SetPosition(i, new Vector3(
                    Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }
            targetRing.enabled = false;
        }

        private LineRenderer BuildRangeLine(Transform parent)
        {
            dashTexture = new Texture2D(16, 1, TextureFormat.RGBA32, false);
            dashTexture.name = "HookDashTexture";
            dashTexture.wrapMode = TextureWrapMode.Repeat;
            dashTexture.filterMode = FilterMode.Point;
            for (int i = 0; i < 16; i++)
                dashTexture.SetPixel(i, 0, i < 8 ? Color.white : Color.clear);
            dashTexture.Apply(false, false);

            Shader shader = Shader.Find("Sprites/Default");
            dashMaterial = shader != null ? new Material(shader) : null;
            if (dashMaterial != null)
            {
                dashMaterial.name = "HookDashMaterial";
                dashMaterial.mainTexture = dashTexture;
            }

            GameObject go = new GameObject("HookRangeCircle");
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.textureScale = new Vector2(22f, 1f);
            line.startWidth = 0.014f;
            line.endWidth = 0.014f;
            line.startColor = new Color(1f, 1f, 1f, 0.35f);
            line.endColor = line.startColor;
            line.sortingOrder = 22;
            if (dashMaterial != null)
                line.material = dashMaterial;

            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }
            return line;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            if (flow == null)
                flow = bootstrap.gameFlow;
            if (skills == null)
                skills = bootstrap.skills;

            PlayerState state = player.State;
            bool gameActive = flow == null ||
                flow.State == GameFlow.FlowState.Playing ||
                flow.State == GameFlow.FlowState.Paused;
            bool musou = skills != null && skills.IsMusouActive;
            bool hookEnabled = skills != null && skills.HookEnabled;
            bool showRange = gameActive && !state.dead && !musou && hookEnabled;

            rangeRoot.gameObject.SetActive(showRange);
            if (showRange)
            {
                Vector2 position = state.WorldPosition;
                rangeRoot.position = new Vector3(position.x, position.y, 0f);
                rangeRoot.localScale = Vector3.one * Mathf.Max(0.05f, config.hookRange);
                rangeRoot.rotation = Quaternion.identity;
            }

            bool showRope = gameActive && !state.dead && !musou && hookEnabled &&
                            skills.IsHookActive &&
                            skills.HookTarget != null && skills.HookTarget.alive;
            ropeLine.enabled = showRope;
            if (showRope)
            {
                Vector2 playerPosition = state.WorldPosition;
                Vector2 targetPosition = skills.HookTarget.WorldPosition;
                ropeLine.SetPosition(0, new Vector3(playerPosition.x, playerPosition.y, 0f));
                ropeLine.SetPosition(1, new Vector3(targetPosition.x, targetPosition.y, 0f));
            }

            UpdateTargetIndicator(gameActive, state);
        }

        /// <summary>
        /// 瞄准提示：网页 demo 版鼠标选目标。钩锁就绪时，
        /// 在鼠标所指/空点回退的目标上画脉动圆环 + 箭头；
        /// 钩住后保留目标圆环，由绳子继续指示。
        /// </summary>
        private void UpdateTargetIndicator(bool gameActive, PlayerState state)
        {
            PlanetRuntime focus = null;
            bool showAim = false;

            if (gameActive && !state.dead && skills != null &&
                skills.HookEnabled && !skills.IsMusouActive)
            {
                if (skills.IsHookActive)
                {
                    focus = skills.HookTarget;
                }
                else if (skills.HookCooldownRemaining <= 0.001f)
                {
                    focus = skills.FindHookCandidateAtScreen(new Vector2(
                        Input.mousePosition.x, Input.mousePosition.y));
                    showAim = focus != null;
                }
            }

            bool showRing = focus != null && focus.alive;
            if (targetRing != null)
                targetRing.enabled = showRing;
            if (aimLine != null)
                aimLine.enabled = showAim;
            if (aimArrow != null)
                aimArrow.enabled = showAim;
            if (!showRing)
                return;

            Vector2 targetPosition = focus.WorldPosition;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.08f;
            float ringRadius = Mathf.Max(0.015f, focus.worldRadius * 1.7f) * pulse;
            for (int i = 0; i < targetRing.positionCount; i++)
            {
                float angle = i / (float)targetRing.positionCount * Mathf.PI * 2f;
                targetRing.SetPosition(i, new Vector3(
                    targetPosition.x + Mathf.Cos(angle) * ringRadius,
                    targetPosition.y + Mathf.Sin(angle) * ringRadius,
                    0f));
            }
            float ringAlpha = 0.5f + 0.2f * pulse;
            Color ringColor = new Color(1f, 0.9f, 0.62f, ringAlpha);
            targetRing.startColor = ringColor;
            targetRing.endColor = ringColor;

            if (!showAim)
                return;

            Vector2 playerPosition = state.WorldPosition;
            Vector2 delta = targetPosition - playerPosition;
            float distance = delta.magnitude;
            if (distance < 0.0001f)
            {
                aimLine.enabled = false;
                aimArrow.enabled = false;
                return;
            }

            Vector2 direction = delta / distance;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float arrowLength = Mathf.Max(0.03f, focus.worldRadius);
            float arrowWidth = arrowLength * 0.55f;
            Vector2 tip = targetPosition - direction * (focus.worldRadius + 0.02f);
            Vector2 back = tip - direction * arrowLength;
            Vector2 left = back + perpendicular * arrowWidth;
            Vector2 right = back - perpendicular * arrowWidth;

            aimLine.SetPosition(0, new Vector3(playerPosition.x, playerPosition.y, 0f));
            aimLine.SetPosition(1, new Vector3(tip.x, tip.y, 0f));
            aimArrow.SetPosition(0, new Vector3(left.x, left.y, 0f));
            aimArrow.SetPosition(1, new Vector3(tip.x, tip.y, 0f));
            aimArrow.SetPosition(2, new Vector3(right.x, right.y, 0f));

            float aimAlpha = 0.6f + 0.15f * pulse;
            Color aimColor = new Color(1f, 0.9f, 0.6f, aimAlpha);
            aimLine.startColor = aimColor;
            aimLine.endColor = aimColor;
            aimArrow.startColor = aimColor;
            aimArrow.endColor = aimColor;
        }

        private void OnDestroy()
        {
            if (dashMaterial != null) Destroy(dashMaterial);
            if (ropeMaterial != null) Destroy(ropeMaterial);
            if (dashTexture != null) Destroy(dashTexture);
        }
    }
}
