using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BouncingPlanet.Editor
{
    /// <summary>
    /// 生成开始页 UI 预制体。保持 uGUI 场景/预制体工作流，不碰玩法对象。
    /// </summary>
    public static class StartUiPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/GameUiRoot.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleAutoBuild()
        {
            if (Application.isPlaying)
                return;
            EditorApplication.delayCall += EnsureBuilt;
        }

        private static void EnsureBuilt()
        {
            if (Application.isPlaying)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureBuilt;
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;
            BuildPrefab();
        }

        [MenuItem("Tools/BouncingPlanet/UI/Rebuild Start UI Prefab")]
        public static void RebuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog(
                    "重建开始页 UI",
                    "会覆盖 GameUiRoot.prefab，手动改动会丢失。继续？",
                    "重建", "取消"))
            {
                return;
            }
            BuildPrefab();
        }

        private static void BuildPrefab()
        {
            EnsureFolders();

            Sprite background = LoadSprite(
                "Assets/Resources/Raw/backgroud/back_begin.PNG");
            Sprite paper = LoadSprite(
                "Assets/Resources/Raw/Player/paper/Paper.PNG");
            Sprite title = LoadSprite(
                "Assets/Resources/Raw/title/title_blue.PNG");
            Sprite back = LoadSprite("Assets/Resources/Raw/button/back_p.PNG");
            Sprite buttonSetting = LoadSprite(
                "Assets/Resources/Raw/button/setting_p.PNG");
            Sprite buttonRestart = LoadSprite(
                "Assets/Resources/Raw/button/restart_p.PNG");
            Sprite backWin = LoadSprite(
                "Assets/Resources/Raw/backgroud/back_win.PNG");
            Sprite backLose = LoadSprite(
                "Assets/Resources/Raw/backgroud/back_lose.PNG");
            Sprite[] digits = LoadDigitSprites();
            Sprite settingsSprite = LoadSprite(
                "Assets/Resources/Raw/begin/paster_setting_blue.PNG");
            Sprite playerSprite = LoadSprite(
                "Assets/Resources/Raw/begin/paster_player_blue.PNG");
            Sprite playerHappy = LoadSprite(
                "Assets/Resources/Image/Player/player_happy.png");
            Sprite playerDown = LoadSprite(
                "Assets/Resources/Image/Player/player_down.png");
            Sprite playerUp = LoadSprite(
                "Assets/Resources/Image/Player/player_up.png");
            Sprite quitSprite = LoadSprite(
                "Assets/Resources/Raw/begin/paster_quit_blue.PNG");
            Sprite groupSprite = LoadSprite(
                "Assets/Resources/Raw/begin/paster_group_blue.PNG");

            Sprite volumeNo = LoadSprite(
                "Assets/Resources/Raw/volume/volume_no.PNG");
            Sprite volumeLow = LoadSprite(
                "Assets/Resources/Raw/volume/volume_low.PNG");
            Sprite volumeMid = LoadSprite(
                "Assets/Resources/Raw/volume/volume_mid.PNG");
            Sprite volumeHigh = LoadSprite(
                "Assets/Resources/Raw/volume/volume_high.PNG");
            Sprite volumeLine = LoadSprite(
                "Assets/Resources/Raw/volume/volume_line.PNG");
            Sprite volumeBlock = LoadSprite(
                "Assets/Resources/Raw/volume/volume_block.PNG");
            Sprite chooseBackground = LoadSprite(
                "Assets/Resources/Raw/backgroud/back_choose.PNG");
            Sprite cloth = LoadSprite(
                "Assets/Resources/Raw/button/clothchange_p.PNG");
            Sprite shiny = LoadSprite(
                "Assets/Resources/Image/Planet/shiny_idle.PNG");
            Sprite bomb = LoadSprite(
                "Assets/Resources/Image/Planet/bomb_idle.PNG");
            Sprite cloud = LoadSprite(
                "Assets/Resources/Image/Planet/cloud_idle.PNG");
            Sprite[] holeFrames = LoadSprites(
                "Assets/Resources/Image/Hole/hole_paper.PNG");
            Sprite hole = holeFrames.Length > 0 ? holeFrames[0] : null;
            Sprite infinity = LoadSprite(
                "Assets/Resources/Raw/hole/ehdless1.PNG");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
            Sprite fullSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/Background.psd");

            GameObject root = new GameObject(
                "GameUiRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(StartPageController));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(root.transform, false);

            RectTransform start = CreateRect(rootRect, "StartPage");
            Stretch(start);
            CanvasGroup startGroup = start.gameObject.AddComponent<CanvasGroup>();

            Image bg = CreateFullImage(start, "Background", background, uiSprite);
            bg.color = Color.white;

            Image titleImage = CreateImage(start, "Title", title,
                new Vector2(0f, 365f), new Vector2(1040f, 347f));
            titleImage.raycastTarget = false;
            StickerSway titleSway =
                titleImage.gameObject.AddComponent<StickerSway>();
            titleSway.angle = 3f;
            titleSway.frequency = 1.43f;
            titleSway.steps = 3;

            RectTransform dropRect = CreateRect(start, "DropTarget");
            dropRect.anchorMin = dropRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            dropRect.pivot = new Vector2(0.5f, 0.5f);
            dropRect.anchoredPosition = Vector2.zero;
            dropRect.sizeDelta = new Vector2(560f, 560f);
            StickerDropTarget target = dropRect.gameObject
                .AddComponent<StickerDropTarget>();
            target.area = dropRect;

            DragSticker settingsSticker = CreateSticker(start,
                "Sticker_Settings", settingsSprite,
                new Vector2(-735f, 105f), new Vector2(360f, 360f),
                StickerAction.Settings, target);
            DragSticker playerSticker = CreateSticker(start,
                "Sticker_Player", playerSprite,
                new Vector2(700f, 60f), new Vector2(390f, 390f),
                StickerAction.Player, target);
            DragSticker quitSticker = CreateSticker(start,
                "Sticker_Quit", quitSprite,
                new Vector2(500f, -300f), new Vector2(340f, 340f),
                StickerAction.Quit, target);
            DragSticker membersSticker = CreateSticker(start,
                "Sticker_Members", groupSprite,
                new Vector2(-595f, -330f), new Vector2(300f, 300f),
                StickerAction.Members, target);

            StartPageController controller = root.GetComponent<StartPageController>();
            controller.startPageGroup = startGroup;
            controller.settingsSticker = settingsSticker;
            controller.playerSticker = playerSticker;
            controller.quitSticker = quitSticker;
            controller.membersSticker = membersSticker;

            GameObject settingsPopup = CreatePopup(
                rootRect, "SettingsPopup", fullSprite, paper);
            Image panel = CreateImage(settingsPopup.transform, "Panel", paper,
                Vector2.zero, new Vector2(980f, 600f));
            panel.preserveAspect = false;
            panel.raycastTarget = true;
            PopupScaleIn settingsAnimation =
                panel.gameObject.AddComponent<PopupScaleIn>();
            settingsAnimation.closeRoot = settingsPopup;
            Button settingsClose = CreateButton(panel.transform, "Close",
                back, new Vector2(-390f, 225f), new Vector2(96f, 96f));

            Image volumeIcon = CreateImage(panel.transform, "VolumeIcon",
                volumeHigh, new Vector2(-310f, 40f), new Vector2(140f, 140f));
            volumeIcon.raycastTarget = true;
            Button muteButton = volumeIcon.gameObject.AddComponent<Button>();
            muteButton.targetGraphic = volumeIcon;
            AddButtonEffect(muteButton, volumeIcon);

            Slider volumeSlider = CreateVolumeSlider(panel.transform,
                new Vector2(100f, 40f), new Vector2(560f, 80f),
                volumeLine, volumeBlock);
            VolumeSettingView volume = panel.gameObject
                .AddComponent<VolumeSettingView>();
            volume.volumeSlider = volumeSlider;
            volume.volumeIcon = volumeIcon;
            volume.muteButton = muteButton;
            volume.volumeNo = volumeNo;
            volume.volumeLow = volumeLow;
            volume.volumeMid = volumeMid;
            volume.volumeHigh = volumeHigh;

            GameObject membersPopup = CreatePopup(
                rootRect, "MembersPopup", fullSprite, paper);
            Image memberPanel = CreateImage(membersPopup.transform, "Panel",
                paper, Vector2.zero, new Vector2(980f, 600f));
            memberPanel.preserveAspect = false;
            memberPanel.raycastTarget = true;
            PopupScaleIn membersAnimation =
                memberPanel.gameObject.AddComponent<PopupScaleIn>();
            membersAnimation.closeRoot = membersPopup;
            Image group = CreateImage(memberPanel.transform, "GroupSticker",
                groupSprite, new Vector2(0f, 40f), new Vector2(260f, 260f));
            group.raycastTarget = false;
            Button membersClose = CreateButton(memberPanel.transform, "Close",
                back, new Vector2(-390f, 225f), new Vector2(96f, 96f));

            settingsPopup.SetActive(false);
            membersPopup.SetActive(false);
            controller.settingsPopup = settingsPopup;
            controller.membersPopup = membersPopup;
            controller.settingsCloseButton = settingsClose;
            controller.membersCloseButton = membersClose;

            LevelSelectController levelSelect =
                root.AddComponent<LevelSelectController>();
            RectTransform levelPage = CreateRect(
                rootRect, "LevelSelectPage");
            Stretch(levelPage);
            Image levelBackground = CreateFullImage(
                levelPage, "Background", chooseBackground, fullSprite);
            levelBackground.color = Color.white;

            Image endlessImage = CreateImage(levelPage, "Endless", hole,
                new Vector2(-650f, 25f), new Vector2(520f, 520f));
            endlessImage.raycastTarget = false;
            UiSpriteAnimator holeAnimator =
                endlessImage.gameObject.AddComponent<UiSpriteAnimator>();
            holeAnimator.target = endlessImage;
            holeAnimator.frames = holeFrames;
            holeAnimator.fps = 10f;
            Image infinityImage = CreateImage(endlessImage.transform,
                "Infinity", infinity, Vector2.zero, new Vector2(220f, 110f));
            infinityImage.raycastTarget = false;
            StickerDropTarget endlessTarget = endlessImage.gameObject
                .AddComponent<StickerDropTarget>();
            endlessTarget.area = endlessImage.rectTransform;
            endlessTarget.levelSelect = levelSelect;
            endlessTarget.levelIndex = 3;

            StickerDropTarget shinyTarget = CreateLevelIcon(
                levelPage, "Level_1_Shiny", shiny,
                new Vector2(0f, -205f), new Vector2(270f, 270f),
                0, levelSelect, 130f, 0.08f);
            StickerDropTarget bombTarget = CreateLevelIcon(
                levelPage, "Level_2_Bomb", bomb,
                new Vector2(430f, 170f), new Vector2(270f, 270f),
                1, levelSelect, 80f, 0.22f);
            StickerDropTarget cloudTarget = CreateLevelIcon(
                levelPage, "Level_3_Cloud", cloud,
                new Vector2(740f, -150f), new Vector2(300f, 300f),
                2, levelSelect, 100f, 0.36f);

            Image playerImage = CreateImage(levelPage,
                "LevelSelect_Player", playerHappy,
                new Vector2(-340f, 305f), new Vector2(220f, 220f));
            playerImage.raycastTarget = true;
            playerImage.preserveAspect = true;
            playerImage.gameObject.AddComponent<CanvasGroup>();
            DragSticker levelPlayer = playerImage.gameObject
                .AddComponent<DragSticker>();
            levelPlayer.action = StickerAction.Player;
            levelPlayer.idleSprite = playerHappy;
            levelPlayer.dragSprite = playerDown;
            levelPlayer.consumeSprite = playerUp;
            levelPlayer.dropTargets = new[]
            {
                shinyTarget, bombTarget, cloudTarget, endlessTarget
            };
            levelPlayer.clickBurst = CreateBurstLayer(playerImage.transform);
            UiOrbitArrival playerArrival = playerImage.gameObject
                .AddComponent<UiOrbitArrival>();
            playerArrival.orbitCenter = new Vector2(-650f, 25f);
            playerArrival.startAngleDegrees = 160f;
            playerArrival.delay = 0.50f;

            Button clothButton = CreateButton(levelPage, "Cloth", cloth,
                new Vector2(-110f, 445f), new Vector2(110f, 110f));
            StickerSway clothSway =
                clothButton.gameObject.AddComponent<StickerSway>();
            clothSway.angle = 2.5f;
            Button backButton = CreateButton(levelPage, "Back", back,
                new Vector2(80f, 445f), new Vector2(110f, 110f));
            StickerSway backSway =
                backButton.gameObject.AddComponent<StickerSway>();
            backSway.angle = 2.5f;

            levelSelect.page = levelPage.gameObject;
            levelSelect.playerSticker = levelPlayer;
            levelSelect.backButton = backButton;
            levelSelect.clothButton = clothButton;
            controller.levelSelectPage = levelPage.gameObject;
            controller.levelSelect = levelSelect;
            levelPage.gameObject.SetActive(false);

            PauseMenuController pause = root.AddComponent<PauseMenuController>();
            GameObject pausePopup = CreatePopup(
                rootRect, "PausePopup", fullSprite, paper);
            Image pausePanel = CreateImage(pausePopup.transform, "Panel",
                paper, Vector2.zero, new Vector2(780f, 380f));
            pausePanel.preserveAspect = false;
            pausePanel.raycastTarget = true;
            PopupScaleIn pauseAnimation =
                pausePanel.gameObject.AddComponent<PopupScaleIn>();
            pauseAnimation.closeRoot = pausePopup;
            Button pauseSettings = CreateButton(pausePanel.transform,
                "Settings", buttonSetting,
                new Vector2(-230f, 0f), new Vector2(130f, 130f));
            Button pauseBack = CreateButton(pausePanel.transform,
                "LevelSelect", back,
                new Vector2(0f, 0f), new Vector2(130f, 130f));
            Button pauseRestart = CreateButton(pausePanel.transform,
                "Restart", buttonRestart,
                new Vector2(230f, 0f), new Vector2(130f, 130f));
            pause.pausePopup = pausePopup;
            pause.settingsPopup = settingsPopup;
            pause.settingsButton = pauseSettings;
            pause.levelSelectButton = pauseBack;
            pause.restartButton = pauseRestart;
            pausePopup.SetActive(false);

            ResultMenuController result = root.AddComponent<ResultMenuController>();
            RectTransform resultPage = CreateRect(rootRect, "ResultPage");
            Stretch(resultPage);
            Image resultBackground = CreateFullImage(resultPage,
                "Background", backWin, fullSprite);
            resultBackground.color = Color.white;
            SpriteNumberDisplay resultHeight = CreateResultNumber(
                resultPage, new Vector2(0f, 120f), 6, digits);
            Button resultBack = CreateButton(resultPage, "Back", back,
                new Vector2(-250f, -250f), new Vector2(150f, 150f));
            Button resultRestart = CreateButton(resultPage, "Restart",
                buttonRestart, new Vector2(250f, -250f),
                new Vector2(150f, 150f));
            result.resultPage = resultPage.gameObject;
            result.background = resultBackground;
            result.winSprite = backWin;
            result.loseSprite = backLose;
            result.heightDisplay = resultHeight;
            result.levelSelectButton = resultBack;
            result.restartButton = resultRestart;
            resultPage.gameObject.SetActive(false);

            Image transitionImage = CreateFullImage(rootRect,
                "TransitionOverlay", null, fullSprite);
            transitionImage.color = new Color(0f, 0f, 0f, 0f);
            transitionImage.raycastTarget = false;
            transitionImage.transform.SetAsLastSibling();
            transitionImage.enabled = false;
            UiTransition transition = root.AddComponent<UiTransition>();
            transition.overlay = transitionImage;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StartUiPrefabBuilder] 生成 " + PrefabPath);
        }

        private static GameObject CreatePopup(Transform parent, string name,
            Sprite dimSprite, Sprite paperSprite)
        {
            RectTransform root = CreateRect(parent, name);
            Stretch(root);
            Image dim = CreateFullImage(root, "Dim", null, dimSprite);
            dim.color = new Color(0f, 0f, 0f, 0.58f);
            dim.raycastTarget = true;
            return root.gameObject;
        }

        private static DragSticker CreateSticker(Transform parent, string name,
            Sprite sprite, Vector2 position, Vector2 size,
            StickerAction action, StickerDropTarget target)
        {
            Image image = CreateImage(parent, name, sprite, position, size);
            image.raycastTarget = true;
            image.preserveAspect = true;
            image.gameObject.AddComponent<CanvasGroup>();
            DragSticker sticker = image.gameObject.AddComponent<DragSticker>();
            sticker.action = action;
            sticker.dropTarget = target;
            sticker.clickBurst = CreateBurstLayer(image.transform);
            return sticker;
        }

        private static UiBurstEffect CreateBurstLayer(Transform parent)
        {
            GameObject go = new GameObject(
                "ClickBurst",
                typeof(RectTransform),
                typeof(UiBurstEffect));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.GetComponent<UiBurstEffect>();
        }

        private static Slider CreateVolumeSlider(Transform parent,
            Vector2 position, Vector2 size, Sprite barSprite, Sprite handleSprite)
        {
            RectTransform root = CreateRect(parent, "VolumeSlider");
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = position;
            root.sizeDelta = size;

            Image bar = CreateFullImage(root, "Bar", barSprite, barSprite);
            bar.preserveAspect = false;
            bar.raycastTarget = false;

            RectTransform handleArea = CreateRect(root, "HandleArea");
            Stretch(handleArea);
            Image handle = CreateImage(handleArea, "Handle", handleSprite,
                Vector2.zero, new Vector2(56f, 80f));
            handle.raycastTarget = true;
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = null;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.65f;
            return slider;
        }

        private static StickerDropTarget CreateLevelIcon(
            Transform parent, string name, Sprite sprite, Vector2 position,
            Vector2 size, int levelIndex, LevelSelectController controller,
            float startAngle, float delay)
        {
            Image image = CreateImage(parent, name, sprite, position, size);
            image.raycastTarget = false;
            image.gameObject.AddComponent<CanvasGroup>();

            StickerDropTarget target = image.gameObject
                .AddComponent<StickerDropTarget>();
            target.area = image.rectTransform;
            target.levelSelect = controller;
            target.levelIndex = levelIndex;

            StickerSway sway = image.gameObject.AddComponent<StickerSway>();
            sway.angle = 2.5f;
            sway.frequency = 1.43f;
            sway.steps = 3;

            UiOrbitArrival arrival =
                image.gameObject.AddComponent<UiOrbitArrival>();
            arrival.orbitCenter = new Vector2(-650f, 25f);
            arrival.startAngleDegrees = startAngle;
            arrival.delay = delay;
            return target;
        }

        private static SpriteNumberDisplay CreateResultNumber(
            Transform parent, Vector2 position, int digitCount, Sprite[] digits)
        {
            RectTransform root = CreateRect(parent, "HighestHeight");
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = position;
            root.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup layout =
                root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SpriteNumberDisplay display =
                root.gameObject.AddComponent<SpriteNumberDisplay>();
            display.digitSprites = digits;
            display.digitImages = new Image[digitCount];
            for (int i = 0; i < digitCount; i++)
            {
                GameObject digitObject = new GameObject(
                    "Digit_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(LayoutElement));
                digitObject.transform.SetParent(root, false);
                Image image = digitObject.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                LayoutElement element =
                    digitObject.GetComponent<LayoutElement>();
                element.preferredWidth = 100f;
                element.preferredHeight = 100f;
                display.digitImages[i] = image;
            }
            display.SetValue(0);
            return display;
        }

        private static Sprite[] LoadDigitSprites()
        {
            Sprite[] digits = new Sprite[10];
            for (int i = 0; i < digits.Length; i++)
            {
                digits[i] = LoadSprite(
                    "Assets/Resources/Raw/number/" + i + "_nop.PNG");
            }
            return digits;
        }

        private static Button CreateButton(Transform parent, string name,
            Sprite sprite, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(parent, name, sprite, position, size);
            image.raycastTarget = true;
            image.preserveAspect = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            AddButtonEffect(button, image);
            return button;
        }

        private static void AddButtonEffect(Button button, Image image)
        {
            if (button == null || image == null)
                return;
            button.transition = Selectable.Transition.None;
            UiBurstEffect burst = CreateBurstLayer(image.transform);
            UiButtonEffect effect =
                image.gameObject.AddComponent<UiButtonEffect>();
            effect.visual = image.rectTransform;
            effect.clickBurst = burst;
        }

        private static Image CreateFullImage(Transform parent, string name,
            Sprite sprite, Sprite fallback)
        {
            Image image = CreateImage(parent, name,
                sprite != null ? sprite : fallback,
                Vector2.zero, Vector2.zero);
            Stretch(image.rectTransform);
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(Transform parent, string name,
            Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
            return LoadFirstSprite(path);
        }

        private static Sprite LoadFirstSprite(string path)
        {
            Sprite[] sprites = LoadSprites(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }

        private static Sprite[] LoadSprites(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    sprites.Add(sprite);
            }
            if (sprites.Count == 0)
            {
                Debug.LogWarning("[StartUiPrefabBuilder] 找不到 Sprite: " + path);
                return Array.Empty<Sprite>();
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites.ToArray();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
