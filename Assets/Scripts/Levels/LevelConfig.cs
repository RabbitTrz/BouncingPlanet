using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 单关配置。1/2/3 关使用 Baked：先生成基线，再手工改固定星球列表；
    /// 无尽关使用 Endless：运行时按规则持续生成。
    /// </summary>
    [CreateAssetMenu(menuName = "BouncingPlanet/Level Config", fileName = "Level")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("身份 Identity")]
        public string levelId = "level_1";
        public string displayName = "第一关";
        public int order = 1;
        public LevelMode mode = LevelMode.Baked;
        public bool unlockedByDefault = true;
        [TextArea] public string description;

        [Header("目标 Goal")]
        [Min(0.1f)] public float goalRadius = 11f;

        [Header("技能 Skills")]
        public bool allowMusou = true;
        public bool allowBomb = true;
        public bool allowHook = true;

        [Header("生成基线 Generation")]
        [Min(0.1f)] public float startRadius = 1.5f;
        public LevelGenerationSettings generation = new LevelGenerationSettings();
        public List<LevelTypeWeight> typeWeights = new List<LevelTypeWeight>();

        [Header("固定星球（Baked）")]
        public List<LevelPlanetEntry> planets = new List<LevelPlanetEntry>();

        [Header("无尽规则 Endless")]
        public LevelEndlessSettings endless = new LevelEndlessSettings();
        [Min(2f)] public float endlessPreviewRadius = 20f;

        public bool IsEndless => mode == LevelMode.Endless;

        public void CopyGenerationFrom(GameConfig config)
        {
            if (config == null)
                return;
            startRadius = config.startRadius;
            generation.CopyFrom(config);
        }

        private void OnValidate()
        {
            goalRadius = Mathf.Max(0.1f, goalRadius);
            startRadius = Mathf.Max(0.1f, startRadius);
            endlessPreviewRadius = Mathf.Max(2f, endlessPreviewRadius);

            if (generation == null)
                generation = new LevelGenerationSettings();
            if (endless == null)
                endless = new LevelEndlessSettings();
            if (planets == null)
                planets = new List<LevelPlanetEntry>();
            if (typeWeights == null)
                typeWeights = new List<LevelTypeWeight>();

            generation.minPlanetsPerRing = Mathf.Max(0, generation.minPlanetsPerRing);
            generation.maxPlanetsPerRing = Mathf.Max(
                generation.minPlanetsPerRing, generation.maxPlanetsPerRing);
            generation.laneSpacing = Mathf.Max(0.01f, generation.laneSpacing);
            generation.minGenerationRadius = Mathf.Max(0.01f, generation.minGenerationRadius);
            if (generation.swaySpeedMax < generation.swaySpeedMin)
                generation.swaySpeedMax = generation.swaySpeedMin;
        }
    }
}
