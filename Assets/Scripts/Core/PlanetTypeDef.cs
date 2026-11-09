using System;
using UnityEngine;

namespace BouncingPlanet
{
    public enum PlanetTypeId
    {
        Rock = 0,
        Bomb = 1,
        Cloud = 2,
        Ice = 3,
        Shiny = 4,
        GoalMarker = 5,
        MilestoneMarker = 6
    }

    public enum PlanetBehavior
    {
        Normal = 0,
        Bomb = 1,
        Cloud = 2,
        Ice = 3,
        Shiny = 4,
        Marker = 5
    }

    /// <summary>
    /// 行星直接配置的三套动画状态。美术不需要再跳去单独的 Profile 资产，
    /// 但要高级覆盖时仍可在 PlanetTypeDef.visualProfile 指定。
    /// </summary>
    [Serializable]
    public sealed class PlanetAnimationSet
    {
        [LabelText("待机 Idle")]
        public SpriteStateDefinition idle;

        [LabelText("受击 Hit")]
        public SpriteStateDefinition hit;

        [LabelText("碎裂 Shattered")]
        public SpriteStateDefinition shattered;

        public bool HasIdleFrames =>
            idle != null && idle.frames != null && idle.frames.Length > 0;

        public bool HasAnyFrames
        {
            get
            {
                if (idle != null && idle.frames != null && idle.frames.Length > 0)
                    return true;
                if (hit != null && hit.frames != null && hit.frames.Length > 0)
                    return true;
                if (shattered != null && shattered.frames != null && shattered.frames.Length > 0)
                    return true;
                return false;
            }
        }

        public void EnsureDefaults()
        {
            if (idle == null)
            {
                idle = CreateState("Idle", true, false, 1.5f);
            }
            else
            {
                idle.stateName = "Idle";
                NormalizeScale(idle);
            }

            if (hit == null)
            {
                hit = CreateState("Hit", false, true, 0.18f);
            }
            else
            {
                hit.stateName = "Hit";
                NormalizeScale(hit);
            }

            if (shattered == null)
            {
                shattered = CreateState("Shattered", false, false, 0.28f);
            }
            else
            {
                shattered.stateName = "Shattered";
                NormalizeScale(shattered);
            }
        }

        private static SpriteStateDefinition CreateState(string stateName, bool loop,
            bool autoReturn, float duration)
        {
            SpriteStateDefinition state = new SpriteStateDefinition
            {
                stateName = stateName,
                loop = loop,
                autoReturn = autoReturn,
                durationOverride = duration,
                baseScale = Vector2.one
            };
            return state;
        }

        private static void NormalizeScale(SpriteStateDefinition state)
        {
            if (Mathf.Approximately(state.baseScale.x, 0f) &&
                Mathf.Approximately(state.baseScale.y, 0f))
            {
                state.baseScale = Vector2.one;
            }
        }
    }

    /// <summary>
    /// 单种星球的配置。Initial Demo v1.0 只使用 Rock/Bomb/Cloud/Ice/Shiny。
    /// 所有数值都是占位基线，可在 Inspector 或运行时调参面板修改。
    /// </summary>
    [CreateAssetMenu(menuName = "BouncingPlanet/Planet Type", fileName = "PlanetType")]
    public sealed class PlanetTypeDef : ScriptableObject
    {
        [Header("身份 Identity")]
        [LabelText("类型 ID")]
        public PlanetTypeId id = PlanetTypeId.Rock;

        [LabelText("显示名称")]
        public string displayName = "Rock";

        [LabelText("行为类型")]
        public PlanetBehavior behavior = PlanetBehavior.Normal;

        [Header("视觉 Visual")]
        [LabelText("主色")]
        public Color color = new Color(0.56f, 0.72f, 0.85f, 1f);

        [LabelText("球体贴图")]
        public Sprite bodySprite;

        [LabelText("细节贴图")]
        public Sprite detailSprite;

        [LabelText("脸部贴图")]
        public Sprite faceSprite;

        [LabelText("碎裂贴图")]
        public Sprite crashSprite;

