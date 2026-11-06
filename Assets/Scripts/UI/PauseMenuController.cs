using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>游戏内暂停弹窗，统一用 Esc 打开/继续。</summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        public GameBootstrap bootstrap;
        public GameObject pausePopup;
        public GameObject settingsPopup;
        public Button settingsButton;
        public Button levelSelectButton;
        public Button restartButton;

        private GameFlow flow;
        private bool ownsSettings;

        private void Awake()
        {
            Instance = this;
            bootstrap = GameBootstrap.Instance;
            flow = bootstrap != null ? bootstrap.gameFlow : null;
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (levelSelectButton != null)
                levelSelectButton.onClick.AddListener(ReturnToLevelSelect);
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartLevel);
        }

        private void OnDestroy()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (levelSelectButton != null)
                levelSelectButton.onClick.RemoveListener(ReturnToLevelSelect);
            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartLevel);
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
            if (flow == null && bootstrap != null)
                flow = bootstrap.gameFlow;
            if (flow == null)
                return;

            bool paused = flow.State == GameFlow.FlowState.Paused;
            bool settingsActive = settingsPopup != null &&
                                  settingsPopup.activeSelf;
            if (ownsSettings && !settingsActive)
                ownsSettings = false;
            bool settingsOpen = ownsSettings && settingsActive;
            if (pausePopup != null)
            {
                if (settingsOpen)
                {
                    // 设置弹窗在层级上早于暂停弹窗，打开设置时先让暂停弹窗退场。
                    if (pausePopup.activeSelf)
                        pausePopup.SetActive(false);
                }
                else if (paused && !pausePopup.activeSelf)
                {
                    pausePopup.SetActive(true);
                }
                else if (!paused && pausePopup.activeSelf)
                {
                    HidePausePopup();
                }
            }

            if (!paused && settingsOpen)
                HideSettings();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (flow.State == GameFlow.FlowState.Playing)
            {
                flow.PauseRun();
            }
            else if (flow.State == GameFlow.FlowState.Paused)
            {
                if (settingsOpen)
                    HideSettings();
                else
                    flow.ResumeRun();
            }
        }

        private void OpenSettings()
        {
            ownsSettings = true;
            if (pausePopup != null)
                pausePopup.SetActive(false);
            if (settingsPopup != null)
                settingsPopup.SetActive(true);
        }

        private void ReturnToLevelSelect()
        {
            HideSettings();
            HidePausePopup();
            flow?.ReturnToStart();
            StartPageController.Instance?.OpenLevelSelect(true);
        }

        private void RestartLevel()
        {
            HideSettings();
            HidePausePopup();
            if (flow != null)
                flow.StartRun(flow.CurrentLevel);
        }

        private void HidePausePopup()
        {
            if (pausePopup == null || !pausePopup.activeSelf)
                return;
            PopupScaleIn animation =
                pausePopup.GetComponentInChildren<PopupScaleIn>(true);
            if (animation != null)
                animation.Close();
            else
                pausePopup.SetActive(false);
        }

        private void HideSettings()
        {
            ownsSettings = false;
            if (settingsPopup == null)
                return;
            if (StartPageController.Instance != null)
            {
                StartPageController.Instance.CloseSettingsPopup();
                return;
            }
            PopupScaleIn animation =
                settingsPopup.GetComponentInChildren<PopupScaleIn>(true);
            if (animation != null)
                animation.Close();
            else
                settingsPopup.SetActive(false);
        }
    }
}
