using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    public sealed class LevelValidationResult
    {
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();

        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrEmpty(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrEmpty(message))
                warnings.Add(message);
        }
    }

    public static class LevelValidator
    {
        public static LevelValidationResult Validate(LevelConfig level,
            IReadOnlyList<PlanetTypeDef> types, GameConfig config)
        {
            var result = new LevelValidationResult();
            if (level == null)
            {
                result.AddError("没有选中关卡。");
                return result;
            }

            float startRadius = level.startRadius;
            float goalRadius = level.goalRadius;
            if (goalRadius <= startRadius)
                result.AddError("目标高度必须大于起始高度。");

            if (level.IsEndless)
            {
                if (level.endless == null)
                    result.AddError("无尽关缺少 EndlessSettings。");
                else if (level.endless.densityByRadius == null ||
                         level.endless.densityByRadius.length == 0)
                    result.AddWarning("无尽密度曲线为空，将使用默认密度。");
                return result;
            }

            if (level.planets == null || level.planets.Count == 0)
            {
                result.AddError("Baked 关卡没有固定星球，请先点“生成”。");
                return result;
            }

            float sizeScale = config != null ? Mathf.Max(0.01f, config.sizeScale) : 0.5f;
            float minRadius = Mathf.Max(0.01f, level.generation.minGenerationRadius);

            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry entry = level.planets[i];
                if (entry == null)
                {
                    result.AddError($"第 {i + 1} 个星球为空。");
                    continue;
                }
                if (entry.type == null)
                {
                    result.AddError($"第 {i + 1} 个星球缺少类型。");
                    continue;
                }
                if (entry.radius < minRadius)
                    result.AddError($"第 {i + 1} 个星球半径 {entry.radius:F2} 低于最小生成半径。");
                if (entry.radius > goalRadius + level.generation.initialAbove + 1f)
                    result.AddWarning($"第 {i + 1} 个星球在目标高度以上较远，玩家可能看不到。");
                if (entry.sizeMultiplier <= 0f)
                    result.AddError($"第 {i + 1} 个星球尺寸倍率必须大于 0。");
                if (float.IsNaN(entry.angleDegrees) || float.IsInfinity(entry.angleDegrees))
                    result.AddError($"第 {i + 1} 个星球角度无效。");
            }

            // 只对固定位置做重叠检查；摆动和运行时碰撞另算。
            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry a = level.planets[i];
                if (a?.type == null)
                    continue;
                Vector2 pa = a.WorldPosition;
                float ra = a.type.baseSize * sizeScale * Mathf.Max(0f, a.sizeMultiplier);
                for (int j = i + 1; j < level.planets.Count; j++)
                {
                    LevelPlanetEntry b = level.planets[j];
                    if (b?.type == null)
                        continue;
                    Vector2 pb = b.WorldPosition;
                    float rb = b.type.baseSize * sizeScale * Mathf.Max(0f, b.sizeMultiplier);
                    float distance = Vector2.Distance(pa, pb);
                    if (distance < (ra + rb) * 0.5f)
                    {
                        result.AddError(
                            $"第 {i + 1} 和 {j + 1} 个星球严重重叠，请调整位置。");
                    }
                    else if (distance < ra + rb)
                    {
                        result.AddWarning(
                            $"第 {i + 1} 和 {j + 1} 个星球可能重叠，建议检查。");
                    }
                }
            }

            bool hasSafeCatch = false;
            bool hasPlanetNearGoal = false;
            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry entry = level.planets[i];
                if (entry == null)
                    continue;
                if (Mathf.Abs(entry.radius - (startRadius - 0.35f)) <= 0.8f &&
                    Mathf.Abs(Mathf.DeltaAngle(0f, entry.angleDegrees)) <= 35f)
                {
                    hasSafeCatch = true;
                }
                if (entry.radius >= goalRadius - 1f)
                    hasPlanetNearGoal = true;
            }

            if (!hasSafeCatch)
                result.AddWarning("出生点下方没有明显的安全落点，请确认开局不会直接掉入黑洞。");
            if (!hasPlanetNearGoal)
                result.AddWarning("目标线附近没有星球，请确认玩家能到达目标高度。");

            if (level.typeWeights != null)
            {
                for (int i = 0; i < level.typeWeights.Count; i++)
                {
                    LevelTypeWeight weight = level.typeWeights[i];
                    if (weight == null || weight.type == null)
                        result.AddWarning($"类型权重第 {i + 1} 项为空。");
                    else if (weight.weight <= 0f)
                        result.AddWarning($"类型权重第 {i + 1} 项权重为 0。");
                }
            }

            if (types != null && types.Count > 0)
            {
                for (int i = 0; i < level.planets.Count; i++)
                {
                    PlanetTypeDef type = level.planets[i]?.type;
                    if (type != null && !Contains(types, type))
                        result.AddWarning($"星球类型 {type.name} 不在当前类型列表中。");
                }
            }

            return result;
        }

        private static bool Contains(IReadOnlyList<PlanetTypeDef> types, PlanetTypeDef type)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] == type)
                    return true;
            }
            return false;
        }
    }
}
