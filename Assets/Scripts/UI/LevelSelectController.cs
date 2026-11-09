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

        [SerializeField] private float lockedLevelBrightness = 0.42f;

        private sealed class LevelTargetInfo
        {
            public StickerDropTarget target;
            public Image[] images;
            public Color[] imageColors;
            public StickerSway sway;
            public bool swayInitiallyEnabled;
            public UiOrbitArrival arrival;
            public bool arrivalInitiallyEnabled;
            public UiSpriteAnimator[] spriteAnimators;
            public bool[] spriteAnimatorEnabled;
            public LevelConfig level;
            public bool unlocked = true;
        }

        private GameFlow flow;
        private PlayerProgress progress;
        private bool subscribedProgress;
        private readonly GuideLineAnimator[] lineAnimators =
            new GuideLineAnimator[4];
        private readonly LevelTargetInfo[] levelTargets =
            new LevelTargetInfo[4];
        private bool playerHovering;
        private bool playerPressed;

        private void Awake()
        {
            bootstrap = GameBootstrap.Instance;
            flow = bootstrap != null ? bootstrap.gameFlow : null;
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            if (clothButton != null)
                clothButton.onClick.AddListener(OnCloth);

            SetupLevelTargets();
            SetupGuideLines();
            SubscribeProgress();

            if (playerSticker != null)
            {
                playerSticker.HoverChanged += OnPlayerHoverChanged;
                playerSticker.PressChanged += OnPlayerPressChanged;
            }

            ResetPage();
            RefreshProgression();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBack);
            if (clothButton != null)
                clothButton.onClick.RemoveListener(OnCloth);
            if (playerSticker != null)
            {
                playerSticker.HoverChanged -= OnPlayerHoverChanged;
                playerSticker.PressChanged -= OnPlayerPressChanged;
            }
            if (subscribedProgress && progress != null)
                progress.Changed -= RefreshProgression;
            subscribedProgress = false;
        }

        private void Update()
        {
            if (page == null || !page.activeInHierarchy)
                return;
            if (flow == null && bootstrap != null)
                flow = bootstrap.gameFlow;
            if (flow != null && flow.State != GameFlow.FlowState.Start)
                return;
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (dressUpPage != null && dressUpPage.activeSelf)
            {
                if (dressUp != null && dressUp.TryCancelAnyDrag())
                    return;
                dressUp?.Close();
                return;
            }

            if (playerSticker != null && playerSticker.IsDragging)
            {
                playerSticker.CancelDrag();
                return;
            }

            OnBack();
        }

        public void ResetPage()
        {
            playerSticker?.Restore();
        }

        public void PlayEntrance()
        {
            ResetPage();
            RefreshProgression();
            HideGuideLines();
            if (page == null)
                return;

            UiOrbitArrival[] arrivals =
                page.GetComponentsInChildren<UiOrbitArrival>(true);
            for (int i = 0; i < arrivals.Length; i++)
            {
                if (arrivals[i].isActiveAndEnabled)
                    arrivals[i].Play();
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
            if (!IsLevelUnlocked(level))
                return;

            HideGuideLines();
            if (UiTransition.Instance != null)
                UiTransition.Instance.Play(() => flow?.StartRun(level));
            else
                flow?.StartRun(level);
        }

        private void OnBack()
        {
            HideGuideLines();
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

            HideGuideLines();
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

        private void SetupLevelTargets()
        {
            if (page == null)
                return;

            StickerDropTarget[] targets =
                page.GetComponentsInChildren<StickerDropTarget>(true);
            for (int i = 0; i < targets.Length; i++)
            {
                StickerDropTarget target = targets[i];
                if (target == null || target.levelSelect != this ||
                    target.levelIndex < 0 || target.levelIndex >= 4 ||
                    levelTargets[target.levelIndex] != null)
                {
                    continue;
                }

                StickerSway sway = target.GetComponent<StickerSway>();
                UiOrbitArrival arrival =
                    target.GetComponent<UiOrbitArrival>();
                Image[] images =
                    target.GetComponentsInChildren<Image>(true);
                Color[] imageColors = new Color[images.Length];
                for (int j = 0; j < images.Length; j++)
                    imageColors[j] = images[j].color;

                UiSpriteAnimator[] spriteAnimators =
                    target.GetComponentsInChildren<UiSpriteAnimator>(true);
                bool[] spriteAnimatorEnabled = new bool[spriteAnimators.Length];
                for (int j = 0; j < spriteAnimators.Length; j++)
                    spriteAnimatorEnabled[j] = spriteAnimators[j].enabled;

                levelTargets[target.levelIndex] = new LevelTargetInfo
                {
                    target = target,
                    images = images,
                    imageColors = imageColors,
                    sway = sway,
                    swayInitiallyEnabled = sway != null && sway.enabled,
                    arrival = arrival,
                    arrivalInitiallyEnabled = arrival != null && arrival.enabled,
                    spriteAnimators = spriteAnimators,
                    spriteAnimatorEnabled = spriteAnimatorEnabled,
                    level = FindLevel(target.levelIndex),
                    unlocked = true
                };
            }
        }

        private void SetupGuideLines()
        {
            if (guideLines == null)
                return;

            for (int i = 0; i < guideLines.Length; i++)
            {
                CanvasGroup group = guideLines[i];
                if (group == null)
                    continue;

                int index = LineIndex(group.gameObject.name);
                if (index < 0)
                    continue;

                GuideLineAnimator animator =
                    group.GetComponent<GuideLineAnimator>();
                if (animator == null)
                    animator = group.gameObject
                        .AddComponent<GuideLineAnimator>();
                lineAnimators[index] = animator;
            }

            RectTransform playerRect = playerSticker != null
                ? playerSticker.transform as RectTransform
                : null;
            for (int i = 0; i < lineAnimators.Length; i++)
            {
                GuideLineAnimator animator = lineAnimators[i];
                if (animator == null)
                    continue;

                Vector2 direction = Vector2.zero;
                LevelTargetInfo info = levelTargets[i];
                RectTransform targetRect = info != null && info.target != null
                    ? info.target.transform as RectTransform
                    : null;
                if (playerRect != null && targetRect != null)
                {
                    direction = targetRect.anchoredPosition -
                                playerRect.anchoredPosition;
                }
                animator.Configure(direction);
                animator.SetState(false, false);
            }
        }

        private static int LineIndex(string name)
        {
            if (name.EndsWith("To1"))
                return 0;
            if (name.EndsWith("To2"))
                return 1;
            if (name.EndsWith("To3"))
                return 2;
            if (name.Contains("ToHole"))
                return 3;
            return -1;
        }

        private void SubscribeProgress()
        {
            if (subscribedProgress)
                return;
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
            progress = bootstrap != null ? bootstrap.progress : null;
            if (progress == null)
                return;
            progress.Changed += RefreshProgression;
            subscribedProgress = true;
        }

        private void OnPlayerHoverChanged(bool hovering)
        {
            playerHovering = hovering;
            UpdateGuideLines();
        }

        private void OnPlayerPressChanged(bool pressed)
        {
            playerPressed = pressed;
            UpdateGuideLines();
        }

        private void RefreshProgression()
        {
            SubscribeProgress();

            for (int i = 0; i < levelTargets.Length; i++)
            {
                LevelTargetInfo info = levelTargets[i];
                if (info == null)
                    continue;

                LevelConfig level = info.level != null
                    ? info.level
                    : FindLevel(i);
                info.level = level;
                bool unlocked = IsLevelUnlocked(level);
                info.unlocked = unlocked;
                info.target.locked = !unlocked;

                if (info.images != null)
                {
                    for (int j = 0; j < info.images.Length; j++)
                    {
                        Color original = info.imageColors[j];
                        info.images[j].color = unlocked
                            ? original
                            : new Color(
                                original.r * lockedLevelBrightness,
                                original.g * lockedLevelBrightness,
                                original.b * lockedLevelBrightness,
                                original.a);
                    }
                }
                if (info.sway != null)
                {
                    info.sway.enabled =
                        unlocked && info.swayInitiallyEnabled;
                }
                if (info.arrival != null)
                {
                    info.arrival.enabled =
                        unlocked && info.arrivalInitiallyEnabled;
                }
                if (info.spriteAnimators != null)
                {
                    for (int j = 0; j < info.spriteAnimators.Length; j++)
                    {
                        info.spriteAnimators[j].enabled =
                            unlocked && info.spriteAnimatorEnabled[j];
                    }
                }
            }

            UpdateGuideLines();
        }

        private void UpdateGuideLines()
        {
            bool dragging = playerSticker != null &&
                            playerSticker.IsDragging;
            bool show = playerHovering || playerPressed || dragging;
            bool animate = playerPressed || dragging;

            for (int i = 0; i < lineAnimators.Length; i++)
            {
                GuideLineAnimator animator = lineAnimators[i];
                if (animator == null)
                    continue;

                LevelTargetInfo info = levelTargets[i];
                bool unlocked = info == null || info.unlocked;
                animator.SetState(show && unlocked, animate);
            }
        }

        private void HideGuideLines()
        {
            playerHovering = false;
            playerPressed = false;
            for (int i = 0; i < lineAnimators.Length; i++)
                lineAnimators[i]?.SetState(false, false);
        }

        private bool IsLevelUnlocked(LevelConfig level)
        {
            return progress == null || progress.IsLevelUnlocked(level);
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
