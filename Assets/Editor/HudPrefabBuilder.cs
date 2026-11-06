using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet.Editor
{
    /// <summary>
    /// 生成可拖进场景的 GameHud 预制体。
    /// 首次编译后自动创建；也可以从菜单手动重建。
    /// </summary>
    public static class HudPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/GameHud.prefab";
        private const string NewPrefabPath = PrefabFolder + "/GameHud-new.prefab";
        private const string SkillFolder = "Assets/Resources/Raw/skill/";
        private const string NumberFolder = "Assets/Resources/Raw/number/";

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

            // 刚编译/导入资源时可能还没准备好，下个编辑器 tick 再试。
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureBuilt;
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                BuildPrefab();

            EnsurePrefabUpgrades(PrefabPath);
            EnsurePrefabUpgrades(NewPrefabPath);
        }

        /// <summary>
        /// 旧版 HUD 预制体缺少 ReadyBurst / PressureWarning / 当前高度时只补节点，
        /// 不覆盖已经手动调整过的布局。GameHud 和 GameHud-new 都会检查。
        /// </summary>
        private static void EnsurePrefabUpgrades(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                return;

            bool changed = false;
            SkillIconView[] views = root.GetComponentsInChildren<SkillIconView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                SkillIconView view = views[i];
                if (view == null || view.readyBurst != null)
                    continue;
                view.readyBurst = CreateReadyBurst(view.transform);
                changed = true;
            }

            GameHudView hud = root.GetComponentInChildren<GameHudView>(true);
            if (hud != null && hud.pressureWarning == null)
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    hud.pressureWarning = CreatePressureWarning(rootRect);
                    changed = true;
                }
            }

            if (hud != null && hud.heightCurrent == null)
            {
                SpriteNumberDisplay current =
                    CreateCurrentHeightDisplay(hud.heightRecord);
                if (current != null)
                {
                    hud.heightCurrent = current;
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();
                Debug.Log("[HudPrefabBuilder] 已为 " + path + " 补充 HUD 节点。");
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        [MenuItem("Tools/BouncingPlanet/UI/Rebuild Game HUD Prefab")]
        public static void RebuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "重建 HUD 预制体",
                        "会覆盖现有的 GameHud.prefab，手动改动会丢失。继续？",
                        "重建", "取消"))
                {
                    return;
                }
            }

            BuildPrefab();
            EnsurePrefabUpgrades(NewPrefabPath);
        }

        [MenuItem("Tools/BouncingPlanet/UI/Upgrade HUD Prefabs")]
        public static void UpgradeFromMenu()
        {
            EnsurePrefabUpgrades(PrefabPath);
            EnsurePrefabUpgrades(NewPrefabPath);
        }

        private static void BuildPrefab()
        {
            EnsureFolders();
            Sprite[] digits = LoadDigitSprites();
            Sprite musouSprite = LoadSprite(SkillFolder + "skill_overdrive_nop.PNG");
            Sprite bombSprite = LoadSprite(SkillFolder + "skill_bomb_nop.PNG");
            Sprite hookSprite = LoadSprite(SkillFolder + "skill_lock_nop.PNG");

            GameObject root = new GameObject(
                "GameHud",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(GameHudView));

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            GameHudView hud = root.GetComponent<GameHudView>();
            hud.canvasGroup = group;
            hud.pressureWarning = CreatePressureWarning(rootRect);
            hud.heightRecord = CreateNumberDisplay(
                rootRect, "HeightRecord", new Vector2(48f, -36f),
                TextAnchor.MiddleLeft, 5, digits);
            hud.heightCurrent = CreateCurrentHeightDisplay(hud.heightRecord);
            hud.scoreDisplay = CreateNumberDisplay(
                rootRect, "Score", new Vector2(-48f, -36f),
                TextAnchor.MiddleRight, 6, digits);

            RectTransform skillsRoot = CreateRectTransform(rootRect, "Skills");
            skillsRoot.anchorMin = skillsRoot.anchorMax = new Vector2(1f, 0f);
            skillsRoot.pivot = new Vector2(1f, 0f);
            skillsRoot.anchoredPosition = new Vector2(-48f, 48f);
            skillsRoot.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup skillsLayout =
                skillsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            skillsLayout.childAlignment = TextAnchor.MiddleRight;
            skillsLayout.spacing = 18f;
            skillsLayout.childControlWidth = true;
            skillsLayout.childControlHeight = true;
            skillsLayout.childForceExpandWidth = false;
            skillsLayout.childForceExpandHeight = false;

            ContentSizeFitter skillsFitter =
                skillsRoot.gameObject.AddComponent<ContentSizeFitter>();
            skillsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            skillsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            hud.musouSkill = CreateSkillIcon(
                skillsRoot, "Skill_Musou", musouSprite);
            hud.bombSkill = CreateSkillIcon(
                skillsRoot, "Skill_Bomb", bombSprite);
            hud.hookSkill = CreateSkillIcon(
                skillsRoot, "Skill_Hook", hookSprite);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HudPrefabBuilder] 生成 " + PrefabPath);
        }

        private static SpriteNumberDisplay CreateNumberDisplay(
            Transform parent, string name, Vector2 anchoredPosition,
            TextAnchor alignment, int digitCount, Sprite[] digits,
            float digitSize = 56f)
        {
            RectTransform root = CreateRectTransform(parent, name);
            bool alignRight = alignment == TextAnchor.MiddleRight;
            root.anchorMin = root.anchorMax =
                new Vector2(alignRight ? 1f : 0f, 1f);
            root.pivot = new Vector2(alignRight ? 1f : 0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup layout =
                root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = alignment;
            layout.spacing = 6f;
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

                LayoutElement element = digitObject.GetComponent<LayoutElement>();
                element.preferredWidth = digitSize;
                element.preferredHeight = digitSize;

                display.digitImages[i] = image;
            }

            display.SetValue(0);
            return display;
        }

        /// <summary>
        /// 在“最高高度”数字下面补一个更小的当前高度数字。
        /// 不挂到最高高度节点内部，避免被它的 HorizontalLayoutGroup 当成数字槽。
        /// </summary>
        private static SpriteNumberDisplay CreateCurrentHeightDisplay(
            SpriteNumberDisplay record)
        {
            if (record == null)
                return null;

            RectTransform recordRect = record.transform as RectTransform;
            if (recordRect == null || recordRect.parent == null)
                return null;

            Sprite[] digits = record.digitSprites;
            if (digits == null || digits.Length < 10)
                digits = LoadDigitSprites();

            bool alignRight = recordRect.pivot.x > 0.5f;
            TextAnchor alignment = alignRight
                ? TextAnchor.MiddleRight
                : TextAnchor.MiddleLeft;
            Vector2 position = recordRect.anchoredPosition +
                new Vector2(0f, -120f);

            SpriteNumberDisplay display = CreateNumberDisplay(
                recordRect.parent, "CurrentHeight", position,
                alignment, 5, digits, 64f);
            RectTransform rect = display.transform as RectTransform;
            rect.anchorMin = recordRect.anchorMin;
            rect.anchorMax = recordRect.anchorMax;
            rect.pivot = recordRect.pivot;
            rect.anchoredPosition = position;
            return display;
        }

        private static SkillIconView CreateSkillIcon(
            Transform parent, string name, Sprite sprite)
        {
            GameObject root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(SkillIconView),
                typeof(LayoutElement));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);

            Image icon = root.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            LayoutElement element = root.GetComponent<LayoutElement>();
            element.preferredWidth = 96f;
            element.preferredHeight = 96f;

            GameObject cooldownObject = new GameObject(
                "Cooldown",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            cooldownObject.transform.SetParent(root.transform, false);

            RectTransform cooldownRect = cooldownObject.GetComponent<RectTransform>();
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.offsetMin = Vector2.zero;
            cooldownRect.offsetMax = Vector2.zero;

            Image cooldown = cooldownObject.GetComponent<Image>();
            cooldown.sprite = sprite;
            cooldown.color = new Color(0f, 0f, 0f, 0.62f);
            cooldown.preserveAspect = true;
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = true;
            cooldown.fillAmount = 0f;
            cooldown.raycastTarget = false;
            cooldownObject.SetActive(false);

            SkillIconView view = root.GetComponent<SkillIconView>();
            view.iconImage = icon;
            view.cooldownImage = cooldown;
            view.readyBurst = CreateReadyBurst(root.transform);
            view.hideWhenDisabled = true;
            return view;
        }

        private static Image CreatePressureWarning(Transform parent)
        {
            GameObject go = new GameObject(
                "PressureWarning",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 120f);

            Image image = go.GetComponent<Image>();
            // 用内置 UI Sprite 保证空 Image 也能稳定渲染。
            Sprite uiSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");
            if (uiSprite != null)
                image.sprite = uiSprite;
            image.color = new Color(0.22f, 0.02f, 0.04f, 0f);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static UiBurstEffect CreateReadyBurst(Transform parent)
        {
            GameObject burstObject = new GameObject(
                "ReadyBurst",
                typeof(RectTransform),
                typeof(UiBurstEffect));
            burstObject.transform.SetParent(parent, false);
            RectTransform rect = burstObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return burstObject.GetComponent<UiBurstEffect>();
        }

        private static RectTransform CreateRectTransform(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Sprite[] LoadDigitSprites()
        {
            Sprite[] digits = new Sprite[10];
            for (int i = 0; i < digits.Length; i++)
                digits[i] = LoadSprite(NumberFolder + i + "_nop.PNG");
            return digits;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite found)
                    return found;
            }

            Debug.LogWarning("[HudPrefabBuilder] 找不到 Sprite: " + path);
            return null;
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
