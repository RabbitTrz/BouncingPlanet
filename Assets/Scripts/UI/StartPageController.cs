using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 开始页：贴纸拖动到中央黑洞后分发设置/玩家/成员/退出。
    /// 玩家贴纸进入选关页，选关页再拖到具体关卡。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StartPageController : MonoBehaviour
    {
        public static StartPageController Instance { get; private set; }

        public GameBootstrap bootstrap;
        public CanvasGroup startPageGroup;
        public GameObject settingsPopup;
        public GameObject membersPopup;
        public Button settingsCloseButton;
        public Button membersCloseButton;
        public DragSticker settingsSticker;
        public DragSticker playerSticker;
        public DragSticker quitSticker;
        public DragSticker membersSticker;
        public GameObject levelSelectPage;
        public LevelSelectController levelSelect;
        public StartDragHintView dragHint;

        private GameFlow flow;
        private bool popupOpen;
        private bool wasOnStart;
        private bool levelSelectOpen;
        private readonly List<StickerGuideLink> guideLinks =
            new List<StickerGuideLink>();
        private bool guidesInitialized;

        private void Awake()
        {
            Instance = this;
            bootstrap = GameBootstrap.Instance;
            flow = bootstrap != null ? bootstrap.gameFlow : null;

            if (settingsCloseButton != null)
                settingsCloseButton.onClick.AddListener(CloseSettingsPopup);
            if (membersCloseButton != null)
                membersCloseButton.onClick.AddListener(CloseMembersPopup);

            ClosePopups();
            SetupGuideLines();
            UpdateVisibility();
        }

        private void OnDestroy()
        {
            if (settingsCloseButton != null)
                settingsCloseButton.onClick.RemoveListener(CloseSettingsPopup);
            if (membersCloseButton != null)
                membersCloseButton.onClick.RemoveListener(CloseMembersPopup);
            HideGuideLines();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
            if (flow == null && bootstrap != null)
                flow = bootstrap.gameFlow;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            bool onStart = flow == null ||
                flow.State == GameFlow.FlowState.Start;

            if (!onStart)
            {
                if (popupOpen)
                    ClosePopups();
                if (levelSelectOpen)
                {
                    levelSelectOpen = false;
                    if (levelSelectPage != null)
                        levelSelectPage.SetActive(false);
                }
                HideGuideLines();
                if (wasOnStart)
                    RestoreAllStickers();
                wasOnStart = false;
                if (startPageGroup != null)
                {
                    startPageGroup.alpha = 0f;
                    startPageGroup.blocksRaycasts = false;
                    startPageGroup.interactable = false;
                }
                return;
            }

            if (!wasOnStart)
            {
                if (!levelSelectOpen)
                    ShowStartPage();
                wasOnStart = true;
            }

            if (startPageGroup != null)
            {
                startPageGroup.alpha = levelSelectOpen ? 0f : 1f;
                bool interactive = !popupOpen && !levelSelectOpen;
                startPageGroup.blocksRaycasts = interactive;
                startPageGroup.interactable = interactive;
            }
        }

        public void HandleStickerDrop(StickerAction action)
        {
            dragHint?.MarkCompleted();
            switch (action)
            {
                case StickerAction.Settings:
                    OpenSettings();
                    break;
                case StickerAction.Members:
                    OpenMembers();
                    break;
                case StickerAction.Player:
                    OpenLevelSelect();
                    break;
                case StickerAction.Quit:
                    QuitGame();
                    break;
            }
        }

        public void OpenLevelSelect(bool suppressStartPage = false)
        {
            HideGuideLines();
            dragHint?.Suppress();
            if (suppressStartPage)
            {
                levelSelectOpen = true;
                if (startPageGroup != null)
                {
                    startPageGroup.alpha = 0f;
                    startPageGroup.blocksRaycasts = false;
                    startPageGroup.interactable = false;
                }
            }

            if (UiTransition.Instance != null)
            {
                UiTransition.Instance.Play(ApplyOpenLevelSelect);
                SetStartInteractable(false);
            }
            else
            {
                ApplyOpenLevelSelect();
            }
        }

        private void ApplyOpenLevelSelect()
        {
            HideGuideLines();
            dragHint?.Suppress();
            ClosePopups();
            levelSelectOpen = true;
            if (levelSelectPage != null)
                levelSelectPage.SetActive(true);
            levelSelect?.PlayEntrance();
            SetStartInteractable(false);
            if (startPageGroup != null)
                startPageGroup.alpha = 0f;
        }

        public void ShowStartPage()
        {
            HideGuideLines();
            dragHint?.Suppress();
            levelSelectOpen = false;
            if (levelSelectPage != null)
                levelSelectPage.SetActive(false);
            if (startPageGroup != null)
            {
                startPageGroup.alpha = 1f;
                startPageGroup.blocksRaycasts = !popupOpen;
                startPageGroup.interactable = !popupOpen;
            }
            RestoreAllStickers();
        }

        public void OpenSettings()
        {
            HideGuideLines();
            dragHint?.Suppress();
            ClosePopups();
            popupOpen = true;
            if (settingsPopup != null)
                settingsPopup.SetActive(true);
            SetStartInteractable(false);
        }

        public void OpenMembers()
        {
            HideGuideLines();
            dragHint?.Suppress();
            ClosePopups();
            popupOpen = true;
            if (membersPopup != null)
                membersPopup.SetActive(true);
            SetStartInteractable(false);
        }

        public void CloseSettingsPopup()
        {
            HidePopup(settingsPopup);
            popupOpen = false;
            settingsSticker?.Restore();
            SetStartInteractable(true);
        }

        public void CloseMembersPopup()
        {
            HidePopup(membersPopup);
            popupOpen = false;
            membersSticker?.Restore();
            SetStartInteractable(true);
        }

        public void ClosePopups()
        {
            popupOpen = false;
            HidePopup(settingsPopup);
            HidePopup(membersPopup);

            bool onStart = flow == null ||
                flow.State == GameFlow.FlowState.Start;
            if (onStart && !levelSelectOpen)
                SetStartInteractable(true);
        }

        private static void HidePopup(GameObject popup)
        {
            if (popup == null || !popup.activeSelf)
                return;

            PopupScaleIn animation =
                popup.GetComponentInChildren<PopupScaleIn>(true);
            if (animation != null)
                animation.Close();
            else
                popup.SetActive(false);
        }

        private void SetupGuideLines()
        {
            if (guidesInitialized)
                return;
            guidesInitialized = true;

            Transform root = startPageGroup != null
                ? startPageGroup.transform
                : transform;
            BindGuideLine(root, "Line_SettingsToHole", settingsSticker);
            BindGuideLine(root, "Line_PlayerToHole", playerSticker);
            BindGuideLine(root, "Line_QuitToHole", quitSticker);
            BindGuideLine(root, "Line_GroupToHole", membersSticker);
        }

        private void BindGuideLine(
            Transform root, string lineName, DragSticker sticker)
        {
            if (root == null || sticker == null)
                return;
            Transform lineTransform = root.Find(lineName);
            if (lineTransform == null)
                return;

            GuideLineAnimator animator =
                lineTransform.GetComponent<GuideLineAnimator>();
            if (animator == null)
                animator = lineTransform.gameObject
                    .AddComponent<GuideLineAnimator>();

            RectTransform stickerRect = sticker.transform as RectTransform;
            Vector2 direction = stickerRect != null
                ? -stickerRect.anchoredPosition
                : Vector2.zero;
            animator.Configure(direction);

            StickerGuideLink link =
                lineTransform.GetComponent<StickerGuideLink>();
            if (link == null)
                link = lineTransform.gameObject.AddComponent<StickerGuideLink>();
            link.Bind(sticker, animator);
            guideLinks.Add(link);
        }

        private void HideGuideLines()
        {
            for (int i = 0; i < guideLinks.Count; i++)
                guideLinks[i]?.Hide();
        }

        private void RestoreAllStickers()
        {
            settingsSticker?.Restore();
            playerSticker?.Restore();
            quitSticker?.Restore();
            membersSticker?.Restore();
        }

        private void SetStartInteractable(bool interactable)
        {
            if (startPageGroup == null)
                return;
            bool value = interactable && !levelSelectOpen;
            startPageGroup.interactable = value;
            startPageGroup.blocksRaycasts = value;
        }

        private void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
