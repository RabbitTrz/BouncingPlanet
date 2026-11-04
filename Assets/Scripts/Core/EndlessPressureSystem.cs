using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 无尽模式黑洞压力：时间软下限 + 最高高度追逐。
    /// 判定是一个随运行时间上升的半径 H；表现上把现有黑洞动画
    /// 放到玩家径向内侧的 H 边缘附近，形成同一动画的假扩张追逐。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EndlessPressureSystem : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private LevelConfig level;
        private LevelEndlessSettings settings;
        private SimulationClock clock;
        private PlayerController player;
        private PlanetWorld world;
        private GameFlow flow;
        private BlackHoleView blackHole;
        private bool subscribed;
        private bool active;
        private float elapsed;
        private float hazardRadius;
        private float gap;
        private float lastCullFloor = -1f;

        public bool IsActive => active;
        public float HazardRadius => hazardRadius;
        public float Gap => gap;
        public float Elapsed => elapsed;
        public float Warning01 { get; private set; }

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap != null
                ? gameBootstrap
                : GameBootstrap.Instance;
            if (bootstrap == null)
                return;

            clock = bootstrap.clock;
            player = bootstrap.player;
            world = bootstrap.planetWorld;
            flow = bootstrap.gameFlow;
            blackHole = bootstrap.worldView != null
                ? bootstrap.worldView.BlackHole
                : null;

            if (clock != null && !subscribed)
            {
                clock.Tick += OnTick;
                subscribed = true;
            }

            SetLevel(flow != null ? flow.CurrentLevel : null);
        }

        public void SetLevel(LevelConfig levelConfig)
        {
            level = levelConfig;
            settings = level != null && level.IsEndless ? level.endless : null;
            active = settings != null && settings.pressureEnabled;
            elapsed = 0f;
            lastCullFloor = -1f;
            Warning01 = 0f;

            GameConfig config = bootstrap != null ? bootstrap.gameConfig : null;
            float baseH = config != null ? Mathf.Max(0f, config.deathRadius) : 0.1f;

            if (!active)
            {
                hazardRadius = baseH;
                gap = 0f;
                player?.ClearHazardRadius();
                world?.SetPressureFloor(0f);
                blackHole?.ClearPressureVisual();
                return;
            }

            hazardRadius = baseH;
            player?.SetHazardRadius(baseH);
            world?.SetPressureFloor(baseH + Mathf.Max(0f, settings.pressureCullMargin));
            blackHole?.ClearPressureVisual();
            UpdateHazard();
        }

        private void OnDestroy()
        {
            if (clock != null && subscribed)
            {
                clock.Tick -= OnTick;
                subscribed = false;
            }
        }

        private void OnTick(float dt)
        {
            if (!active || player == null || settings == null)
                return;
            if (flow != null && flow.State != GameFlow.FlowState.Playing)
                return;
            if (player.IsDead)
                return;

            elapsed += Mathf.Max(0f, dt);
            UpdateHazard();
        }

        private void UpdateHazard()
        {
            if (player == null || settings == null)
                return;

            GameConfig config = bootstrap != null ? bootstrap.gameConfig : null;
            float baseH = config != null ? Mathf.Max(0f, config.deathRadius) : 0.1f;

            float delay = Mathf.Max(0f, settings.pressureStartDelay);
            float afterDelay = Mathf.Max(0f, elapsed - delay);
            float timeH = baseH +
                Mathf.Max(0f, settings.pressureGrowthPerSecond) * afterDelay;

            float fallbackStart = Mathf.Max(0f, settings.pressureFallbackStart);
            float fallbackEnd = Mathf.Max(0f, settings.pressureFallbackEnd);
            float ramp = Mathf.Clamp01(afterDelay /
                Mathf.Max(0.1f, settings.pressureFallbackRampSeconds));
            float fallback = Mathf.Lerp(fallbackStart, fallbackEnd, ramp);
            float chaseH = Mathf.Max(baseH,
                player.State.maxRadius - fallback);

            hazardRadius = Mathf.Max(baseH, Mathf.Max(timeH, chaseH));
            gap = player.State.radius - hazardRadius;
            player.SetHazardRadius(hazardRadius);

            if (world == null)
                return;

            float floor = hazardRadius +
                Mathf.Max(0f, settings.pressureCullMargin);
            // 半径变化很慢时不必每帧重建/清理；每 0.05 世界单位处理一次。
            if (lastCullFloor < 0f || floor > lastCullFloor + 0.05f)
            {
                world.SetPressureFloor(floor);
                lastCullFloor = floor;
            }
        }

        private void LateUpdate()
        {
            if (!active || player == null || settings == null)
                return;

            bool playing = flow == null ||
                flow.State == GameFlow.FlowState.Playing;
            if (!playing || player.IsDead)
            {
                Warning01 = 0f;
                return;
            }

            GameConfig config = bootstrap != null ? bootstrap.gameConfig : null;
            float baseH = config != null ? Mathf.Max(0f, config.deathRadius) : 0.1f;
            if (hazardRadius <= baseH + 0.001f)
            {
                // 压力还没真正抬升时保持原点黑洞，避免开局就被放大。
                Warning01 = 0f;
                blackHole?.ClearPressureVisual();
                return;
            }

            float warningDistance = Mathf.Max(0f,
                settings.pressureWarningDistance);
            Warning01 = warningDistance > 0.001f
                ? 1f - Mathf.Clamp01(Mathf.Max(0f, gap) / warningDistance)
                : 0f;

            if (blackHole == null)
                return;

            float enter = Mathf.Max(0.1f,
                settings.pressureVisualEnterDistance);
            float t = 1f - Mathf.Clamp01(Mathf.Max(0f, gap) / enter);
            float minD = Mathf.Max(0.01f,
                settings.pressureVisualDiameterMin);
            float maxD = Mathf.Max(minD,
                settings.pressureVisualDiameterMax);
            float diameter = Mathf.Lerp(minD, maxD, t);
            float ratio = Mathf.Clamp(
                settings.pressureEventHorizonRatio, 0.1f, 1f);
            float darkRadius = diameter * 0.5f * ratio;
            float centerRadius = Mathf.Max(0f, hazardRadius - darkRadius);

            PlayerState state = player.State;
            Vector2 direction = new Vector2(
                Mathf.Cos(state.WorldAngle),
                Mathf.Sin(state.WorldAngle));
            blackHole.SetLocalWorldPosition(direction * centerRadius);
            blackHole.SetVisualDiameter(diameter);
        }
    }
}
