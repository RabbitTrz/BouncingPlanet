using System.Collections;
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
        public GameObject dressUpPage;
        public DressUpPageController dressUp;
        public CanvasGroup[] guideLines;
        public float guideLineFadeDuration = 0.22f;

        private GameFlow flow;
        private Coroutine guideRoutine;

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
            float revealDelay = 0f;
            for (int i = 0; i < arrivals.Length; i++)
            {
                arrivals[i].Play();
                revealDelay = Mathf.Max(
                    revealDelay,
                    arrivals[i].delay + Mathf.Max(0.05f, arrivals[i].duration));
            }

            HideGuideLines();
            if (guideRoutine != null)
                StopCoroutine(guideRoutine);
            guideRoutine = StartCoroutine(RevealGuideLines(revealDelay));
        }

        private IEnumerator RevealGuideLines(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float duration = Mathf.Max(0.01f, guideLineFadeDuration);
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetGuideLineAlpha(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            SetGuideLineAlpha(1f);
            guideRoutine = null;
        }

        private void HideGuideLines()
        {
            SetGuideLineAlpha(0f);
        }

        private void SetGuideLineAlpha(float alpha)
        {
            if (guideLines == null)
                return;
            for (int i = 0; i < guideLines.Length; i++)
            {
                if (guideLines[i] != null)
                    guideLines[i].alpha = alpha;
            }
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
            if (dressUpPage == null)
                return;

            if (UiTransition.Instance != null)
            {
                UiTransition.Instance.Play(() =>
                {
                    dressUpPage.SetActive(true);
                    dressUp?.Show();
                });
            }
            else
            {
                dressUpPage.SetActive(true);
                dressUp?.Show();
            }
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
