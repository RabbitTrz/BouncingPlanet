using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 关卡基线生成器。编辑器用它生成 Baked 关卡，运行时也可以复用。
    /// 生成结果是确定性的：同一个 LevelConfig + seed + 类型列表会得到同样的布局。
    /// </summary>
    public static class LevelLayoutBuilder
    {
        public static List<LevelPlanetEntry> Generate(LevelConfig level,
            IReadOnlyList<PlanetTypeDef> types, float previewMaxRadius = -1f)
        {
            var result = new List<LevelPlanetEntry>();
            if (level == null)
                return result;

            LevelGenerationSettings settings = level.generation;
            if (settings == null)
                return result;

            float maxRadius = previewMaxRadius > 0f
                ? previewMaxRadius
                : Mathf.Max(level.goalRadius, level.startRadius) + settings.initialAbove;
            float lowRadius = Mathf.Max(settings.minGenerationRadius,
                level.startRadius - settings.initialBelow);

            var rng = new System.Random(settings.seed);
            for (float radius = lowRadius; radius < maxRadius; radius += settings.laneSpacing)
            {
                AddBand(result, level, types, rng, radius);
            }

            AddSafeCatchPlanet(result, level, types, rng);
            result.Sort((a, b) =>
            {
                int radiusCompare = a.radius.CompareTo(b.radius);
                return radiusCompare != 0
                    ? radiusCompare
                    : a.angleDegrees.CompareTo(b.angleDegrees);
            });
            return result;
        }

        private static void AddBand(List<LevelPlanetEntry> output, LevelConfig level,
            IReadOnlyList<PlanetTypeDef> types, System.Random rng, float radius)
        {
            if (radius < level.generation.minGenerationRadius)
                return;

            int count;
            if (level.IsEndless)
            {
                float density = level.generation.densityMultiplier;
                if (level.endless != null && level.endless.densityByRadius != null)
                {
                    float curve = level.endless.densityByRadius.Evaluate(radius);
                    density *= Mathf.Clamp(curve, 0.1f, level.endless.maxDensity);
                }
                float spacing = Mathf.Max(0.1f, level.generation.targetArcSpacing);
                count = Mathf.RoundToInt(
                    Mathf.PI * 2f * radius / spacing * Mathf.Max(0.01f, density));
                count = Mathf.Max(level.generation.minPlanetsPerRing, count);
            }
            else
            {
                count = level.generation.GetRingCount(radius);
            }

            if (count <= 0)
                return;

            float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
            float sway = level.generation.GetSwayAmplitude(count);
            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * (Mathf.PI * 2f / count);
                if (InSpawnClearZone(level, radius, angle))
                    continue;
                AddEntry(output, level, types, rng, radius, angle, sway);
            }

            float gapRadius = radius - level.generation.laneSpacing * 0.5f;
            if (gapRadius < level.generation.minGenerationRadius)
                return;

            int gapCount = level.IsEndless
                ? Mathf.RoundToInt(count * level.generation.gapCountMultiplier)
                : level.generation.GetGapCount(radius);
            float gapSway = Mathf.Min(level.generation.swayMax, 0.08f);
            for (int i = 0; i < gapCount; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float jitter = ((float)rng.NextDouble() * 2f - 1f) *
                               level.generation.gapRadiusJitter;
                float r = gapRadius + jitter;
                if (r < level.generation.minGenerationRadius ||
                    InSpawnClearZone(level, r, angle))
                {
                    continue;
                }
                AddEntry(output, level, types, rng, r, angle, gapSway);
            }
        }

        private static void AddEntry(List<LevelPlanetEntry> output, LevelConfig level,
            IReadOnlyList<PlanetTypeDef> types, System.Random rng, float radius,
            float angle, float swayAmplitude)
        {
            PlanetTypeDef type = PickType(level, types, rng);
            if (type == null)
                return;

            float sizeRoll = 1f +
                ((float)rng.NextDouble() * 2f - 1f) * level.generation.sizeJitter;
            float swaySpeed = swayAmplitude > 0f
                ? Mathf.Lerp(level.generation.swaySpeedMin,
                    level.generation.swaySpeedMax, (float)rng.NextDouble())
                : 0f;

            output.Add(new LevelPlanetEntry
            {
                type = type,
                radius = radius,
                angleDegrees = NormalizeDegrees(angle * Mathf.Rad2Deg),
                sizeMultiplier = Mathf.Max(0.05f, sizeRoll),
                swayAmplitude = swayAmplitude,
                swaySpeed = swaySpeed,
                swayPhase = (float)rng.NextDouble() * Mathf.PI * 2f
            });
        }

        private static PlanetTypeDef PickType(LevelConfig level,
            IReadOnlyList<PlanetTypeDef> types, System.Random rng)
        {
            float total = 0f;
            if (level.typeWeights != null)
            {
                for (int i = 0; i < level.typeWeights.Count; i++)
                {
                    LevelTypeWeight entry = level.typeWeights[i];
                    if (entry != null && entry.type != null)
                        total += Mathf.Max(0f, entry.weight);
                }
            }

            if (total > 0f)
            {
                float roll = (float)rng.NextDouble() * total;
                for (int i = 0; i < level.typeWeights.Count; i++)
                {
                    LevelTypeWeight entry = level.typeWeights[i];
                    if (entry == null || entry.type == null)
                        continue;
                    roll -= Mathf.Max(0f, entry.weight);
                    if (roll <= 0f)
                        return entry.type;
                }
            }

            if (types == null || types.Count == 0)
                return null;

            total = 0f;
            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] != null)
                    total += Mathf.Max(0f, types[i].spawnWeight);
            }

            if (total <= 0f)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i] != null)
                        return types[i];
                }
                return null;
            }

            float fallbackRoll = (float)rng.NextDouble() * total;
            for (int i = 0; i < types.Count; i++)
            {
                PlanetTypeDef type = types[i];
                if (type == null)
                    continue;
                fallbackRoll -= Mathf.Max(0f, type.spawnWeight);
                if (fallbackRoll <= 0f)
                    return type;
            }
            return types[types.Count - 1];
        }

        private static void AddSafeCatchPlanet(List<LevelPlanetEntry> output,
            LevelConfig level, IReadOnlyList<PlanetTypeDef> types, System.Random rng)
        {
            PlanetTypeDef safeType = null;
            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    PlanetTypeDef type = types[i];
                    if (type != null && type.id == PlanetTypeId.Rock)
                    {
                        safeType = type;
                        break;
                    }
                }
                if (safeType == null)
                {
                    for (int i = 0; i < types.Count; i++)
                    {
                        if (types[i] != null &&
                            types[i].behavior == PlanetBehavior.Normal)
                        {
                            safeType = types[i];
                            break;
                        }
                    }
                }
            }

            if (safeType == null && level.typeWeights != null)
            {
                for (int i = 0; i < level.typeWeights.Count; i++)
                {
                    LevelTypeWeight entry = level.typeWeights[i];
                    if (entry != null && entry.type != null)
                    {
                        safeType = entry.type;
                        break;
                    }
                }
            }

            if (safeType == null)
                return;

            float radius = Mathf.Max(level.generation.minGenerationRadius,
                level.startRadius - 0.35f);
            output.Add(new LevelPlanetEntry
            {
                type = safeType,
                radius = radius,
                angleDegrees = 0f,
                sizeMultiplier = 1f,
                swayAmplitude = 0f,
                swaySpeed = 0f,
                swayPhase = (float)rng.NextDouble() * Mathf.PI * 2f
            });
        }

        private static bool InSpawnClearZone(LevelConfig level, float radius, float angle)
        {
            float radialDelta = radius - level.startRadius;
            if (radialDelta < -0.5f || radialDelta > 0.35f)
                return false;
            return Mathf.Abs(NormalizeAngle(angle)) < 0.28f;
        }

        private static float NormalizeAngle(float angle)
        {
            angle = (angle + Mathf.PI) % (Mathf.PI * 2f);
            if (angle < 0f)
                angle += Mathf.PI * 2f;
            return angle - Mathf.PI;
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            if (degrees < 0f)
                degrees += 360f;
            return degrees;
        }
    }
}
