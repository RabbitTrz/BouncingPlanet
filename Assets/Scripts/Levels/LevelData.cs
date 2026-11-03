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
    }
}
