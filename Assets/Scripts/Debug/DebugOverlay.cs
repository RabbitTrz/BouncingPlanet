using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// F1 调试叠层：只读展示当前配置与仿真状态，不参与玩法。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugOverlay : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        [SerializeField] private bool visibleOnStart = true;

        private bool visible;
        private float smoothedFps = 60f;
        private GUIStyle labelStyle;
        private GUIStyle hintStyle;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap;
            visible = visibleOnStart;
        }

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
        }

        private void Update()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;

            if (Input.GetKeyDown(KeyCode.F1))
                visible = !visible;

            if (bootstrap != null && bootstrap.clock != null)
            {
                bool allowDebugClock =
                    bootstrap.gameFlow == null ||
                    bootstrap.gameFlow.State == GameFlow.FlowState.Playing ||
                    bootstrap.gameFlow.State == GameFlow.FlowState.Paused;

                if (allowDebugClock && Input.GetKeyDown(KeyCode.P))
                    bootstrap.clock.TogglePause();
                if (allowDebugClock && Input.GetKeyDown(KeyCode.O) && bootstrap.clock.IsPaused)
                    bootstrap.clock.StepOnce();
            }

            float instantFps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            smoothedFps = Mathf.Lerp(smoothedFps, instantFps, 0.08f);
        }

        private void OnGUI()
        {
            if (!visible || bootstrap == null)
                return;

            EnsureStyles();
            GameConfig config = bootstrap.gameConfig;
            SimulationClock clock = bootstrap.clock;

            GUILayout.BeginArea(new Rect(12f, 12f, 500f, 460f), GUI.skin.box);
            GUILayout.Label("跃动星球 · Initial Demo v1.1 · Stage 3/4", hintStyle);
            GUILayout.Space(4f);

            GUILayout.Label($"FPS {smoothedFps:F0}   Screen {Screen.width}x{Screen.height}", labelStyle);

            if (bootstrap.player != null)
            {
                PlayerState state = bootstrap.player.State;
                GUILayout.Label(
                    $"Player r={state.radius:F3}  θ={state.theta:F2}  vr={state.radialVelocity:F2}  " +
                    $"ω={state.angularVelocity:F2}",
                    labelStyle);
                GUILayout.Label(
                    $"maxR={state.maxRadius:F3}  dead={state.dead}  deaths={state.deaths}  " +
                    $"hold={bootstrap.player.IsHoldingRadius}",
                    labelStyle);
            }
            else
            {
                GUILayout.Label("Player: missing", labelStyle);
            }

            if (bootstrap.inputReader != null && config != null)
            {
                GUILayout.Label(
                    $"Input h={bootstrap.inputReader.Horizontal:F0}  " +
                    $"Left={config.moveLeftKey}/{config.moveLeftAltKey}  " +
                    $"Right={config.moveRightKey}/{config.moveRightAltKey}",
                    labelStyle);
            }

            GUILayout.Label(
                $"Score={bootstrap.score?.Score ?? 0}  Planets={bootstrap.planetWorld?.AliveCount ?? 0}  " +
                $"Shake={bootstrap.feedbackDirector?.Shake ?? 0f:F2}  " +
                $"Punch={bootstrap.feedbackDirector?.Punch ?? 0f:F2}  " +
                $"Squash={bootstrap.feedbackDirector?.Squash ?? 0f:F2}  " +
                $"Flow={bootstrap.gameFlow?.State}",
                labelStyle);

            if (clock != null)
            {
                GUILayout.Label(
                    $"Sim {clock.FixedDelta * 1000f:F1}ms ({1f / Mathf.Max(0.0001f, clock.FixedDelta):F0}Hz)  " +
                    $"t={clock.SimulationTime:F1}s  steps={clock.StepsLastFrame}",
                    labelStyle);
                GUILayout.Label(
                    $"Paused={clock.IsPaused}  Slow={clock.SlowMotionScale:F2}  Hitstop={clock.HitstopRemaining:F2}s",
                    labelStyle);
            }
            else
            {
                GUILayout.Label("Clock: missing", labelStyle);
            }

            if (config != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"Profile: {config.profile}", hintStyle);
                GUILayout.Label(
                    $"G={config.gravity:F2}  E={config.bounceGain:F2}  J={config.baseImpulse:F2}  " +
                    $"JUMP={config.jumpSpeed:F2}",
                    labelStyle);
                string launchMaxText = float.IsPositiveInfinity(config.LaunchMax)
                    ? "∞"
                    : config.LaunchMax.ToString("F3");
                GUILayout.Label(
                    $"Launch clamp [{config.LaunchMin:F3}, {launchMaxText}]  " +
                    $"AscendReturn={config.ascendBlockFactor:F2}",
                    labelStyle);
                GUILayout.Label(
                    $"Player r={config.EffectivePlayerRadius:F4}  diameter={config.PlayerDiameterWorld:F4} " +
                    $"({config.PlayerScreenDiameter:F1}px)",
                    labelStyle);
                GUILayout.Label(
                    $"StartR={config.startRadius:F2}  DeathR={config.deathRadius:F2}  " +
                    $"Lane={config.laneSpacing:F2}  Ring@start={config.GetRingCount(config.startRadius)}",
                    labelStyle);
                int startCount = config.GetRingCount(config.startRadius);
                GUILayout.Label(
                    $"Sway @start={config.GetSwayAmplitude(startCount):F3} rad  " +
                    $"Camera ortho={config.cameraOrthographicSize:F2}  PPU={config.cameraPixelsPerUnit:F0}",
                    labelStyle);
                GUILayout.Label(
                    $"Score: normal={config.normalPlanetScore}  threshold={config.musouScoreThreshold}  " +
                    $"HUD={config.showScoreHud}",
                    labelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Click Game view first. F1 hide  T tuning  P pause  O step  R retry  H hold  S bomb  LMB hook",
                hintStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
                return;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = false
            };
            labelStyle.normal.textColor = new Color(0.82f, 0.90f, 1f, 1f);

            hintStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold
            };
            hintStyle.normal.textColor = new Color(1f, 0.86f, 0.58f, 1f);
        }
    }
}