        [Header("动画状态 Animation States")]
        [LabelText("Idle / Hit / Shattered")]
        [Tooltip("直接在这个行星资产里配置三套动画状态；有 Idle 帧时会优先使用这里的配置。")]
        public PlanetAnimationSet animationStates = new PlanetAnimationSet();

        [LabelText("高级覆盖 Profile（可选）")]
        [Tooltip("可选的高级覆盖：当上面 Idle 没有帧时使用该 Profile；两者都为空则使用运行时默认。")]
        public SpriteStateProfile visualProfile;

        [Header("尺寸 / 权重 Size / Weight")]
        [Tunable("Planet", 0.02f, 0.35f, 0.005f, "基础半径")]
        public float baseSize = 0.120f;

        [Tunable("Planet", 0f, 10f, 0.1f, "生成权重")]
        public float spawnWeight = 7f;

        [Header("待机摆动 Idle Sway")]
        [Tunable("Planet", 0f, 15f, 0.1f, "待机摆动角度")]
        [Tooltip("轻度左右旋转的角度；开启摆动时会替换 Idle 的呼吸缩放。")]
        public float idleSwayAngle = 2.5f;

        [Tunable("Planet", 0.1f, 5f, 0.05f, "待机摆动频率 Hz")]
        [Tooltip("默认约等于黑洞 7 帧 / 10fps 的循环频率。")]
        public float idleSwayFrequency = 1.43f;

        [Tunable("Planet", 2f, 16f, 1f, "待机摆动帧数")]
        [Tooltip("一个循环用几个离散姿势；7 帧配 1.43Hz 约等于 10fps。")]
        public int idleSwaySteps = 7;

        [Tunable("Planet", 0f, 1f, 1f, "启用待机摆动")]
        public bool idleSwayEnabled = true;

        [Header("撞碎 Smash")]
        [Tunable("Planet", 0f, 20f, 0.05f, "撞碎速度阈值")]
        [Tooltip("作为被撞目标时的普通上升撞碎阈值；填 0 表示使用 GameConfig 的全局阈值。")]
        public float smashSpeedThreshold = 0f;

        [Tunable("Planet", 0f, 1f, 1f, "普通撞碎免疫")]
        [Tooltip("开启后普通上升撞碎无效；无双仍然可以撞碎。")]
        public bool normalSmashImmune = false;

        [Header("弹跳 / 分数 Bounce / Score")]
        [Tunable("Planet", 0f, 3f, 0.01f, "弹力倍率")]
        [Tooltip("通用弹力倍率；冰冰星使用下面的冰星弹力加成。")]
        public float bounceMultiplier = 1f;

        [Tunable("Planet", 0f, 10f, 0.1f, "分数倍率")]
        public float scoreMultiplier = 1f;

        [Header("冰 Ice")]
        [Tunable("Ice", 1f, 5f, 0.01f, "冰星弹力加成")]
        [Tooltip("冰冰星下落弹起的专用倍率，与通用 bounceMultiplier 独立。")]
        public float iceBounceBonus = 1.15f;

        [Tunable("Ice", 0f, 1f, 1f, "首次碰撞即碎")]
        [Tooltip("当前口径：冰冰星被撞一次后碎裂；后续如需改为可反复撞可关闭。")]
        public bool iceBreaksOnFirstHit = true;

        [Header("云 Cloud")]
        [Tunable("Cloud", 0f, 10f, 0.1f, "穿过速度加值")]
        [Tooltip("加算值：穿过云朵后径向速度 = 原速度 + 该值；是否封顶由下方开关控制。")]
        public float cloudSpeedMultiplier = 2.2f;

        [LabelText("启用穿过速度上限")]
        [Tooltip("开启后限制云朵加值后的最终速度；原速度已超过上限时不会被削减。")]
        public bool cloudSpeedCapEnabled = true;

        [LabelText("穿过速度上限")]
        [Tooltip("最终径向速度不超过 max(进入速度, 该值)，只限制云朵的加值结果。")]
        public float cloudSpeedCap = 3.2f;

