using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 游戏内 HUD：高度记录、分数、技能图标/冷却。
    /// 由场景里的 Canvas 预制体挂载；旧的 OnGUI HUD 会在它存在时自动隐藏。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        public static GameHudView Instance { get; private set; }

        public CanvasGroup canvasGroup;
        public SpriteNumberDisplay heightRecord;
        public SpriteNumberDisplay scoreDisplay;
        public SkillIconView musouSkill;
        public SkillIconView bombSkill;
        public SkillIconView hookSkill;

        [Tooltip("高度显示倍率；1 表示显示整数世界半径，10 表示显示一位小数。")]
        public float heightDisplayScale = 1f;

        private GameBootstrap bootstrap;
        private float previousBombCooldown;
        private float previousHookCooldown;
        private bool cooldownInitialized;

        private void Awake()
        {
            Instance = this;
            bootstrap = GameBootstrap.Instance;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.gameConfig == null ||
                bootstrap.player == null || bootstrap.skills == null)
            {
                return;
            }

            GameFlow flow = bootstrap.gameFlow;
            bool visible = flow == null || flow.State == GameFlow.FlowState.Playing;
            SetVisible(visible);
            if (!visible)
            {
                // 重新进入游戏时重新取当前冷却，避免把暂停期间的变化当成“刚完成”。
                cooldownInitialized = false;
                return;
            }

            PlayerState state = bootstrap.player.State;
            if (heightRecord != null)
            {
                int height = Mathf.RoundToInt(
                    state.maxRadius * Mathf.Max(0.01f, heightDisplayScale));
                heightRecord.SetValue(height);
            }

            if (scoreDisplay != null && bootstrap.score != null)
                scoreDisplay.SetValue(bootstrap.score.Score);

            PlayerSkills skills = bootstrap.skills;
            GameConfig config = bootstrap.gameConfig;

            if (!cooldownInitialized)
            {
                previousBombCooldown = skills.BombCooldownRemaining;
                previousHookCooldown = skills.HookCooldownRemaining;
                cooldownInitialized = true;
            }
            else
            {
                const float ReadyEpsilon = 0.001f;
                bool bombReady = skills.BombEnabled &&
                    previousBombCooldown > ReadyEpsilon &&
                    skills.BombCooldownRemaining <= ReadyEpsilon;
                bool hookReady = skills.HookEnabled &&
                    previousHookCooldown > ReadyEpsilon &&
                    skills.HookCooldownRemaining <= ReadyEpsilon;

                if (bombReady || hookReady)
                    bootstrap.audioService?.PlayCooldownReady();
                if (bombReady)
                    bombSkill?.PlayReadyBurst();
                if (hookReady)
                    hookSkill?.PlayReadyBurst();
            }

            previousBombCooldown = skills.BombCooldownRemaining;
            previousHookCooldown = skills.HookCooldownRemaining;

            if (musouSkill != null)
            {
                musouSkill.Set(
                    skills.MusouEnabled,
                    skills.IsMusouActive ? skills.MusouRemaining : 0f,
                    config.musouDuration,
                    skills.IsMusouActive);
            }

            if (bombSkill != null)
            {
                bombSkill.Set(
                    skills.BombEnabled,
                    skills.BombCooldownRemaining,
                    config.bombSkillCooldown,
                    false);
            }

            if (hookSkill != null)
            {
                hookSkill.Set(
                    skills.HookEnabled,
                    skills.HookCooldownRemaining,
                    config.hookCooldown,
                    false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}
