using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 关卡流程：选关 → 游玩 → 暂停/死亡/通关 → 重试、下一关或返回选关。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlow : MonoBehaviour
    {
        public enum FlowState
        {
            Start,
            Playing,
            Paused,
            Dying,
            Winning,
            Dead,
            Win
        }

        public GameBootstrap bootstrap;

        /// <summary>每次 StartRun 成功后触发，用于关卡开始时的教程弹窗。</summary>
        public event System.Action<LevelConfig> RunStarted;

        private SimulationClock clock;
        private PlayerController player;
        private ScoreSystem score;
        private PlanetWorld planetWorld;
        private PlayerSkills skills;
        private PlayerInputReader input;
        private LevelDatabase levelDatabase;
        private LevelConfig currentLevel;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        public FlowState State { get; private set; } = FlowState.Start;
        public bool IsSimulationAllowed => State == FlowState.Playing;
        public LevelConfig CurrentLevel => currentLevel;
        public LevelDatabase LevelDatabase => levelDatabase;

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
            levelDatabase = bootstrap.levelDatabase;
            if (levelDatabase == null)
                levelDatabase = Resources.Load<LevelDatabase>("Levels/LevelDatabase");

            if (player != null)
                player.Died += OnPlayerDied;

            currentLevel = FindLevel(0);
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
                    // 场景里放了原生开始页后，由贴纸拖动负责开始；旧键盘入口让位。
                    if (StartPageController.Instance == null &&
                        (StartPressed() || ConfirmPressed()))
                    {
                        StartRun(currentLevel);
                    }
                    break;

                case FlowState.Playing:
                    // 原生暂停弹窗存在时，Esc 由 PauseMenuController 处理。
                    if (PauseMenuController.Instance == null &&
                        Input.GetKeyDown(KeyCode.Escape))
                    {
                        PauseRun();
                    }
                    else if (currentLevel != null && !currentLevel.IsEndless &&
                             player != null &&
                             player.State.radius >= currentLevel.goalRadius)
                        WinRun();
                    break;

                case FlowState.Paused:
                    if (PauseMenuController.Instance == null &&
                        (Input.GetKeyDown(KeyCode.Escape) || ConfirmPressed()))
                    {
                        ResumeRun();
                    }
                    break;

                case FlowState.Dying:
                case FlowState.Winning:
                    break;

                case FlowState.Dead:
                    if (ResultMenuController.Instance != null)
                        break;
                    if (RetryPressed() || StartPressed() || ConfirmPressed())
                        StartRun(currentLevel);
                    else if (Input.GetKeyDown(KeyCode.Escape))
                        ReturnToStart();
                    break;

                case FlowState.Win:
                    if (ResultMenuController.Instance != null)
                        break;
                    if (Input.GetKeyDown(KeyCode.Escape))
                        ReturnToStart();
                    else if (RetryPressed())
                        StartRun(currentLevel);
                    else if (ConfirmPressed())
                        NextLevel();
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
            if (State != FlowState.Playing)
                return;

            if (OutcomeSequenceController.Instance != null)
            {
                State = FlowState.Dying;
                clock?.Pause();
                OutcomeSequenceController.Instance.PlayDeath();
            }
            else
            {
                State = FlowState.Dead;
                clock?.Pause();
            }
        }

        public void CompleteOutcome(bool victory)
        {
            if (State != FlowState.Dying && State != FlowState.Winning)
                return;
            State = victory ? FlowState.Win : FlowState.Dead;
            clock?.Pause();
        }

        public void StartRun()
        {
            StartRun(currentLevel);
        }

        public void StartRun(LevelConfig level)
        {
            if (player == null)
                return;

            currentLevel = level != null ? level : FindLevel(0);
            bool musou = currentLevel == null || currentLevel.allowMusou;
            bool bomb = currentLevel == null || currentLevel.allowBomb;
            bool hook = currentLevel == null || currentLevel.allowHook;
            skills?.SetSkillFlags(musou, bomb, hook);
            skills?.ResetSkills();
            score?.ResetScore();
            player.SetHoldRadius(false);
            player.Respawn();
            if (bootstrap != null)
                bootstrap.pressureSystem?.SetLevel(currentLevel);
            planetWorld?.SetLevel(currentLevel);
            clock?.SetSlowMotion(1f);
            clock?.Resume();
            State = FlowState.Playing;
            RunStarted?.Invoke(currentLevel);
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
            bootstrap?.pressureSystem?.SetLevel(null);
        }

        public void WinRun()
        {
            if (State != FlowState.Playing)
                return;

            bootstrap?.progress?.CompleteLevel(currentLevel);
            float reachedHeight = player != null
                ? player.State.maxRadius
                : (currentLevel != null ? currentLevel.goalRadius : 0f);
            bootstrap?.planetWorld?.MarkMarkersReached(reachedHeight);

            if (OutcomeSequenceController.Instance != null)
            {
                State = FlowState.Winning;
                clock?.Pause();
                OutcomeSequenceController.Instance.PlayWin();
            }
            else
            {
                State = FlowState.Win;
                clock?.Pause();
            }
        }

        public void NextLevel()
        {
            int index = FindLevelIndex(currentLevel);
            LevelConfig next = FindLevel(index + 1);
            if (next != null)
                StartRun(next);
            else
                ReturnToStart();
        }

        private LevelConfig FindLevel(int index)
        {
            if (levelDatabase == null || levelDatabase.levels == null ||
                levelDatabase.levels.Count == 0)
            {
                return null;
            }
            index = Mathf.Clamp(index, 0, levelDatabase.levels.Count - 1);
            for (int i = index; i < levelDatabase.levels.Count; i++)
            {
                if (levelDatabase.levels[i] != null)
                    return levelDatabase.levels[i];
            }
            for (int i = index - 1; i >= 0; i--)
            {
                if (levelDatabase.levels[i] != null)
                    return levelDatabase.levels[i];
            }
            return null;
        }

        private int FindLevelIndex(LevelConfig level)
        {
            if (levelDatabase == null || levelDatabase.levels == null || level == null)
                return -1;
            for (int i = 0; i < levelDatabase.levels.Count; i++)
            {
                if (levelDatabase.levels[i] == level)
                    return i;
            }
            return -1;
        }

        private LevelConfig NextLevelConfig()
        {
            return FindLevel(FindLevelIndex(currentLevel) + 1);
        }

        private void OnGUI()
        {
            if (State == FlowState.Playing ||
                State == FlowState.Dying ||
                State == FlowState.Winning)
            {
                return;
            }

            // 原生 UI 存在时，对应旧面板让位。
            if (State == FlowState.Start && StartPageController.Instance != null)
                return;
            if (State == FlowState.Paused && PauseMenuController.Instance != null)
                return;
            if ((State == FlowState.Dead || State == FlowState.Win) &&
                ResultMenuController.Instance != null)
            {
                return;
            }

            EnsureStyles();
            string title;
            string body;

            switch (State)
            {
                case FlowState.Start:
                    title = "跃动星球";
                    body = "选择关卡\n\nA/D 环绕黑洞 · S 炸弹 · 左键 钩锁 · H 锁定半径 · B 背景有线/无线 · F1 调试 · T 调参";
                    break;
                case FlowState.Paused:
                    title = "暂停";
                    body = "回到游戏继续观察下落与弹跳。";
                    break;
                case FlowState.Win:
                    title = "通关";
                    body = "到达目标高度！\n\n得分：" +
                           (score != null ? score.Score : 0) +
                           "    最高半径：" +
                           (player != null ? player.State.maxRadius.ToString("0.00") : "-");
                    break;
                default:
                    title = "被黑洞吞噬";
                    body = "本局得分：" + (score != null ? score.Score : 0) +
                           "    最高半径：" +
                           (player != null ? player.State.maxRadius.ToString("0.00") : "-");
                    break;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0.015f, 0.02f, 0.035f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            float width = Mathf.Min(720f, Screen.width - 32f);
            float height = State == FlowState.Start && HasLevelList() ? 500f : 300f;
            height = Mathf.Min(height, Screen.height - 32f);
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUI.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.18f, 0.16f, 0.4f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f), Texture2D.whiteTexture);
            GUI.color = oldColor;

            GUILayout.BeginArea(new Rect(
                panel.x + 42f, panel.y + 24f,
                panel.width - 84f, panel.height - 48f));
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(6f);
            GUILayout.Label(body, bodyStyle);
            GUILayout.FlexibleSpace();

            switch (State)
            {
                case FlowState.Start:
                    DrawStartButtons();
                    break;

                case FlowState.Paused:
                    if (GUILayout.Button("继续  (Esc / 空格)", buttonStyle, GUILayout.Height(48f)))
                        ResumeRun();
                    if (GUILayout.Button("返回选关", buttonStyle, GUILayout.Height(44f)))
                        ReturnToStart();
                    break;

                case FlowState.Win:
                    GUILayout.BeginHorizontal();
                    if (NextLevelConfig() != null &&
                        GUILayout.Button("下一关  (空格)", buttonStyle, GUILayout.Height(48f)))
                    {
                        NextLevel();
                    }
                    if (GUILayout.Button("重试  (R)", buttonStyle, GUILayout.Height(48f)))
                        StartRun(currentLevel);
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("返回选关  (Esc)", buttonStyle, GUILayout.Height(44f)))
                        ReturnToStart();
                    break;

                default:
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("重试  (R)", buttonStyle, GUILayout.Height(48f)))
                        StartRun(currentLevel);
                    if (GUILayout.Button("返回选关  (Esc)", buttonStyle, GUILayout.Height(48f)))
                        ReturnToStart();
                    GUILayout.EndHorizontal();
                    break;
            }

            GUILayout.EndArea();
        }

        private bool HasLevelList()
        {
            return levelDatabase != null && levelDatabase.levels != null &&
                   levelDatabase.levels.Count > 0;
        }

        private void DrawStartButtons()
        {
            if (!HasLevelList())
            {
                if (GUILayout.Button("开始  (空格 / 回车)", buttonStyle, GUILayout.Height(48f)))
                    StartRun(null);
                return;
            }

            for (int i = 0; i < levelDatabase.levels.Count; i++)
            {
                LevelConfig level = levelDatabase.levels[i];
                if (level == null)
                    continue;
                string skills = string.Empty;
                if (level.allowMusou) skills += " 无双";
                if (level.allowBomb) skills += " 炸弹";
                if (level.allowHook) skills += " 钩锁";
                string label = level.displayName;
                if (!string.IsNullOrEmpty(skills))
                    label += "   [" + skills.Trim() + "]";
                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(44f)))
                    StartRun(level);
            }
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
