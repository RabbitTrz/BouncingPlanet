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
        public Button continueButton;
        public GameObject tutorialPopup;
        public Image tutorialImage;
        public Sprite[] tutorialSprites;
        public Button tutorialContinueButton;

        private GameFlow flow;
        private GameFlow subscribedFlow;
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
            if (continueButton != null)
                continueButton.onClick.AddListener(ResumeRun);
            if (tutorialContinueButton != null)
                tutorialContinueButton.onClick.AddListener(CloseTutorial);
        }

        private void OnDestroy()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (levelSelectButton != null)
                levelSelectButton.onClick.RemoveListener(ReturnToLevelSelect);
            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartLevel);
            if (continueButton != null)
                continueButton.onClick.RemoveListener(ResumeRun);
            if (tutorialContinueButton != null)
                tutorialContinueButton.onClick.RemoveListener(CloseTutorial);
            if (subscribedFlow != null)
                subscribedFlow.RunStarted -= OnRunStarted;
            subscribedFlow = null;
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

            SubscribeFlow();

            bool paused = flow.State == GameFlow.FlowState.Paused;
            bool tutorialOpen = tutorialPopup != null &&
                                tutorialPopup.activeSelf;
            bool settingsActive = settingsPopup != null &&
                                  settingsPopup.activeSelf;
            if (ownsSettings && !settingsActive)
                ownsSettings = false;
            bool settingsOpen = ownsSettings && settingsActive;
            if (pausePopup != null)
            {
                if (settingsOpen || tutorialOpen)
                {
                    // 设置/教程弹窗优先，暂停弹窗让位。
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

            if (tutorialOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    CloseTutorial();
                return;
            }

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

        /// <summary>HUD 暂停按钮调用；只在游戏进行中生效。</summary>
        public void OpenPause()
        {
            if (flow == null)
                flow = GameBootstrap.Instance?.gameFlow;
            if (flow != null && flow.State == GameFlow.FlowState.Playing)
                flow.PauseRun();
        }

        private void ResumeRun()
        {
            if (flow == null)
                flow = GameBootstrap.Instance?.gameFlow;
            flow?.ResumeRun();
        }

        private void SubscribeFlow()
        {
            if (flow == subscribedFlow)
                return;
            if (subscribedFlow != null)
                subscribedFlow.RunStarted -= OnRunStarted;
            subscribedFlow = flow;
            if (subscribedFlow != null)
                subscribedFlow.RunStarted += OnRunStarted;
        }

        private void OnRunStarted(LevelConfig level)
        {
            if (tutorialPopup == null || tutorialImage == null ||
                tutorialSprites == null || tutorialSprites.Length == 0)
            {
                return;
            }

            Sprite sprite = GetTutorialSprite(level);
            if (sprite == null)
                return;

            tutorialImage.sprite = sprite;
            tutorialPopup.SetActive(true);
            subscribedFlow?.PauseRun();
        }

        private Sprite GetTutorialSprite(LevelConfig level)
        {
            if (level == null)
                return null;
            int index = level.IsEndless
                ? tutorialSprites.Length - 1
                : level.order - 1;
            if (index < 0 || index >= tutorialSprites.Length)
                return null;
            return tutorialSprites[index];
        }

        private void CloseTutorial()
        {
            if (tutorialPopup == null || !tutorialPopup.activeSelf)
                return;

            PopupScaleIn animation =
                tutorialPopup.GetComponentInChildren<PopupScaleIn>(true);
            if (animation != null)
                animation.Close();
            else
                tutorialPopup.SetActive(false);

            if (flow != null && flow.State == GameFlow.FlowState.Paused)
                flow.ResumeRun();
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
