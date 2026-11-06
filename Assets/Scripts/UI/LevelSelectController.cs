using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>选关页：拖玩家贴纸到关卡星球后启动对应关卡。</summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectController : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        public GameObject page;
        public DragSticker playerSticker;
        public Button backButton;
        public Button clothButton;

        private GameFlow flow;

        private void Awake()
        {
            bootstrap = GameBootstrap.Instance;
            flow = bootstrap != null ? bootstrap.gameFlow : null;
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            if (clothButton != null)
                clothButton.onClick.AddListener(OnCloth);
            ResetPage();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBack);
            if (clothButton != null)
                clothButton.onClick.RemoveListener(OnCloth);
        }

        public void ResetPage()
        {
            playerSticker?.Restore();
        }

        public void PlayEntrance()
        {
            ResetPage();
            if (page == null)
                return;

            UiOrbitArrival[] arrivals =
                page.GetComponentsInChildren<UiOrbitArrival>(true);
            for (int i = 0; i < arrivals.Length; i++)
                arrivals[i].Play();
        }

        public void StartLevel(int index)
        {
            if (flow == null)
                flow = GameBootstrap.Instance?.gameFlow;
            LevelConfig level = FindLevel(index);
            if (level == null)
            {
                Debug.LogWarning("[LevelSelectController] Missing level " + index);
                return;
            }

            if (UiTransition.Instance != null)
                UiTransition.Instance.Play(() => flow?.StartRun(level));
            else
                flow?.StartRun(level);
        }

        private void OnBack()
        {
            if (UiTransition.Instance != null)
            {
                UiTransition.Instance.Play(
                    () => StartPageController.Instance?.ShowStartPage());
            }
            else
            {
                StartPageController.Instance?.ShowStartPage();
            }
        }

        private void OnCloth()
        {
            // 换装界面按规划暂不实现；按钮保留点击音效/粒子。
        }

        private LevelConfig FindLevel(int index)
        {
            LevelDatabase database = bootstrap != null
                ? bootstrap.levelDatabase
                : null;
            if (database == null || database.levels == null)
                return null;

            if (index >= 3)
            {
                for (int i = 0; i < database.levels.Count; i++)
                {
                    LevelConfig level = database.levels[i];
                    if (level != null && level.IsEndless)
                        return level;
                }
                return null;
            }

            int wantedOrder = index + 1;
            for (int i = 0; i < database.levels.Count; i++)
            {
                LevelConfig level = database.levels[i];
                if (level != null && !level.IsEndless && level.order == wantedOrder)
                    return level;
            }

            int fallback = 0;
            for (int i = 0; i < database.levels.Count; i++)
            {
                LevelConfig level = database.levels[i];
                if (level == null || level.IsEndless)
                    continue;
                if (fallback == index)
                    return level;
                fallback++;
            }
            return null;
        }
    }
}
