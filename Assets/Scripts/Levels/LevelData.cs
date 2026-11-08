using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    public enum LevelMode
    {
        Baked = 0,
        Endless = 1
    }

    /// <summary>
    /// 关卡生成基线。1/2/3 关先按这套规则生成，再烘焙成固定星球列表。
    /// </summary>
    [Serializable]
    public sealed class LevelGenerationSettings
    {
        public int seed = 20261103;
        public float densityMultiplier = 1f;
        public float laneSpacing = 0.50f;
        public float ringCountMultiplier = 5.5f;
        public int minPlanetsPerRing = 6;
        public int maxPlanetsPerRing = 64;
        public float gapCountMultiplier = 0.60f;
        public float gapRadialOffset = 0.25f;
        public float gapRadiusJitter = 0.05f;
        public float sizeJitter = 0.15f;
        public float swayMax = 0.11f;
        public float swaySlotRatio = 0.22f;
        public float swaySpeedMin = 0.25f;
        public float swaySpeedMax = 0.70f;
        public float initialBelow = 1.20f;
        public float initialAbove = 3.00f;
        public float spawnAhead = 8f;
        public float backfillBehind = 4f;
        public float minGenerationRadius = 0.18f;
        public float recycleAbove = 40f;
        [LabelText("目标弧长间距（无尽）")]
        [Tooltip("无尽关按这个屏幕弧长间距计算槽位，保持不同半径下的星球密度。")]
        [Min(0.1f)] public float targetArcSpacing = 1.40f;

        [LabelText("槽位角度抖动（无尽）")]
        [Tooltip("主环星球偏离等角槽位的比例：±该值 × 槽位角，用来打散径向连线。")]
        [Range(0f, 0.5f)] public float angularJitter = 0.25f;

        [LabelText("弧窗口额外边距（无尽）")]
        [Tooltip("只实例化玩家附近角度窗口；边距越大越不容易在屏幕边缘看到生成/回收。")]
        [Min(0f)] public float arcWindowMargin = 2.00f;

        public int GetRingCount(float radius)
        {
            int raw = Mathf.FloorToInt(
                Mathf.Max(0f, radius) * ringCountMultiplier * Mathf.Max(0f, densityMultiplier));
            int max = Mathf.Max(minPlanetsPerRing, maxPlanetsPerRing);
            return Mathf.Clamp(raw, minPlanetsPerRing, max);
        }

        public int GetGapCount(float radius)
        {
            return Mathf.Max(0,
                Mathf.RoundToInt(GetRingCount(radius) * gapCountMultiplier));
        }

        public float GetSwayAmplitude(int count)
        {
            if (count <= 0)
                return 0f;
            float slot = Mathf.PI * 2f / count;
            return Mathf.Min(swayMax, slot * swaySlotRatio);
        }

        public void CopyFrom(GameConfig config)
        {
            if (config == null)
                return;

            seed = config.generationSeed;
            laneSpacing = config.laneSpacing;
            ringCountMultiplier = config.ringCountMultiplier;
            minPlanetsPerRing = config.minPlanetsPerRing;
            maxPlanetsPerRing = config.maxPlanetsPerRing;
            gapCountMultiplier = config.gapCountMultiplier;
            gapRadialOffset = config.gapRadialOffset;
            gapRadiusJitter = config.gapRadiusJitter;
            sizeJitter = config.sizeJitter;
            swayMax = config.swayMax;
            swaySlotRatio = config.swaySlotRatio;
            swaySpeedMin = config.swaySpeedMin;
            swaySpeedMax = config.swaySpeedMax;
            initialBelow = config.initialBelow;
            initialAbove = config.initialAbove;
            spawnAhead = config.spawnAhead;
            backfillBehind = config.backfillBehind;
            minGenerationRadius = config.minGenerationRadius;
            recycleAbove = config.recycleAbove;
        }

        /// <summary>运行时副本；用于换随机种子而不改到关卡资产。</summary>
        public LevelGenerationSettings Clone()
        {
            return (LevelGenerationSettings)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class LevelPlanetEntry
    {
        public PlanetTypeDef type;
        public float radius = 1.5f;
        public float angleDegrees;
        public float sizeMultiplier = 1f;
        public float swayAmplitude;
        public float swaySpeed;
        public float swayPhase;
        [TextArea] public string note;

        public Vector2 WorldPosition =>
            PolarMath.Polar(radius, angleDegrees * Mathf.Deg2Rad);
    }

    [Serializable]
    public sealed class LevelTypeWeight
    {
        public PlanetTypeDef type;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public sealed class LevelEndlessSettings
    {
        public AnimationCurve densityByRadius =
            AnimationCurve.Constant(0f, 20f, 1f);
        [Min(0.1f)] public float maxDensity = 1.5f;
        [Min(0f)] public float musouUnlockRadius;
        [Min(0f)] public float bombUnlockRadius;
        [Min(0f)] public float hookUnlockRadius;

        [Header("黑洞压力 Black Hole Pressure")]
        [LabelText("启用无尽黑洞压力")]
        public bool pressureEnabled = true;

        [LabelText("压力开始延迟（秒）")]
        [Min(0f)] public float pressureStartDelay = 10f;

        [LabelText("时间下限增长速度（世界单位/秒）")]
        [Min(0f)] public float pressureGrowthPerSecond = 0.12f;

        [LabelText("允许回落起始距离")]
        [Min(0f)] public float pressureFallbackStart = 4f;

        [LabelText("允许回落最终距离")]
        [Min(0f)] public float pressureFallbackEnd = 1.8f;

        [LabelText("回落收紧时长（秒）")]
        [Min(0.1f)] public float pressureFallbackRampSeconds = 120f;

        [LabelText("底部警告距离")]
        [Min(0f)] public float pressureWarningDistance = 2f;

        [LabelText("行星清理边距")]
        [Min(0f)] public float pressureCullMargin = 0.35f;

        [LabelText("视觉进入距离")]
        [Min(0.1f)] public float pressureVisualEnterDistance = 3f;

        [LabelText("黑洞视觉最小直径")]
        [Min(0.01f)] public float pressureVisualDiameterMin = 0.52f;

        [LabelText("黑洞视觉最大直径")]
        [Min(0.01f)] public float pressureVisualDiameterMax = 2.2f;

        [LabelText("暗色核心比例")]
        [Range(0.1f, 1f)] public float pressureEventHorizonRatio = 0.62f;
    }
}
