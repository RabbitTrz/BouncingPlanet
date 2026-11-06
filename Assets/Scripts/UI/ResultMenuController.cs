using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>胜利/失败结算：只显示本局最高高度，不显示分数。</summary>
    [DisallowMultipleComponent]
    public sealed class ResultMenuController : MonoBehaviour
    {
        public static ResultMenuController Instance { get; private set; }

        public GameBootstrap bootstrap;
        public GameObject resultPage;
        public Image background;
        public Sprite winSprite;
        public Sprite loseSprite;
        public SpriteNumberDisplay heightDisplay;
        public Button levelSelectButton;
        public Button restartButton;

        private GameFlow flow;
        private bool resultShown;

        private void Awake()
        {
            Instance = this;
            bootstrap = GameBootstrap.Instance;
            flow = bootstrap != null ? bootstrap.gameFlow : null;
            if (levelSelectButton != null)
                levelSelectButton.onClick.AddListener(ReturnToLevelSelect);
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartLevel);
            if (resultPage != null)
                resultPage.SetActive(false);
        }

        private void OnDestroy()
        {
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

            bool show = flow.State == GameFlow.FlowState.Dead ||
                        flow.State == GameFlow.FlowState.Win;
            if (resultPage != null && resultPage.activeSelf != show)
                resultPage.SetActive(show);
            if (!show)
            {
                resultShown = false;
                return;
            }

            if (!resultShown)
            {
                resultShown = true;
                if (flow.State == GameFlow.FlowState.Win)
                    bootstrap?.audioService?.PlayWinResult();
                else
                    bootstrap?.audioService?.PlayLoseResult();
            }

            if (background != null)
            {
                background.sprite = flow.State == GameFlow.FlowState.Win
                    ? winSprite
                    : loseSprite;
            }
            if (heightDisplay != null && bootstrap.player != null)
            {
                heightDisplay.SetValue(Mathf.RoundToInt(
                    bootstrap.player.State.maxRadius));
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                ReturnToLevelSelect();
        }

        private void ReturnToLevelSelect()
        {
            if (flow == null)
                return;
            flow.ReturnToStart();
            StartPageController.Instance?.OpenLevelSelect(true);
        }

        private void RestartLevel()
        {
            if (flow == null)
                return;
            flow.StartRun(flow.CurrentLevel);
        }
    }
}
