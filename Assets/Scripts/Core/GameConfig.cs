using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Initial Demo v1.0 的全局配置。
    /// 所有数值都是占位基线，优先保证可调，不追求最终平衡。
    /// Inspector 与运行时调参面板都显示中文标签。
    /// </summary>
    [CreateAssetMenu(menuName = "BouncingPlanet/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("规则 Rules")]
        [LabelText("规则口径")]
        [Tooltip("当前实现口径。Initial Demo v1.1 不做无双/钩锁/炸弹技能/关卡/距离缩放。")]
        public RulesProfile profile = RulesProfile.InitialDemoV1;

        [Header("键位 Key Bindings")]
        [LabelText("左移主键")]
        public KeyCode moveLeftKey = KeyCode.A;

        [LabelText("左移副键")]
        public KeyCode moveLeftAltKey = KeyCode.LeftArrow;

        [LabelText("右移主键")]
        public KeyCode moveRightKey = KeyCode.D;

        [LabelText("右移副键")]
        public KeyCode moveRightAltKey = KeyCode.RightArrow;

        [LabelText("重生键")]
        public KeyCode respawnKey = KeyCode.R;

        [LabelText("锁定半径键")]
        public KeyCode holdRadiusKey = KeyCode.H;

        [Header("仿真 Simulation")]
        [Tunable("Simulation", 30f, 240f, 1f, "仿真频率 (Hz)")]
        [Tooltip("固定仿真频率。默认 120 Hz；只影响逻辑步进，不影响渲染。")]
        public int simulationHz = 120;

        [Tunable("Simulation", 1f, 16f, 1f, "单帧最大步数")]
        [Tooltip("单帧最多补算多少个固定步，防止卡顿后追帧失控。")]
        public int maxStepsPerFrame = 8;

        [Header("相机 Camera")]
        [Tunable("Camera", 100f, 1000f, 10f, "像素 / 世界单位")]
        [Tooltip("参考像素密度：1 世界单位对应多少参考像素。默认 360。")]
        public float cameraPixelsPerUnit = 360f;

        [Tunable("Camera", 0.3f, 3f, 0.01f, "正交相机半高")]
        [Tooltip("正交相机半高。默认 1.0，对应 720p 下 1 世界单位 = 360px。")]
        public float cameraOrthographicSize = 1f;

        [LabelText("相机背景色")]
        public Color backgroundColor = new Color(0.02f, 0.024f, 0.04f, 1f);

        [Header("玩家移动 Player Movement")]
        [Tunable("Player", 0.02f, 0.4f, 0.005f, "玩家碰撞半径")]
        [Tooltip("玩家碰撞半径，乘统一尺寸缩放后得到世界半径。")]
        public float playerCollisionRadius = 0.085f;

        [Tunable("Player", 0.1f, 2f, 0.01f, "统一尺寸缩放")]
        [Tooltip("玩家与星球共用的尺寸缩放。默认 0.5。")]
        public float sizeScale = 0.5f;

        [Tunable("Player", 0.2f, 6f, 0.05f, "初始半径")]
        public float startRadius = 1.50f;

        [Tunable("Player", -3f, 3f, 0.05f, "初始径向速度")]
        public float startRadialVelocity = 0.60f;

        [Tunable("Player", 0.01f, 0.5f, 0.005f, "死亡半径")]
        public float deathRadius = 0.10f;

        [Tunable("Player", 0.1f, 5f, 0.05f, "最大角速度")]
        public float maxAngularSpeed = 2.60f;

        [Tunable("Player", 0.1f, 5f, 0.05f, "切向速度上限")]
        public float tangentialSpeed = 1.10f;

        [Tunable("Player", 0.1f, 10f, 0.05f, "转向加速度")]
        public float turnAcceleration = 2.40f;

        [Tunable("Player", 0.1f, 10f, 0.05f, "松手滑行减速度")]
        public float glideDeceleration = 1.35f;

        [Tunable("Player", 0.2f, 20f, 0.1f, "反向刹车减速度")]
        public float brakeDeceleration = 2.00f;

        [Tunable("Player", 0f, 2f, 0.01f, "上升返回系数（1=原速）")]
        [Tooltip("上升撞到星球时，径向速度变为 -落速 × 该系数。当前口径默认 1.0（原速返回），后续按手感再调。")]
        public float ascendBlockFactor = 1.0f;

        [Tunable("Player", 0f, 10f, 0.05f, "引力 G")]
        public float gravity = 2.40f;

        [Header("弹跳 Bounce")]
        [Tunable("Bounce", 1f, 3f, 0.01f, "撞击增幅 E")]
        public float bounceGain = 1.75f;

        [Tunable("Bounce", 0f, 3f, 0.05f, "保底冲量 J")]
        public float baseImpulse = 1.00f;

        [Tunable("Bounce", 0.5f, 3f, 0.05f, "起跳速度基准")]
        public float jumpSpeed = 1.60f;

        [Tunable("Bounce", 0.5f, 1f, 0.01f, "起跳下限倍率")]
        public float launchMinFactor = 0.82f;

        [Tunable("Bounce", 1f, 2f, 0.01f, "起跳上限倍率")]
        public float launchMaxFactor = 1.28f;

        [Tunable("Bounce", 0f, 1f, 1f, "启用起跳上限（暂关）")]
        [Tooltip("关闭时不做上弹速度上限截断；仍保留最小起跳速度。后续调试再决定是否开启。")]
        public bool useLaunchMax = false;

        [Header("星环与生成 Star Rings / Generation")]
        [Tunable("World", 0.1f, 2f, 0.01f, "轨道间距")]
        public float laneSpacing = 0.50f;

        [Tunable("World", 1f, 10f, 0.1f, "每环数量系数")]
        public float ringCountMultiplier = 5.5f;

        [Tunable("World", 1f, 32f, 1f, "每环最少行星")]
        public int minPlanetsPerRing = 6;

        [Tunable("World", 1f, 128f, 1f, "每环最多行星")]
        public int maxPlanetsPerRing = 64;

        [Tunable("World", 0f, 2f, 0.05f, "间隙球数量系数")]
        public float gapCountMultiplier = 0.60f;

        [Tunable("World", 0f, 1f, 0.01f, "间隙球半径偏移")]
        public float gapRadialOffset = 0.25f;

        [Tunable("World", 0f, 0.2f, 0.005f, "间隙球半径抖动")]
        public float gapRadiusJitter = 0.05f;

        [Tunable("World", 0f, 0.5f, 0.01f, "行星大小抖动")]
        public float sizeJitter = 0.15f;

        [Tunable("World", 0f, 0.3f, 0.005f, "最大摆幅 (rad)")]
        public float swayMax = 0.11f;

        [Tunable("World", 0f, 0.5f, 0.01f, "摆幅占格位比例")]
        public float swaySlotRatio = 0.22f;

        [Tunable("World", 0f, 2f, 0.01f, "摆动速度下限")]
        public float swaySpeedMin = 0.25f;

        [Tunable("World", 0f, 2f, 0.01f, "摆动速度上限")]
        public float swaySpeedMax = 0.70f;

        [Tunable("World", 0.1f, 3f, 0.05f, "初始向下生成距离")]
        public float initialBelow = 1.20f;

        [Tunable("World", 0.1f, 10f, 0.05f, "初始向上生成距离")]
        public float initialAbove = 3.00f;

        [Tunable("World", 0.05f, 0.5f, 0.01f, "初始最低半径")]
        public float initialLowRadius = 0.30f;

        [Tunable("World", 1f, 20f, 0.1f, "向上生成距离")]
        public float spawnAhead = 8f;

        [Tunable("World", 1f, 10f, 0.1f, "向下回填距离")]
        public float backfillBehind = 4f;

        [Tunable("World", 0.02f, 0.5f, 0.01f, "最低生成半径")]
        public float minGenerationRadius = 0.18f;

        [Tunable("World", 5f, 100f, 1f, "回收高度")]
        public float recycleAbove = 40f;

        [Tunable("World", 0f, 9999999f, 1f, "生成种子")]
        [Tooltip("相同的种子会生成相同的星环布局。修改后点调参面板“重建世界”生效。")]
        public int generationSeed = 20261101;

        [Header("碰撞 Cooldowns")]
        [Tunable("Collision", 0.05f, 1f, 0.01f, "普通行星受击冷却")]
        public float planetHitCooldown = 0.28f;

        [Tunable("Collision", 0.1f, 2f, 0.05f, "云朵受击冷却")]
        public float cloudHitCooldown = 0.8f;

        [Header("分数 Score")]
        [Tunable("Score", 0f, 100f, 1f, "普通星基础分")]
        [Tooltip("普通星每次碰撞的基础分；不同类型再乘 scoreMultiplier。")]
        public int normalPlanetScore = 10;

        [Tunable("Score", 0f, 10000f, 10f, "无双积分阈值")]
        [Tooltip("未来无双入口阈值。v1.0 只保存，不触发无双。")]
        public int musouScoreThreshold = 100;

        [Tunable("Score", 0f, 1f, 1f, "显示分数 HUD")]
        public bool showScoreHud = true;

        [Header("音频 Audio")]
        [Tunable("Audio", 0f, 1f, 0.01f, "主音量")]
        public float audioMasterVolume = 0.65f;

        // ------------------------------------------------------------------
        // Derived values
        // ------------------------------------------------------------------
        public float FixedDeltaTime => 1f / Mathf.Clamp(simulationHz, 1, 1000);
        public float EffectivePlayerRadius => Mathf.Max(0.001f, playerCollisionRadius * sizeScale);
        public float PlayerDiameterWorld => EffectivePlayerRadius * 2f;
        public float PlayerScreenDiameter => PlayerDiameterWorld * cameraPixelsPerUnit;
        public float LaunchMin => jumpSpeed * Mathf.Min(launchMinFactor, launchMaxFactor);
        public float LaunchMax => useLaunchMax
            ? jumpSpeed * Mathf.Max(launchMinFactor, launchMaxFactor)
            : float.PositiveInfinity;

        public float ClampLaunch(float rawVelocity)
        {
            return Mathf.Clamp(rawVelocity, LaunchMin, LaunchMax);
        }

        public float AscendBlockVelocity(float incomingSpeed)
        {
            return -Mathf.Abs(incomingSpeed) * ascendBlockFactor;
        }

        public int GetRingCount(float radius)
        {
            int raw = Mathf.FloorToInt(Mathf.Max(0f, radius) * ringCountMultiplier);
            return Mathf.Clamp(raw, minPlanetsPerRing, maxPlanetsPerRing);
        }

        public int GetGapCount(float radius)
        {
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0f, radius) * gapCountMultiplier));
        }

        public float GetSwayAmplitude(int planetsInRing)
        {
            float slot = Mathf.PI * 2f / Mathf.Max(1, planetsInRing);
            return Mathf.Min(swayMax, slot * swaySlotRatio);
        }

        public float GetSwaySpeed(System.Random rng)
        {
            float min = Mathf.Min(swaySpeedMin, swaySpeedMax);
            float max = Mathf.Max(swaySpeedMin, swaySpeedMax);
            return min + (float)rng.NextDouble() * (max - min);
        }

        public float GetPlanetWorldRadius(PlanetTypeDef type)
        {
            return type == null ? 0f : type.baseSize * sizeScale;
        }

        public float GetPlanetScreenDiameter(PlanetTypeDef type)
        {
            return GetPlanetWorldRadius(type) * 2f * cameraPixelsPerUnit;
        }

        // ------------------------------------------------------------------
        // Defaults / validation
        // ------------------------------------------------------------------
        [ContextMenu("Reset To Initial Demo Defaults")]
        public void ResetToDefaults()
        {
            profile = RulesProfile.InitialDemoV1;

            moveLeftKey = KeyCode.A;
            moveLeftAltKey = KeyCode.LeftArrow;
            moveRightKey = KeyCode.D;
            moveRightAltKey = KeyCode.RightArrow;
            respawnKey = KeyCode.R;
            holdRadiusKey = KeyCode.H;

            simulationHz = 120;
            maxStepsPerFrame = 8;

            cameraPixelsPerUnit = 360f;
            cameraOrthographicSize = 1f;
            backgroundColor = new Color(0.02f, 0.024f, 0.04f, 1f);

            playerCollisionRadius = 0.085f;
            sizeScale = 0.5f;
            startRadius = 1.50f;
            startRadialVelocity = 0.60f;
            deathRadius = 0.10f;
            maxAngularSpeed = 2.60f;
            tangentialSpeed = 1.10f;
            turnAcceleration = 2.40f;
            glideDeceleration = 1.35f;
            brakeDeceleration = 2.00f;
            ascendBlockFactor = 1.0f;
            gravity = 2.40f;

            bounceGain = 1.75f;
            baseImpulse = 1.00f;
            jumpSpeed = 1.60f;
            launchMinFactor = 0.82f;
            launchMaxFactor = 1.28f;
            useLaunchMax = false;

            laneSpacing = 0.50f;
            ringCountMultiplier = 5.5f;
            minPlanetsPerRing = 6;
            maxPlanetsPerRing = 64;
            gapCountMultiplier = 0.60f;
            gapRadialOffset = 0.25f;
            gapRadiusJitter = 0.05f;
            sizeJitter = 0.15f;
            swayMax = 0.11f;
            swaySlotRatio = 0.22f;
            swaySpeedMin = 0.25f;
            swaySpeedMax = 0.70f;
            initialBelow = 1.20f;
            initialAbove = 3.00f;
            initialLowRadius = 0.30f;
            spawnAhead = 8f;
            backfillBehind = 4f;
            minGenerationRadius = 0.18f;
            recycleAbove = 40f;
            generationSeed = 20261101;
            planetHitCooldown = 0.28f;
            cloudHitCooldown = 0.8f;

            normalPlanetScore = 10;
            musouScoreThreshold = 100;
            showScoreHud = true;
            audioMasterVolume = 0.65f;
        }

        private void OnValidate()
        {
            simulationHz = Mathf.Clamp(simulationHz, 1, 1000);
            maxStepsPerFrame = Mathf.Clamp(maxStepsPerFrame, 1, 64);

            cameraPixelsPerUnit = Mathf.Max(1f, cameraPixelsPerUnit);
            cameraOrthographicSize = Mathf.Max(0.01f, cameraOrthographicSize);

            playerCollisionRadius = Mathf.Max(0.001f, playerCollisionRadius);
            sizeScale = Mathf.Max(0.01f, sizeScale);
            startRadius = Mathf.Max(0.01f, startRadius);
            deathRadius = Mathf.Clamp(deathRadius, 0.001f, startRadius);
            maxAngularSpeed = Mathf.Max(0.01f, maxAngularSpeed);
            tangentialSpeed = Mathf.Max(0.01f, tangentialSpeed);
            turnAcceleration = Mathf.Max(0.01f, turnAcceleration);
            glideDeceleration = Mathf.Max(0.01f, glideDeceleration);
            brakeDeceleration = Mathf.Max(0.01f, brakeDeceleration);
            gravity = Mathf.Max(0f, gravity);

            if (maxPlanetsPerRing < minPlanetsPerRing)
                maxPlanetsPerRing = minPlanetsPerRing;
            ringCountMultiplier = Mathf.Max(0.01f, ringCountMultiplier);
            laneSpacing = Mathf.Max(0.01f, laneSpacing);
            minGenerationRadius = Mathf.Max(0.01f, minGenerationRadius);
            recycleAbove = Mathf.Max(spawnAhead, recycleAbove);
            if (swaySpeedMax < swaySpeedMin)
                swaySpeedMax = swaySpeedMin;
            if (launchMaxFactor < launchMinFactor)
                launchMaxFactor = launchMinFactor;

            normalPlanetScore = Mathf.Max(0, normalPlanetScore);
            musouScoreThreshold = Mathf.Max(0, musouScoreThreshold);
        }
    }
}
