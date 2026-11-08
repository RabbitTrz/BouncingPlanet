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
        public SpriteNumberDisplay heightCurrent;
        public SpriteNumberDisplay scoreDisplay;
        public SkillIconView musouSkill;
        public SkillIconView bombSkill;
        public SkillIconView hookSkill;
        public Image pressureWarning;
        public Button pauseButton;

        [Tooltip("高度显示倍率；1 表示显示整数世界半径，10 表示显示一位小数。")]
        public float heightDisplayScale = 1f;

        [Tooltip("无尽压力警告在屏幕底部升起的最大高度（UI 像素）。")]
        public float pressureWarningMaxHeight = 150f;

        [Range(0f, 1f)]
        public float pressureWarningMaxAlpha = 0.55f;

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
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OpenPauseMenu);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OpenPauseMenu);
            if (Instance == this)
                Instance = null;
        }

        private void OpenPauseMenu()
        {
            PauseMenuController.Instance?.OpenPause();
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
                UpdatePressureWarning(0f);
                return;
            }

            UpdatePressureWarning();

            PlayerState state = bootstrap.player.State;
            float displayScale = Mathf.Max(0.01f, heightDisplayScale);
            if (heightRecord != null)
            {
                int record = Mathf.RoundToInt(state.maxRadius * displayScale);
                heightRecord.SetValue(record);
            }

            if (heightCurrent != null)
            {
                int current = Mathf.RoundToInt(state.radius * displayScale);
                heightCurrent.SetValue(current);
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

        private void UpdatePressureWarning()
        {
            float warning = 0f;
            if (bootstrap.pressureSystem != null &&
                bootstrap.pressureSystem.IsActive)
            {
                warning = bootstrap.pressureSystem.Warning01;
            }
            UpdatePressureWarning(warning);
        }

        private void UpdatePressureWarning(float warning)
        {
            if (pressureWarning == null)
                return;

            warning = Mathf.Clamp01(warning);
            Color color = pressureWarning.color;
            color.a = warning * pressureWarningMaxAlpha;
            pressureWarning.color = color;
            pressureWarning.enabled = warning > 0.001f;
            if (!pressureWarning.enabled)
                return;

            RectTransform rect = pressureWarning.rectTransform;
            Vector2 size = rect.sizeDelta;
            size.y = Mathf.Lerp(24f, Mathf.Max(24f, pressureWarningMaxHeight),
                warning);
            rect.sizeDelta = size;
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