        [Header("Marker")]
        [Tooltip("标记星球：没有碰撞，玩家首次越过后切换为点亮状态。")]
        public bool isMarker;
        [Tooltip("点亮后的贴图；留空时只改用 markerReachedColor。")]
        public Sprite markerReachedSprite;
        [Tooltip("点亮后的颜色。")]
        public Color markerReachedColor = new Color(1f, 0.98f, 0.7f, 1f);

        [Header("炸弹 Bomb")]
        [Tunable("Bomb", 0.1f, 5f, 0.05f, "爆炸特效半径")]
        public float bombFxRadius = 0.9f;

        [Tunable("Bomb", 0f, 10f, 0.05f, "玩家推力速度")]
        public float bombPushSpeed = 2.9f;

        [Tunable("Bomb", 0f, 10f, 0.05f, "玩家推力半径")]
        public float bombPushRadius = 2.16f;

        public bool IsSpecial => behavior != PlanetBehavior.Normal;

        /// <summary>把该类型恢复成 Initial Demo v1.0 的占位默认值。</summary>
        public void ResetToDefaults(PlanetTypeId type)
        {
            id = type;
            bodySprite = null;
            detailSprite = null;
            faceSprite = null;
            crashSprite = null;
            visualProfile = null;
            animationStates = new PlanetAnimationSet();
            animationStates.EnsureDefaults();
            idleSwayAngle = 2.5f;
            idleSwayFrequency = 1.43f;
            idleSwaySteps = 7;
            idleSwayEnabled = true;
            smashSpeedThreshold = 0f;
            normalSmashImmune = false;

            switch (type)
            {
                case PlanetTypeId.Bomb:
                    displayName = "炸弹星";
                    behavior = PlanetBehavior.Bomb;
                    color = new Color(1f, 0.616f, 0.42f, 1f);
                    baseSize = 0.115f;
                    spawnWeight = 2f;
                    bounceMultiplier = 1f;
                    scoreMultiplier = 1f;
                    break;

                case PlanetTypeId.Cloud:
                    displayName = "云朵星";
                    behavior = PlanetBehavior.Cloud;
                    color = new Color(0.91f, 0.945f, 1f, 1f);
                    baseSize = 0.150f;
                    spawnWeight = 1f;
                    bounceMultiplier = 1f;
                    scoreMultiplier = 1f;
                    cloudSpeedMultiplier = 2.2f;
                    cloudSpeedCapEnabled = true;
                    cloudSpeedCap = 3.2f;
                    normalSmashImmune = true;
                    break;

                case PlanetTypeId.Ice:
                    displayName = "冰冰星";
                    behavior = PlanetBehavior.Ice;
                    color = new Color(0.75f, 0.918f, 1f, 1f);
                    baseSize = 0.110f;
                    spawnWeight = 3f;
                    bounceMultiplier = 1.15f;
                    scoreMultiplier = 1f;
                    iceBounceBonus = 1.15f;
                    iceBreaksOnFirstHit = true;
                    break;

                case PlanetTypeId.Shiny:
                    displayName = "闪亮星";
                    behavior = PlanetBehavior.Shiny;
                    color = new Color(1f, 0.878f, 0.541f, 1f);
                    baseSize = 0.105f;
                    spawnWeight = 1f;
                    bounceMultiplier = 1f;
                    scoreMultiplier = 3f;
                    break;

                default:
                    displayName = "岩石星";
                    behavior = PlanetBehavior.Normal;
                    color = new Color(0.56f, 0.72f, 0.85f, 1f);
                    baseSize = 0.120f;
                    spawnWeight = 7f;
                    bounceMultiplier = 1f;
                    scoreMultiplier = 1f;
                    break;
            }
        }

        private void OnValidate()
        {
            if (animationStates == null)
                animationStates = new PlanetAnimationSet();
            animationStates.EnsureDefaults();
            idleSwayAngle = Mathf.Clamp(idleSwayAngle, 0f, 15f);
            idleSwayFrequency = Mathf.Max(0.01f, idleSwayFrequency);
            idleSwaySteps = Mathf.Clamp(idleSwaySteps, 2, 16);
            smashSpeedThreshold = Mathf.Max(0f, smashSpeedThreshold);
            cloudSpeedCap = Mathf.Max(0f, cloudSpeedCap);
        }
    }
}
