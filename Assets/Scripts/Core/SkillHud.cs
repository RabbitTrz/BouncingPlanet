using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 占位技能 HUD：积分/无双状态、炸弹和钩锁冷却。
    /// 后续可替换成正式贴纸/图标 UI。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillHud : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private GUIStyle labelStyle;
        private GUIStyle hotStyle;
        private GUIStyle cooldownStyle;

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
        }

        private void OnGUI()
        {
            // 场景里放了原生 uGUI HUD 后，旧的文字 HUD 自动让位。
            if (GameHudView.Instance != null)
                return;

            if (bootstrap == null || bootstrap.skills == null || bootstrap.gameConfig == null)
                return;

            EnsureStyles();
            GameConfig config = bootstrap.gameConfig;
            PlayerSkills skills = bootstrap.skills;

            if (config.showScoreHud)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 300f, 62f, 280f, 80f));
                if (skills.IsMusouActive)
                {
                    GUILayout.Label($"无双 {skills.MusouRemaining:F1}s", hotStyle);
                }
                else if (skills.MusouEnabled)
                {
                    int threshold = Mathf.Max(1, config.musouScoreThreshold);
                    GUILayout.Label($"积分 {skills.MusouScore}/{threshold}", labelStyle);
                }
                else
                {
                    GUILayout.Label($"积分 {skills.MusouScore}", labelStyle);
                }
                GUILayout.EndArea();
            }

            GUILayout.BeginArea(new Rect(Screen.width - 360f, Screen.height - 128f, 340f, 110f));
            if (skills.BombEnabled)
                DrawCooldown("S", "炸弹", skills.BombCooldownRemaining, config.bombSkillCooldown);
            if (skills.HookEnabled)
                DrawCooldown("左键", "钩锁", skills.HookCooldownRemaining, config.hookCooldown);
            GUILayout.EndArea();
        }

        private void DrawCooldown(string key, string name, float remaining, float total)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{key}  {name}", labelStyle, GUILayout.Width(150f));
            if (remaining > 0.01f)
                GUILayout.Label($"{remaining:F1}s", cooldownStyle, GUILayout.Width(100f));
            else
                GUILayout.Label("就绪", hotStyle, GUILayout.Width(100f));
            GUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
                return;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleRight
            };
            labelStyle.normal.textColor = new Color(0.86f, 0.92f, 1f, 0.95f);

            hotStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold
            };
            hotStyle.normal.textColor = new Color(1f, 0.85f, 0.45f, 1f);

            cooldownStyle = new GUIStyle(labelStyle);
            cooldownStyle.normal.textColor = new Color(0.65f, 0.7f, 0.8f, 0.9f);
        }
    }
}
