using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Initial Demo v1.0 的启动器。负责装配配置与服务；没有额外框架依赖。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [Header("Config Assets")]
        public GameConfig gameConfig;
        public FeedbackConfig feedbackConfig;
        public PlanetTypeDef[] planetTypes;

        [Header("Levels")]
        [Tooltip("留空时自动从 Resources/Levels/LevelDatabase 加载。")]
        public LevelDatabase levelDatabase;

        [Header("Animation")]
        [Tooltip("可选的玩家 Sprite 状态配置；留空使用运行时默认状态。")]
        public SpriteStateProfile playerAnimationProfile;

        [Header("Services")]
        public SimulationClock clock;
        public ObjectPoolService pool;
        public PlayerInputReader inputReader;
        public PlayerController player;
        public ScoreSystem score;
        public ImpactVfxService vfx;
        public ProceduralAudioService audioService;
        public BgmPlayer bgmPlayer;
        public Wardrobe wardrobe;
        public PlayerProgress progress;
        public FeedbackDirector feedbackDirector;
        public PlanetWorld planetWorld;
        public PlayerSkills skills;
        public CollisionSystem collisionSystem;
        public GameFlow gameFlow;
        public OutcomeSequenceController outcomeSequence;
        public EndlessPressureSystem pressureSystem;
        public HookView hookView;
        public SkillHud skillHud;
        public MusouRingView musouRingView;
        public SmashSpeedLinesView smashSpeedLinesView;
        public CameraRig cameraRig;
        public Stage1WorldView worldView;
        public Stage0Preview preview;
        public DebugOverlay debugOverlay;
        public TuningPanel tuningPanel;

        public GameEventBus Events { get; private set; }
        public RulesProfile Profile => gameConfig != null ? gameConfig.profile : RulesProfile.InitialDemoV1;

        private void Awake()
        {
            // 桌面版锁 60 帧；WebGL 的帧率最终仍由浏览器控制。
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureConfigs();
            Events = new GameEventBus();

            clock = EnsureComponent(clock);
            clock.Initialize(gameConfig);

            pool = EnsureComponent(pool);
            pool.Initialize();

            // Stage 1 起不再使用 Stage0Preview；组件保留但关闭，避免和真实相机抢控制。
            if (preview == null)
                preview = GetComponent<Stage0Preview>();
            if (preview != null)
                preview.enabled = false;

            inputReader = EnsureComponent(inputReader);
            inputReader.Initialize(gameConfig);
            player = EnsureComponent(player);
            player.Initialize(this, inputReader);

            score = EnsureComponent(score);
            score.ResetScore();

            vfx = EnsureComponent(vfx);
            vfx.Initialize(this);

            audioService = EnsureComponent(audioService);
            audioService.Initialize(this);

            bgmPlayer = EnsureComponent(bgmPlayer);
            bgmPlayer.Initialize(this);

            wardrobe = EnsureComponent(wardrobe);
            wardrobe.Initialize(this);

            progress = EnsureComponent(progress);
            progress.Initialize(this);

            feedbackDirector = EnsureComponent(feedbackDirector);
            feedbackDirector.Initialize(this);

            planetWorld = EnsureComponent(planetWorld);
            planetWorld.Initialize(this);

            skills = EnsureComponent(skills);
            skills.Initialize(this);
            player.skills = skills;

            // CollisionSystem 必须在 PlayerController 之后初始化，保证 Tick 顺序：
            // 玩家先移动并记录 previousPosition，再扫掠碰撞。
            collisionSystem = EnsureComponent(collisionSystem);
            collisionSystem.Initialize(this);

            cameraRig = EnsureComponent(cameraRig);
            cameraRig.Initialize(this);

            worldView = EnsureComponent(worldView);
            if (playerAnimationProfile != null)
                worldView.playerProfile = playerAnimationProfile;
            worldView.Initialize(this);

            gameFlow = EnsureComponent(gameFlow);
            gameFlow.Initialize(this);

            pressureSystem = EnsureComponent(pressureSystem);
            pressureSystem.Initialize(this);

            outcomeSequence = EnsureComponent(outcomeSequence);
            outcomeSequence.Initialize(this);

            hookView = EnsureComponent(hookView);
            hookView.Initialize(this);

            skillHud = EnsureComponent(skillHud);
            skillHud.bootstrap = this;

            musouRingView = EnsureComponent(musouRingView);
            musouRingView.Initialize(this);

            smashSpeedLinesView = EnsureComponent(smashSpeedLinesView);

            debugOverlay = EnsureComponent(debugOverlay);
            debugOverlay.Initialize(this);

            tuningPanel = EnsureComponent(tuningPanel);
            tuningPanel.Initialize(this);

            Application.targetFrameRate = 60;
            Debug.Log("[BouncingPlanet] Initial Demo v1.1 / Stage 3+4 + Skills bootstrapped.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private T EnsureComponent<T>(T existing) where T : Component
        {
            if (existing != null)
                return existing;
            T found = GetComponent<T>();
            return found != null ? found : gameObject.AddComponent<T>();
        }

        private void EnsureConfigs()
        {
            if (gameConfig == null)
            {
                gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                gameConfig.name = "RuntimeGameConfig";
                gameConfig.ResetToDefaults();
                gameConfig.hideFlags = HideFlags.DontSave;
                Debug.LogWarning("[BouncingPlanet] GameConfig 未指定，使用运行时默认值。运行 Tools/BouncingPlanet/Setup Initial Demo 可生成可编辑资产。");
            }

            if (feedbackConfig == null)
            {
                feedbackConfig = ScriptableObject.CreateInstance<FeedbackConfig>();
                feedbackConfig.name = "RuntimeFeedbackConfig";
                feedbackConfig.ResetToDefaults();
                feedbackConfig.hideFlags = HideFlags.DontSave;
            }

            if (clock == null)
                clock = GetComponent<SimulationClock>();
            if (clock == null)
                clock = gameObject.AddComponent<SimulationClock>();

            List<PlanetTypeDef> types = new List<PlanetTypeDef>(planetTypes ?? Array.Empty<PlanetTypeDef>());
            foreach (PlanetTypeId id in Enum.GetValues(typeof(PlanetTypeId)))
            {
                bool found = false;
                for (int i = 0; i < types.Count; i++)
                {
                    if (types[i] != null && types[i].id == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                PlanetTypeDef runtimeDef = ScriptableObject.CreateInstance<PlanetTypeDef>();
                runtimeDef.name = "Runtime_" + id;
                runtimeDef.ResetToDefaults(id);
                runtimeDef.hideFlags = HideFlags.DontSave;
                types.Add(runtimeDef);
            }

            planetTypes = types.ToArray();

            if (playerAnimationProfile == null)
            {
                playerAnimationProfile = Resources.Load<SpriteStateProfile>(
                    "Animation/PlayerAnimationProfile");
            }

            if (levelDatabase == null)
            {
                levelDatabase = Resources.Load<LevelDatabase>(
                    "Levels/LevelDatabase");
            }
        }

        public PlanetTypeDef GetPlanetType(PlanetTypeId id)
        {
            if (planetTypes == null)
                return null;

            for (int i = 0; i < planetTypes.Length; i++)
            {
                if (planetTypes[i] != null && planetTypes[i].id == id)
                    return planetTypes[i];
            }
            return null;
        }
    }
}
