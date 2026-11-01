using UnityEngine;

namespace BouncingPlanet
{
    public enum PlanetTypeId
    {
        Rock = 0,
        Bomb = 1,
        Cloud = 2,
        Ice = 3,
        Shiny = 4
    }

    public enum PlanetBehavior
    {
        Normal = 0,
        Bomb = 1,
        Cloud = 2,
        Ice = 3,
        Shiny = 4
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

        [Header("尺寸 / 权重 Size / Weight")]
        [Tunable("Planet", 0.02f, 0.35f, 0.005f, "基础半径")]
        public float baseSize = 0.120f;

        [Tunable("Planet", 0f, 10f, 0.1f, "生成权重")]
        public float spawnWeight = 7f;

        [Header("弹跳 / 分数 Bounce / Score")]
        [Tunable("Planet", 0f, 3f, 0.01f, "弹力倍率")]
        public float bounceMultiplier = 1f;

        [Tunable("Planet", 0f, 10f, 0.1f, "分数倍率")]
        public float scoreMultiplier = 1f;

        [Header("冰 Ice")]
        [Tunable("Ice", 1f, 2f, 0.01f, "冰星弹力加成")]
        public float iceBounceBonus = 1.15f;

        [Tunable("Ice", 0f, 1f, 1f, "首次碰撞即碎")]
        [Tooltip("当前口径：冰冰星被撞一次后碎裂；后续如需改为可反复撞可关闭。")]
        public bool iceBreaksOnFirstHit = true;

        [Header("云 Cloud")]
        [Tunable("Cloud", 1f, 5f, 0.05f, "穿过加速倍率")]
        public float cloudSpeedMultiplier = 2.2f;

        [Tunable("Cloud", 0.5f, 10f, 0.1f, "穿过速度上限")]
        public float cloudSpeedCap = 3.2f;

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

            switch (type)
            {
                case PlanetTypeId.Bomb:
                    displayName = "炸弹星";
                    behavior = PlanetBehavior.Bomb;
                    color = new Color(1f, 0.616f, 0.42f, 1f);
                    baseSize = 0.115f;
                    spawnWeight = 2f;
                    bounceMultiplier = 1f;
                    scoreMultiplier = 0f;
                    break;

                case PlanetTypeId.Cloud:
                    displayName = "云朵星";
                    behavior = PlanetBehavior.Cloud;
                    color = new Color(0.91f, 0.945f, 1f, 1f);
                    baseSize = 0.150f;
                    spawnWeight = 1f;
                    bounceMultiplier = 1f;
                    scoreMultiplier = 0f;
                    cloudSpeedMultiplier = 2.2f;
                    cloudSpeedCap = 3.2f;
                    break;

                case PlanetTypeId.Ice:
                    displayName = "冰冰星";
                    behavior = PlanetBehavior.Ice;
                    color = new Color(0.75f, 0.918f, 1f, 1f);
                    baseSize = 0.110f;
                    spawnWeight = 3f;
                    bounceMultiplier = 1.15f;
                    scoreMultiplier = 0f;
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
    }
}
