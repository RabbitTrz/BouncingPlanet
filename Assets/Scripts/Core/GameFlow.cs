using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 初期 Demo 的流程：单关开始 → 游玩 → 暂停/死亡 → 重试或返回占位选关。
    /// 真正的多关卡/技能解锁留到后续版本。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlow : MonoBehaviour
    {
        public enum FlowState
        {
            Start,
            Playing,
            Paused,
            Dead
        }

        public GameBootstrap bootstrap;

        private SimulationClock clock;
        private PlayerController player;
        private ScoreSystem score;
        private PlanetWorld planetWorld;
        private PlayerSkills skills;
        private PlayerInputReader input;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        public FlowState State { get; private set; } = FlowState.Start;
        public bool IsSimulationAllowed => State == FlowState.Playing;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null)
                return;

            clock = bootstrap.clock;
            player = bootstrap.player;
            score = bootstrap.score;
            planetWorld = bootstrap.planetWorld;
            skills = bootstrap.skills;
            input = bootstrap.inputReader;

            if (player != null)
                player.Died += OnPlayerDied;

            State = FlowState.Start;
            if (clock != null)
                clock.Pause();
        }

        private void OnDestroy()
        {
            if (player != null)
                player.Died -= OnPlayerDied;
        }

        private void Update()
        {
            if (bootstrap == null)
                return;

            switch (State)
            {
                case FlowState.Start:
                    if (StartPressed() || ConfirmPressed())
                        StartRun();
                    break;

                case FlowState.Playing:
                    if (Input.GetKeyDown(KeyCode.Escape))
                        PauseRun();
                    break;

                case FlowState.Paused:
                    if (Input.GetKeyDown(KeyCode.Escape) || ConfirmPressed())
                        ResumeRun();
                    break;

                case FlowState.Dead:
                    if (RetryPressed() || StartPressed() || ConfirmPressed())
                        StartRun();
                    else if (Input.GetKeyDown(KeyCode.Escape))
                        ReturnToStart();
                    break;
            }
        }

        private bool StartPressed()
        {
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        }

        private bool ConfirmPressed()
        {
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        }

        private bool RetryPressed()
        {
            return input != null && input.RespawnPressed;
        }

        private void OnPlayerDied(PlayerState state)
        {
            State = FlowState.Dead;
            if (clock != null)
                clock.Pause();
        }

        public void StartRun()
        {
            if (player == null)
                return;

            score?.ResetScore();
            skills?.ResetSkills();
            player.SetHoldRadius(false);
            player.Respawn();
            planetWorld?.Rebuild();
            clock?.SetSlowMotion(1f);
            clock?.Resume();
            State = FlowState.Playing;
        }

        public void PauseRun()
        {
            if (State != FlowState.Playing)
                return;
            State = FlowState.Paused;
            clock?.Pause();
        }

        public void ResumeRun()
        {
            if (State != FlowState.Paused)
                return;
            State = FlowState.Playing;
            clock?.Resume();
        }

        public void ReturnToStart()
        {
            State = FlowState.Start;
            clock?.Pause();
        }

        private void OnGUI()
        {
            if (State == FlowState.Playing)
                return;

            EnsureStyles();
            string title;
            string body;

            switch (State)
            {
                case FlowState.Start:
                    title = "跃动星球";
                    body = "初期 Demo v1.1 · 单关演示\n\nA/D 环绕黑洞 · S 炸弹 · 左键 钩锁 · H 锁定半径 · F1 调试 · T 调参";
                    break;
                case FlowState.Paused:
                    title = "暂停";
                    body = "回到游戏继续观察下落与弹跳。";
                    break;
                default:
                    title = "被黑洞吞噬";
                    body = "本局得分：" + (score != null ? score.Score : 0) +
                           "    最高半径：" + (player != null ? player.State.maxRadius.ToString("0.00") : "-");
                    break;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0.015f, 0.02f, 0.035f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            float width = Mathf.Min(720f, Screen.width - 32f);
            float height = State == FlowState.Start ? 330f : 280f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.18f, 0.16f, 0.4f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f), Texture2D.whiteTexture);
            GUI.color = oldColor;

            GUILayout.BeginArea(new Rect(panel.x + 42f, panel.y + 28f, panel.width - 84f, panel.height - 56f));
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(10f);
            GUILayout.Label(body, bodyStyle);
            GUILayout.FlexibleSpace();

            if (State == FlowState.Start)
            {
                if (GUILayout.Button("开始  (空格 / 回车)", buttonStyle, GUILayout.Height(48f)))
                    StartRun();
            }
            else if (State == FlowState.Paused)
            {
                if (GUILayout.Button("继续  (Esc / 空格)", buttonStyle, GUILayout.Height(48f)))
                    ResumeRun();
                if (GUILayout.Button("返回选关", buttonStyle, GUILayout.Height(44f)))
                    ReturnToStart();
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("重试  (R)", buttonStyle, GUILayout.Height(48f)))
                    StartRun();
                if (GUILayout.Button("返回选关  (Esc)", buttonStyle, GUILayout.Height(48f)))
                    ReturnToStart();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.24f, 0.2f, 0.16f, 1f);

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            bodyStyle.normal.textColor = new Color(0.3f, 0.27f, 0.22f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(16, 16, 8, 8)
            };
        }
    }
}
