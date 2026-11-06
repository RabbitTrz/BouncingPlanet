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

        [LabelText("炸弹技能键")]
        public KeyCode bombSkillKey = KeyCode.S;

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

        [Header("背景 Background")]
        [Tunable("Background", 0f, 1f, 1f, "启用循环背景")]
        [Tooltip("关闭后显示相机纯色背景，不再绘制星空平铺图。")]
        public bool backgroundEnabled = true;

        [Tunable("Background", 0f, 1f, 1f, "使用带线版本")]
        [Tooltip("运行时按 B 也可切换；带线版本带水平轨道线。")]
        public bool backgroundUseLineVariant = false;

        [Tunable("Background", 0f, 0.95f, 0.01f, "背景视差强度")]
        [Tooltip("0 = 背景完全跟随相机不动；数值越大，背景相对相机反向滚动越明显。")]
        public float backgroundParallax = 0.35f;

        [Tunable("Background", 1f, 6f, 0.05f, "背景平铺世界尺寸")]
        [Tooltip("一张 512 背景图在世界上覆盖的边长；越小星点越密。")]
        public float backgroundTileSize = 2.4f;

        [Header("黑洞 Black Hole")]
        [Tunable("Black Hole", 0.1f, 2f, 0.01f, "视觉直径")]
        [Tooltip("带纸边黑洞的外径；中央黑色区域会随比例变化。")]
        public float blackHoleVisualDiameter = 0.52f;

        [Tunable("Black Hole", 1f, 30f, 1f, "动画帧率")]
        [Tooltip("黑洞 7 帧循环动画的播放帧率。")]
        public float blackHoleAnimationFps = 10f;

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

        [Header("玩家表现 Player Visual")]
        [Tunable("Player", 0f, 0.05f, 0.001f, "左右偏移（世界单位）")]
        [Tooltip("纯视觉左右偏移；0.015 在 360 PPU 下约 5.4 像素。")]
        public float playerLateralOffset = 0.015f;

        [Tunable("Player", 0f, 30f, 0.5f, "转向倾斜角度")]
        public float playerBankAngle = 8f;

        [Tunable("Player", 0.01f, 0.5f, 0.01f, "转向平滑时间")]
        [Tooltip("越小越紧跟角速度；越大回正越软。")]
        public float playerMotionSmoothing = 0.06f;

        [Header("上升拖尾 Ascend Trail")]
        [Tunable("Player", 0f, 1f, 1f, "启用上升拖尾")]
        public bool playerTrailEnabled = true;

        [Tunable("Player", 0f, 5f, 0.05f, "拖尾最低上升速度")]
        public float playerTrailMinAscendSpeed = 0.3f;

        [Tunable("Player", 0.05f, 1f, 0.01f, "拖尾持续时间")]
        public float playerTrailTime = 0.28f;

        [Tunable("Player", 0.001f, 0.1f, 0.001f, "拖尾宽度")]
        public float playerTrailWidth = 0.014f;

        [LabelText("普通拖尾颜色")]
        public Color playerTrailColor = new Color(0.65f, 0.85f, 1f, 0.85f);

        [LabelText("无双拖尾颜色")]
        public Color musouTrailColor = new Color(1f, 0.85f, 0.25f, 0.95f);

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
        [Tunable("World", 0f, 1f, 1f, "显示星环线")]
        [Tooltip("关闭后不绘制每条轨道上的参考圆环线，只保留星球。")]
        public bool showRingLines = false;

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

        [Tunable("Collision", 0f, 30f, 1f, "下降反弹后撞碎免疫帧")]
        [Tooltip("下降撞球弹起后，给该星球几帧免疫，避免下一 tick 被误判为上升撞碎。")]
        public int bounceSmashGraceFrames = 6;

        [Header("碰撞横向冲量 Collision Lateral")]
        [Tunable("Collision", 0f, 1f, 1f, "启用横向冲量")]
        [Tooltip("只对普通星（含闪亮星）和冰冰星生效；云朵、炸弹、无双不生效。")]
        public bool collisionLateralEnabled = true;

        [Tunable("Collision", 0f, 2f, 0.05f, "横向冲量系数")]
        [Tooltip("按碰撞法线的切向分量和总速度计算横向冲量。")]
        public float collisionLateralFactor = 1.2f;

        [Tunable("Collision", 0f, 3f, 0.05f, "横向冲量最大速度")]
        [Tooltip("单次碰撞最多增加的切向速度；最终仍受玩家正常切向上限约束。")]
        public float collisionLateralMaxSpeed = 1.2f;

        [Header("普通撞碎 Normal Smash")]
        [Tunable("Smash", 0f, 1f, 1f, "启用普通撞碎")]
        [Tooltip("上升径向速度达到阈值时，撞碎碰到的任何星球（含云朵/炸弹）。")]
        public bool normalSmashEnabled = true;

        [Tunable("Smash", 0.5f, 10f, 0.1f, "撞碎速度阈值")]
        public float normalSmashSpeedThreshold = 2.0f;

        [Header("撞碎速度线 Smash Speed Lines")]
        [Tunable("Smash", 0f, 1f, 1f, "启用撞碎速度线")]
        [Tooltip("达到普通撞碎速度阈值时，在屏幕两侧显示向下滑动的白线。")]
        public bool smashSpeedLinesEnabled = true;

        [Tunable("Smash", 4f, 40f, 1f, "速度线数量")]
        public int smashSpeedLineCount = 18;

        [Tunable("Smash", 0.05f, 3f, 0.05f, "速度线滑动速度")]
        [Tooltip("以屏幕高度/秒为单位。")]
        public float smashSpeedLineSpeed = 0.9f;

        [Tunable("Smash", 10f, 400f, 1f, "速度线最短长度")]
        public float smashSpeedLineMinLength = 48f;

        [Tunable("Smash", 20f, 600f, 1f, "速度线最长长度")]
        public float smashSpeedLineMaxLength = 150f;

        [Tunable("Smash", 1f, 8f, 0.5f, "速度线宽度")]
        public float smashSpeedLineWidth = 2f;

        [Tunable("Smash", 0f, 1f, 0.01f, "速度线透明度")]
        public float smashSpeedLineAlpha = 0.55f;

        [Tunable("Smash", 0.02f, 0.35f, 0.01f, "速度线侧边范围")]
        [Tooltip("屏幕左右两侧各占屏幕宽度的比例。")]
        public float smashSpeedLineSideBand = 0.16f;

        [Header("分数 Score")]
        [Tunable("Score", 0f, 100f, 1f, "碰撞得分")]
        [Tooltip("每次成功碰撞的基础分；不同类型再乘 scoreMultiplier。")]
        public int normalPlanetScore = 10;

        [Tunable("Score", 0f, 10000f, 10f, "无双积分阈值")]
        [Tooltip("积分达到该值消耗并进入无双。")]
        public int musouScoreThreshold = 150;

        [Tunable("Score", 0f, 100f, 1f, "撞碎得分")]
        [Tooltip("普通上升撞碎的得分；无双撞碎不加分。")]
        public int smashScore = 15;

        [Tunable("Score", 0f, 1f, 1f, "显示分数 HUD")]
        public bool showScoreHud = true;

        [Header("技能 Skills")]
        [Tunable("Musou", 0.5f, 10f, 0.1f, "无双持续秒")]
        public float musouDuration = 4f;

        [Tunable("Musou", 0f, 5f, 0.05f, "每次撞碎延长")]
        [Tooltip("无双每次撞碎的延长时间；剩余时长最多封顶到一次完整无双持续时间。")]
        public float musouExtension = 0.75f;

        [Tunable("Musou", 0.1f, 10f, 0.1f, "无双上升速度")]
        [Tooltip("占位值；最终由测试决定。")]
        public float musouSpeed = 2.4f;

        [Tunable("Musou", 0f, 1f, 1f, "显示无双环")]
        public bool showMusouRing = true;

        [Tunable("Musou", 0.02f, 0.3f, 0.005f, "无双环半径")]
        public float musouRingRadius = 0.085f;

        [Tunable("Musou", 0.001f, 0.03f, 0.001f, "无双环线宽")]
        public float musouRingWidth = 0.007f;

        [Tunable("Musou", 0f, 1f, 0.01f, "无双环底色透明度")]
        public float musouRingBaseAlpha = 0.15f;

        [LabelText("无双环充能色")]
        public Color musouRingChargeColor = new Color(0.35f, 0.82f, 1f, 1f);

        [LabelText("无双环激活色")]
        public Color musouRingActiveColor = new Color(1f, 0.82f, 0.32f, 1f);

        [Tunable("BombSkill", 0.5f, 20f, 0.1f, "炸弹技能冷却")]
        public float bombSkillCooldown = 6f;

        [Tunable("BombSkill", 0f, 1f, 0.01f, "炸弹生成下移")]
        public float bombSkillSpawnBelow = 0.2f;

        [Tunable("BombSkill", 0.1f, 5f, 0.05f, "炸弹爆炸半径")]
        public float bombSkillExplosionRadius = 0.9f;

        [Tunable("BombSkill", 0f, 10f, 0.05f, "炸弹推力")]
        public float bombSkillPushSpeed = 3.3f;

        [Tunable("Hook", 0.5f, 20f, 0.1f, "钩锁冷却")]
        public float hookCooldown = 4f;

        [Tunable("Hook", 0.1f, 3f, 0.05f, "钩锁范围")]
        [Tooltip("占位值；需求表待定。")]
        public float hookRange = 0.75f;

        [Tunable("Hook", 0f, 40f, 0.5f, "钩锁加速度")]
        public float hookAcceleration = 9f;

        [Tunable("Hook", 0.1f, 30f, 0.05f, "钩锁限速")]
        public float hookMaxSpeed = 1.9f;

        [Tunable("Hook", 1f, 10f, 0.1f, "钩锁断开距离")]
        public float hookBreakDistance = 3f;

        [Tunable("Hook", 0f, 0.5f, 0.01f, "钩锁松脱距离")]
        public float hookLooseDistance = 0.08f;

        [Tunable("Hook", 0.1f, 5f, 0.1f, "钩锁重击速度阈值")]
        public float hookHeavySpeed = 1.7f;

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
            bombSkillKey = KeyCode.S;

            simulationHz = 120;
            maxStepsPerFrame = 8;

            cameraPixelsPerUnit = 360f;
            cameraOrthographicSize = 1f;
            backgroundColor = new Color(0.02f, 0.024f, 0.04f, 1f);
            backgroundEnabled = true;
            backgroundUseLineVariant = false;
            backgroundParallax = 0.35f;
            backgroundTileSize = 2.4f;
            blackHoleVisualDiameter = 0.52f;
            blackHoleAnimationFps = 10f;

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

            playerLateralOffset = 0.015f;
            playerBankAngle = 8f;
            playerMotionSmoothing = 0.06f;
            playerTrailEnabled = true;
            playerTrailMinAscendSpeed = 0.3f;
            playerTrailTime = 0.28f;
            playerTrailWidth = 0.014f;
            playerTrailColor = new Color(0.65f, 0.85f, 1f, 0.85f);
            musouTrailColor = new Color(1f, 0.85f, 0.25f, 0.95f);

            bounceGain = 1.75f;
            baseImpulse = 1.00f;
            jumpSpeed = 1.60f;
            launchMinFactor = 0.82f;
            launchMaxFactor = 1.28f;
            useLaunchMax = false;

            showRingLines = false;
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
            bounceSmashGraceFrames = 6;
            collisionLateralEnabled = true;
            collisionLateralFactor = 1.2f;
            collisionLateralMaxSpeed = 1.2f;
            normalSmashEnabled = true;
            normalSmashSpeedThreshold = 2.0f;
            smashSpeedLinesEnabled = true;
            smashSpeedLineCount = 18;
            smashSpeedLineSpeed = 0.9f;
            smashSpeedLineMinLength = 48f;
            smashSpeedLineMaxLength = 150f;
            smashSpeedLineWidth = 2f;
            smashSpeedLineAlpha = 0.55f;
            smashSpeedLineSideBand = 0.16f;

            normalPlanetScore = 10;
            musouScoreThreshold = 150;
            smashScore = 15;
            showScoreHud = true;

            musouDuration = 4f;
            musouExtension = 0.75f;
            musouSpeed = 2.4f;
            showMusouRing = true;
            musouRingRadius = 0.085f;
            musouRingWidth = 0.007f;
            musouRingBaseAlpha = 0.15f;
            musouRingChargeColor = new Color(0.35f, 0.82f, 1f, 1f);
            musouRingActiveColor = new Color(1f, 0.82f, 0.32f, 1f);

            bombSkillCooldown = 6f;
            bombSkillSpawnBelow = 0.2f;
            bombSkillExplosionRadius = 0.9f;
            bombSkillPushSpeed = 3.3f;

            hookCooldown = 4f;
            hookRange = 0.75f;
            hookAcceleration = 9f;
            hookMaxSpeed = 1.9f;
            hookBreakDistance = 3f;
            hookLooseDistance = 0.08f;
            hookHeavySpeed = 1.7f;
        }

        private void OnValidate()
        {
            simulationHz = Mathf.Clamp(simulationHz, 1, 1000);
            maxStepsPerFrame = Mathf.Clamp(maxStepsPerFrame, 1, 64);

            cameraPixelsPerUnit = Mathf.Max(1f, cameraPixelsPerUnit);
            cameraOrthographicSize = Mathf.Max(0.01f, cameraOrthographicSize);
            backgroundParallax = Mathf.Clamp(backgroundParallax, 0f, 0.95f);
            backgroundTileSize = Mathf.Max(0.1f, backgroundTileSize);
            blackHoleVisualDiameter = Mathf.Max(0.01f, blackHoleVisualDiameter);
            blackHoleAnimationFps = Mathf.Max(0.1f, blackHoleAnimationFps);

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

            playerLateralOffset = Mathf.Max(0f, playerLateralOffset);
            playerBankAngle = Mathf.Max(0f, playerBankAngle);
            playerMotionSmoothing = Mathf.Max(0.01f, playerMotionSmoothing);
            playerTrailMinAscendSpeed = Mathf.Max(0f, playerTrailMinAscendSpeed);
            playerTrailTime = Mathf.Max(0.01f, playerTrailTime);
            playerTrailWidth = Mathf.Max(0.0001f, playerTrailWidth);

            bounceSmashGraceFrames = Mathf.Clamp(bounceSmashGraceFrames, 0, 60);
            collisionLateralFactor = Mathf.Max(0f, collisionLateralFactor);
            collisionLateralMaxSpeed = Mathf.Max(0f, collisionLateralMaxSpeed);
            normalSmashSpeedThreshold = Mathf.Max(0f, normalSmashSpeedThreshold);
            smashSpeedLineCount = Mathf.Clamp(smashSpeedLineCount, 4, 40);
            smashSpeedLineSpeed = Mathf.Max(0f, smashSpeedLineSpeed);
            smashSpeedLineMinLength = Mathf.Max(1f, smashSpeedLineMinLength);
            smashSpeedLineMaxLength = Mathf.Max(
                smashSpeedLineMinLength, smashSpeedLineMaxLength);
            smashSpeedLineWidth = Mathf.Max(0.5f, smashSpeedLineWidth);
            smashSpeedLineAlpha = Mathf.Clamp01(smashSpeedLineAlpha);
            smashSpeedLineSideBand = Mathf.Clamp(smashSpeedLineSideBand, 0.02f, 0.35f);

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
            smashScore = Mathf.Max(0, smashScore);

            musouRingRadius = Mathf.Max(0.001f, musouRingRadius);
            musouRingWidth = Mathf.Max(0.0001f, musouRingWidth);
            musouRingBaseAlpha = Mathf.Clamp01(musouRingBaseAlpha);
        }
    }
}
