using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BouncingPlanet.Editor
{
    /// <summary>
    /// 策划用关卡编辑器：1/2/3 关生成基线后烘焙成固定星球列表，再手工调整；
    /// 无尽关只编辑规则和种子，预览不落盘。
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string LevelsFolder = "Assets/Resources/Levels";
        private const string DatabasePath = LevelsFolder + "/LevelDatabase.asset";
        private const float LeftWidth = 200f;
        private const float RightWidth = 360f;
        private const float PreviewPadding = 20f;

        private LevelDatabase database;
        private List<PlanetTypeDef> planetTypes = new List<PlanetTypeDef>();
        private GameConfig gameConfig;
        private int levelIndex;
        private int selectedPlanetIndex = -1;
        private readonly HashSet<int> selectedPlanets = new HashSet<int>();
        private readonly List<Vector3> dragSeeds = new List<Vector3>();
        private PlanetTypeDef paletteType;
        private Vector2 leftScroll;
        private Vector2 rightScroll;
        private LevelValidationResult validation;
        private List<LevelPlanetEntry> endlessPreview;
        private Texture2D circleTexture;
        private Texture2D ringTexture;
        private bool circularPreview = true;
        private bool draggingPlanet;
        private bool dragUndoRecorded;
        private float dragStartRadius;
        private float dragStartAngle;
        private float zoom = 1f;
        private float zoomFocusRadius;
        private float angleOffset;
        private bool boxSelecting;
        private Vector2 boxStart;
        private Vector2 boxEnd;
        private bool boxAdditive;
        private const float ScrollbarSize = 16f;

        [MenuItem("Tools/BouncingPlanet/Level Editor")]
        private static void Open()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>("关卡编辑器");
            window.minSize = new Vector2(1100f, 620f);
            window.Show();
        }

        [MenuItem("Tools/BouncingPlanet/Create Default Levels")]
        private static void CreateDefaultLevelsMenu()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>("关卡编辑器");
            window.CreateDefaultLevels();
        }

        private void OnEnable()
        {
            circleTexture = CreateCircleTexture();
            ringTexture = CreateRingTexture();
            RefreshAssets();
        }

        private void OnDisable()
        {
            if (circleTexture != null)
                DestroyImmediate(circleTexture);
            if (ringTexture != null)
                DestroyImmediate(ringTexture);
        }

        private void RefreshAssets()
        {
            planetTypes = LoadAll<PlanetTypeDef>();
            planetTypes.Sort((a, b) => a.id.CompareTo(b.id));
            gameConfig = FindFirst<GameConfig>();
            database = FindFirst<LevelDatabase>();
            if (paletteType == null && planetTypes.Count > 0)
                paletteType = planetTypes[0];
            zoom = 1f;
            zoomFocusRadius = 0f;
            ClearSelection();
            ClampSelection();
            endlessPreview = null;
            validation = null;
            Repaint();
        }

        private void ClampSelection()
        {
            if (database == null || database.levels == null || database.levels.Count == 0)
            {
                levelIndex = 0;
                ClearSelection();
                return;
            }
            levelIndex = Mathf.Clamp(levelIndex, 0, database.levels.Count - 1);
            LevelConfig level = database.levels[levelIndex];
            if (level == null || level.IsEndless || level.planets == null)
            {
                ClearSelection();
                return;
            }
            selectedPlanets.RemoveWhere(i => i < 0 || i >= level.planets.Count);
            if (!selectedPlanets.Contains(selectedPlanetIndex))
                selectedPlanetIndex = FirstSelected();
        }

        private void ClearSelection()
        {
            selectedPlanets.Clear();
            selectedPlanetIndex = -1;
        }

        private int FirstSelected()
        {
            foreach (int index in selectedPlanets)
                return index;
            return -1;
        }

        private bool IsSelected(int index)
        {
            return selectedPlanets.Contains(index);
        }

        private void SelectOnly(int index)
        {
            selectedPlanets.Clear();
            if (index >= 0)
                selectedPlanets.Add(index);
            selectedPlanetIndex = index;
        }

        private void ToggleSelection(int index)
        {
            if (index < 0)
                return;
            if (selectedPlanets.Contains(index))
            {
                selectedPlanets.Remove(index);
                if (selectedPlanetIndex == index)
                    selectedPlanetIndex = FirstSelected();
            }
            else
            {
                selectedPlanets.Add(index);
                selectedPlanetIndex = index;
            }
        }

        private void ResetPreviewView()
        {
            zoom = 1f;
            zoomFocusRadius = 0f;
            angleOffset = 0f;
            boxSelecting = false;
            ClearSelection();
            EndDrag();
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 132f;
            DrawToolbar();

            if (database == null)
            {
                EditorGUILayout.HelpBox(
                    "还没有 LevelDatabase。点击“创建默认关卡”生成 1/2/3 + 无尽四关的数据资产。",
                    MessageType.Info);
                if (GUILayout.Button("创建默认关卡", GUILayout.Height(32f)))
                    CreateDefaultLevels();
                return;
            }

            if (database.levels == null || database.levels.Count == 0)
            {
                EditorGUILayout.HelpBox("数据库里没有关卡。", MessageType.Warning);
                if (GUILayout.Button("创建默认关卡", GUILayout.Height(32f)))
                    CreateDefaultLevels();
                return;
            }

            ClampSelection();
            DrawLevelTabs();
            LevelConfig level = database.levels[levelIndex];
            if (level == null)
            {
                EditorGUILayout.HelpBox("当前关卡引用为空。", MessageType.Error);
                return;
            }

            GUILayout.BeginHorizontal();
            DrawLeftPanel(level);
            DrawCenterPanel(level);
            DrawRightPanel(level);
            GUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                RefreshAssets();
            if (GUILayout.Button("创建默认关卡", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                CreateDefaultLevels();
            GUILayout.FlexibleSpace();
            if (database != null)
                GUILayout.Label("数据库：" + database.name, EditorStyles.miniLabel);
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                SaveAll();
            GUILayout.EndHorizontal();
        }

        private void DrawLevelTabs()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < database.levels.Count; i++)
            {
                LevelConfig level = database.levels[i];
                if (level == null)
                    continue;
                Color old = GUI.backgroundColor;
                if (i == levelIndex)
                    GUI.backgroundColor = new Color(0.55f, 0.8f, 1f, 1f);
                string label = string.IsNullOrEmpty(level.displayName)
                    ? "Level " + (i + 1)
                    : level.displayName;
                if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    levelIndex = i;
                    ResetPreviewView();
                    endlessPreview = null;
                    validation = null;
                }
                GUI.backgroundColor = old;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                database.levels[levelIndex].IsEndless ? "模式：无尽（规则随机）" : "模式：固定（可手工改）",
                EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        private void DrawLeftPanel(LevelConfig level)
        {
            GUILayout.BeginVertical(GUILayout.Width(LeftWidth));
            GUILayout.Label("星球类型", EditorStyles.boldLabel);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll,
                GUILayout.ExpandHeight(true));

            for (int i = 0; i < planetTypes.Count; i++)
            {
                PlanetTypeDef type = planetTypes[i];
                if (type == null)
                    continue;
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = paletteType == type
                    ? new Color(0.55f, 0.8f, 1f, 1f)
                    : GetPreviewColor(type);
                if (GUILayout.Button(type.displayName, GUILayout.Height(26f)))
                    paletteType = type;
                GUI.backgroundColor = old;
            }

            EditorGUILayout.Space(8f);
            if (!level.IsEndless)
            {
                if (GUILayout.Button("添加星球", GUILayout.Height(28f)))
                    AddPlanet(level);
                GUILayout.Label("固定星球：" + level.planets.Count, EditorStyles.miniLabel);
                for (int i = 0; i < level.planets.Count; i++)
                {
                    LevelPlanetEntry entry = level.planets[i];
                    if (entry == null)
                        continue;
                    string typeName = entry.type != null ? entry.type.displayName : "空类型";
                    bool additive = Event.current.shift || Event.current.control;
                    Color old = GUI.backgroundColor;
                    if (IsSelected(i))
                        GUI.backgroundColor = new Color(0.55f, 0.8f, 1f, 1f);
                    string label = $"{i + 1}. {typeName}  r={entry.radius:F2}";
                    if (GUILayout.Button(label, GUILayout.Height(24f)))
                    {
                        if (additive)
                            ToggleSelection(i);
                        else
                            SelectOnly(i);
                        validation = null;
                    }
                    GUI.backgroundColor = old;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "无尽关使用规则和种子生成，预览不保存为固定列表。",
                    MessageType.None);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawCenterPanel(LevelConfig level)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            circularPreview = GUILayout.Toggle(circularPreview, "圆形预览",
                EditorStyles.toolbarButton, GUILayout.Width(84f));
            GUILayout.Label(
                circularPreview ? "半径向外 / 角度按屏幕坐标" : "半径向上 / 角度横向展开",
                EditorStyles.miniLabel);
            if (GUILayout.Button("缩放 " + zoom.ToString("0.0") + "x",
                EditorStyles.toolbarButton, GUILayout.Width(92f)))
            {
                zoom = 1f;
                zoomFocusRadius = 0f;
                angleOffset = 0f;
            }
            GUILayout.Label("滚轮缩放 / 边缘滚动条平移", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            Rect previewRect = GUILayoutUtility.GetRect(
                100f, 10000f, 220f, 10000f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawPreview(previewRect, level);

            GUILayout.BeginHorizontal();
            string generateLabel = level.IsEndless ? "生成预览" : "生成基线";
            if (GUILayout.Button(generateLabel, GUILayout.Height(30f)))
                Generate(level);
            if (GUILayout.Button("验证", GUILayout.Height(30f)))
                validation = LevelValidator.Validate(level, planetTypes, gameConfig);
            if (GUILayout.Button("保存", GUILayout.Height(30f)))
                SaveAll();
            GUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                level.IsEndless
                    ? "无尽关：点“生成预览”查看当前种子的一段布局；滚轮缩放，边缘滚动条平移。"
                    : "固定关：点击选中，空白处拖动框选，Shift/Ctrl 多选；拖动整组移动；Delete 删除。" +
                      "滚轮缩放，边缘滚动条平移。生成会覆盖当前固定列表。",
                MessageType.None);
            GUILayout.EndVertical();
        }

        private void DrawRightPanel(LevelConfig level)
        {
            GUILayout.BeginVertical(GUILayout.Width(RightWidth));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll,
                GUILayout.ExpandHeight(true));

            var so = new SerializedObject(level);
            so.Update();
            EditorGUILayout.LabelField("关卡属性", EditorStyles.boldLabel);
            Prop(so, "levelId", "关卡 ID");
            Prop(so, "displayName", "显示名称");
            Prop(so, "order", "顺序");
            Prop(so, "mode", "模式（Baked / Endless）");
            Prop(so, "unlockedByDefault", "默认解锁");
            Prop(so, "description", "关卡说明");
            Prop(so, "goalRadius", "目标高度");
            Prop(so, "startRadius", "起始高度");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("可用技能", EditorStyles.boldLabel);
            Prop(so, "allowMusou", "无双");
            Prop(so, "allowBomb", "炸弹技能");
            Prop(so, "allowHook", "钩锁");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("生成基线", EditorStyles.boldLabel);
            SerializedProperty generation = so.FindProperty("generation");
            if (generation != null)
                EditorGUILayout.PropertyField(generation, new GUIContent("生成参数"), true);
            SerializedProperty weights = so.FindProperty("typeWeights");
            if (weights != null)
                EditorGUILayout.PropertyField(weights, new GUIContent("类型权重"), true);

            if (level.IsEndless)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("无尽规则", EditorStyles.boldLabel);
                Prop(so, "endlessPreviewRadius", "编辑器预览高度");
                SerializedProperty endless = so.FindProperty("endless");
                if (endless != null)
                    EditorGUILayout.PropertyField(endless, new GUIContent("无尽参数"), true);
            }

            bool modified = so.hasModifiedProperties;
            if (modified)
                Undo.RecordObject(level, "Edit Level");
            bool applied = so.ApplyModifiedProperties();
            if (applied || modified)
            {
                validation = null;
                if (level.IsEndless)
                    endlessPreview = null;
                ClampSelection();
            }

            if (!level.IsEndless)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("选中星球", EditorStyles.boldLabel);
                DrawSelectedPlanet(level);
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("验证当前关卡", GUILayout.Height(28f)))
                validation = LevelValidator.Validate(level, planetTypes, gameConfig);

            if (validation != null)
            {
                if (validation.errors.Count == 0 && validation.warnings.Count == 0)
                {
                    EditorGUILayout.HelpBox("验证通过。", MessageType.Info);
                }
                for (int i = 0; i < validation.errors.Count; i++)
                    EditorGUILayout.HelpBox(validation.errors[i], MessageType.Error);
                for (int i = 0; i < validation.warnings.Count; i++)
                    EditorGUILayout.HelpBox(validation.warnings[i], MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void Prop(SerializedObject so, string field, string label)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private void DrawSelectedPlanet(LevelConfig level)
        {
            if (selectedPlanets.Count == 0)
            {
                EditorGUILayout.HelpBox("在左侧列表或预览中选中星球。Shift/Ctrl 可多选。",
                    MessageType.None);
                return;
            }

            if (selectedPlanets.Count > 1)
            {
                EditorGUILayout.HelpBox(
                    $"已选 {selectedPlanets.Count} 颗。可整体拖动，或批量删除/复制。",
                    MessageType.Info);
                DrawSelectionActions(level);
                return;
            }

            if (!IsSelected(selectedPlanetIndex))
                selectedPlanetIndex = FirstSelected();
            if (selectedPlanetIndex < 0 || selectedPlanetIndex >= level.planets.Count)
            {
                EditorGUILayout.HelpBox("选中项无效。", MessageType.Warning);
                return;
            }

            LevelPlanetEntry entry = level.planets[selectedPlanetIndex];
            if (entry == null)
            {
                EditorGUILayout.HelpBox("选中项为空。", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            PlanetTypeDef type = (PlanetTypeDef)EditorGUILayout.ObjectField(
                "类型", entry.type, typeof(PlanetTypeDef), false);
            float radius = EditorGUILayout.FloatField("半径", entry.radius);
            float angle = EditorGUILayout.Slider("角度", entry.angleDegrees, 0f, 360f);
            float size = EditorGUILayout.FloatField("尺寸倍率", entry.sizeMultiplier);
            float swayAmplitude = EditorGUILayout.FloatField("摆动幅度", entry.swayAmplitude);
            float swaySpeed = EditorGUILayout.FloatField("摆动速度", entry.swaySpeed);
            string note = EditorGUILayout.TextArea(entry.note, GUILayout.Height(44f));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(level, "Edit Level Planet");
                entry.type = type;
                entry.radius = Mathf.Max(0.01f, radius);
                entry.angleDegrees = Mathf.Repeat(angle, 360f);
                entry.sizeMultiplier = Mathf.Max(0.05f, size);
                entry.swayAmplitude = Mathf.Max(0f, swayAmplitude);
                entry.swaySpeed = Mathf.Max(0f, swaySpeed);
                entry.note = note;
                EditorUtility.SetDirty(level);
                validation = null;
            }

            DrawSelectionActions(level);
        }

        private void DrawSelectionActions(LevelConfig level)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("删除选中"))
                DeleteSelected(level);
            if (GUILayout.Button("复制选中"))
                DuplicateSelected(level);
            GUILayout.EndHorizontal();
        }

        private void DrawPreview(Rect rect, LevelConfig level)
        {
            EditorGUI.DrawRect(rect, new Color(0.09f, 0.11f, 0.16f, 1f));
            bool showBars = zoom > 1.001f;
            Rect viewRect = rect;
            if (showBars)
            {
                viewRect.width = Mathf.Max(20f, viewRect.width - ScrollbarSize);
                viewRect.height = Mathf.Max(20f, viewRect.height - ScrollbarSize);
            }

            Rect plot = new Rect(
                viewRect.x + PreviewPadding,
                viewRect.y + PreviewPadding,
                Mathf.Max(10f, viewRect.width - PreviewPadding * 2f),
                Mathf.Max(10f, viewRect.height - PreviewPadding * 2f));

            List<LevelPlanetEntry> display = level.IsEndless
                ? endlessPreview
                : level.planets;
            if (display == null)
                display = new List<LevelPlanetEntry>();

            float minRadius = Mathf.Max(0f,
                Mathf.Min(level.startRadius - level.generation.initialBelow, 0.1f) - 0.2f);
            float maxRadius = Mathf.Max(
                level.startRadius + 2f,
                level.IsEndless ? level.endlessPreviewRadius : level.goalRadius + 0.5f);
            for (int i = 0; i < display.Count; i++)
            {
                LevelPlanetEntry entry = display[i];
                if (entry == null)
                    continue;
                minRadius = Mathf.Min(minRadius, entry.radius - 0.4f);
                maxRadius = Mathf.Max(maxRadius, entry.radius + 0.4f);
            }
            minRadius = Mathf.Max(0f, minRadius);
            maxRadius = Mathf.Max(minRadius + 1f, maxRadius);
            float dataMin = minRadius;
            float dataMax = maxRadius;
            GetVisibleRadiusRange(minRadius, maxRadius, out float viewMin, out float viewMax);
            minRadius = viewMin;
            maxRadius = viewMax;

            if (circularPreview)
            {
                DrawCircularPreview(viewRect, level, display, minRadius, maxRadius);
                DrawSelectionBox(viewRect);
                DrawViewScrollbars(rect, viewRect, dataMin, dataMax,
                    minRadius, maxRadius);
                return;
            }

            float lane = Mathf.Max(0.05f, level.generation.laneSpacing);
            for (float radius = Mathf.Ceil(minRadius / lane) * lane;
                 radius <= maxRadius; radius += lane)
            {
                float y = YFromRadius(plot, minRadius, maxRadius, radius);
                EditorGUI.DrawRect(
                    new Rect(plot.x, y, plot.width, 1f),
                    new Color(0.24f, 0.3f, 0.4f, 0.4f));
                if (Mathf.Abs(radius - Mathf.Round(radius)) < 0.001f)
                {
                    GUI.Label(new Rect(plot.x + 3f, y - 8f, 54f, 16f),
                        radius.ToString("0"), EditorStyles.miniLabel);
                }
            }

            if (level.goalRadius > level.startRadius && !level.IsEndless &&
                level.goalRadius >= minRadius && level.goalRadius <= maxRadius)
            {
                float goalY = YFromRadius(plot, minRadius, maxRadius, level.goalRadius);
                EditorGUI.DrawRect(
                    new Rect(plot.x, goalY, plot.width, 2f),
                    new Color(0.35f, 1f, 0.55f, 0.8f));
                GUI.Label(new Rect(plot.x + 4f, goalY - 18f, 110f, 18f),
                    "目标高度 " + level.goalRadius.ToString("0.##"),
                    EditorStyles.boldLabel);
            }

            if (level.startRadius >= minRadius && level.startRadius <= maxRadius)
            {
                float startY = YFromRadius(plot, minRadius, maxRadius, level.startRadius);
                EditorGUI.DrawRect(
                    new Rect(plot.x, startY, plot.width, 2f),
                    new Color(0.35f, 0.8f, 1f, 0.8f));
                GUI.Label(new Rect(plot.x + 4f, startY + 2f, 110f, 18f),
                    "起始高度 " + level.startRadius.ToString("0.##"),
                    EditorStyles.miniLabel);
            }

            if (level.IsEndless && (endlessPreview == null || endlessPreview.Count == 0))
            {
                GUI.Label(new Rect(plot.x + 10f, plot.y + 10f, plot.width - 20f, 24f),
                    "无尽关：点下方“生成预览”查看当前种子的一段布局。",
                    EditorStyles.boldLabel);
            }
            else if (!level.IsEndless && level.planets.Count == 0)
            {
                GUI.Label(new Rect(plot.x + 10f, plot.y + 10f, plot.width - 20f, 24f),
                    "固定关还没有星球，点下方“生成基线”。",
                    EditorStyles.boldLabel);
            }

            for (int i = 0; i < display.Count; i++)
            {
                LevelPlanetEntry entry = display[i];
                if (entry == null || entry.radius < minRadius || entry.radius > maxRadius)
                    continue;
                Vector2 point = PlanetPoint(plot, minRadius, maxRadius, entry,
                    angleOffset);
                float worldRadius = entry.type != null
                    ? entry.type.baseSize *
                      (gameConfig != null ? gameConfig.sizeScale : 0.5f) *
                      Mathf.Max(0.05f, entry.sizeMultiplier)
                    : 0.06f;
                float pixelRadius = Mathf.Clamp(
                    worldRadius / Mathf.Max(0.01f, maxRadius - minRadius) *
                    plot.height * 2f, 4f, 14f);
                bool selected = !level.IsEndless && IsSelected(i);

                if (selected)
                {
                    DrawDisc(new Rect(
                        point.x - pixelRadius - 3f,
                        point.y - pixelRadius - 3f,
                        (pixelRadius + 3f) * 2f,
                        (pixelRadius + 3f) * 2f),
                        new Color(1f, 1f, 1f, 0.9f));
                }

                Color color = GetPreviewColor(entry.type);
                DrawDisc(new Rect(
                    point.x - pixelRadius,
                    point.y - pixelRadius,
                    pixelRadius * 2f,
                    pixelRadius * 2f), color);
            }

            DrawSelectionBox(viewRect);
            HandlePreviewInput(viewRect, plot, level, minRadius, maxRadius);
            DrawViewScrollbars(rect, viewRect, dataMin, dataMax,
                minRadius, maxRadius);
        }

        private void DrawCircularPreview(Rect rect, LevelConfig level,
            List<LevelPlanetEntry> display, float minRadius, float maxRadius)
        {
            float maxPixelRadius = Mathf.Max(10f,
                Mathf.Min(rect.width, rect.height) * 0.5f - PreviewPadding);
            Vector2 center = rect.center;
            float lane = Mathf.Max(0.05f, level.generation.laneSpacing);

            for (float radius = Mathf.Ceil(minRadius / lane) * lane;
                 radius <= maxRadius; radius += lane)
            {
                float pixel = RadiusToPixel(minRadius, maxRadius, maxPixelRadius, radius);
                DrawRing(center, pixel, new Color(0.26f, 0.33f, 0.44f, 0.45f));
                if (Mathf.Abs(radius - Mathf.Round(radius)) < 0.001f)
                {
                    GUI.Label(new Rect(center.x + pixel + 3f, center.y - 8f, 42f, 16f),
                        radius.ToString("0"), EditorStyles.miniLabel);
                }
            }

            if (!level.IsEndless && level.goalRadius > level.startRadius &&
                level.goalRadius >= minRadius && level.goalRadius <= maxRadius)
            {
                float goalPixel = RadiusToPixel(
                    minRadius, maxRadius, maxPixelRadius, level.goalRadius);
                DrawRing(center, goalPixel, new Color(0.35f, 1f, 0.55f, 0.9f));
                GUI.Label(new Rect(center.x - 60f, center.y - goalPixel - 22f, 120f, 18f),
                    "目标 " + level.goalRadius.ToString("0.##"),
                    EditorStyles.boldLabel);
            }

            if (level.startRadius >= minRadius && level.startRadius <= maxRadius)
            {
                float startPixel = RadiusToPixel(
                    minRadius, maxRadius, maxPixelRadius, level.startRadius);
                DrawRing(center, startPixel, new Color(0.35f, 0.8f, 1f, 0.9f));
                GUI.Label(new Rect(center.x + startPixel + 4f, center.y + 2f, 110f, 18f),
                    "起始 " + level.startRadius.ToString("0.##"),
                    EditorStyles.miniLabel);
            }

            DrawDisc(new Rect(center.x - 3f, center.y - 3f, 6f, 6f),
                new Color(0.04f, 0.05f, 0.08f, 1f));

            if (level.IsEndless && (endlessPreview == null || endlessPreview.Count == 0))
            {
                GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 22f),
                    "无尽关：点下方“生成预览”查看当前种子的一段布局。",
                    EditorStyles.boldLabel);
            }
            else if (!level.IsEndless && level.planets.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 22f),
                    "固定关还没有星球，点下方“生成基线”。",
                    EditorStyles.boldLabel);
            }

            for (int i = 0; i < display.Count; i++)
            {
                LevelPlanetEntry entry = display[i];
                if (entry == null || entry.radius < minRadius || entry.radius > maxRadius)
                    continue;
                Vector2 point = CircularPlanetPoint(
                    center, maxPixelRadius, minRadius, maxRadius, entry, angleOffset);
                float worldRadius = entry.type != null
                    ? entry.type.baseSize *
                      (gameConfig != null ? gameConfig.sizeScale : 0.5f) *
                      Mathf.Max(0.05f, entry.sizeMultiplier)
                    : 0.06f;
                float pixelRadius = Mathf.Clamp(
                    worldRadius / Mathf.Max(0.01f, maxRadius - minRadius) *
                    maxPixelRadius * 3f, 5f, 15f);
                bool selected = !level.IsEndless && IsSelected(i);

                if (selected)
                {
                    DrawDisc(new Rect(
                        point.x - pixelRadius - 3f,
                        point.y - pixelRadius - 3f,
                        (pixelRadius + 3f) * 2f,
                        (pixelRadius + 3f) * 2f),
                        new Color(1f, 1f, 1f, 0.95f));
                }

                DrawDisc(new Rect(
                    point.x - pixelRadius,
                    point.y - pixelRadius,
                    pixelRadius * 2f,
                    pixelRadius * 2f),
                    GetPreviewColor(entry.type));
            }

            HandleCircularInput(rect, center, maxPixelRadius, minRadius, maxRadius, level);
        }

        private void GetVisibleRadiusRange(float dataMin, float dataMax,
            out float viewMin, out float viewMax)
        {
            if (zoom <= 1.0001f)
            {
                viewMin = dataMin;
                viewMax = dataMax;
                return;
            }

            float span = Mathf.Max(0.5f, (dataMax - dataMin) / zoom);
            float center = Mathf.Clamp(zoomFocusRadius, dataMin, dataMax);
            viewMin = center - span * 0.5f;
            viewMax = center + span * 0.5f;

            if (viewMin < dataMin)
            {
                viewMax += dataMin - viewMin;
                viewMin = dataMin;
            }
            if (viewMax > dataMax)
            {
                viewMin -= viewMax - dataMax;
                viewMax = dataMax;
            }
            viewMin = Mathf.Max(0f, viewMin);
            if (viewMax - viewMin < 0.2f)
                viewMax = viewMin + 0.2f;
        }

        private void BeginDrag(LevelConfig level, float mouseRadius, float mouseAngle)
        {
            dragSeeds.Clear();
            foreach (int index in selectedPlanets)
            {
                if (index < 0 || index >= level.planets.Count)
                    continue;
                LevelPlanetEntry entry = level.planets[index];
                if (entry == null)
                    continue;
                dragSeeds.Add(new Vector3(index, entry.radius, entry.angleDegrees));
            }
            dragStartRadius = mouseRadius;
            dragStartAngle = mouseAngle;
            dragUndoRecorded = false;
            draggingPlanet = dragSeeds.Count > 0;
        }

        private void ApplyDrag(LevelConfig level, float mouseRadius, float mouseAngle,
            float minRadius, float maxRadius)
        {
            if (!draggingPlanet || dragSeeds.Count == 0)
                return;
            if (!dragUndoRecorded)
            {
                Undo.RecordObject(level, "Move Level Planets");
                dragUndoRecorded = true;
            }

            float deltaRadius = mouseRadius - dragStartRadius;
            float deltaAngle = Mathf.DeltaAngle(dragStartAngle, mouseAngle);
            float minEditRadius = Mathf.Max(0.01f,
                level.generation.minGenerationRadius);
            float maxEditRadius = Mathf.Max(minEditRadius + 0.01f, maxRadius);
            for (int i = 0; i < dragSeeds.Count; i++)
            {
                Vector3 seed = dragSeeds[i];
                int index = (int)seed.x;
                if (index < 0 || index >= level.planets.Count)
                    continue;
                LevelPlanetEntry entry = level.planets[index];
                if (entry == null)
                    continue;
                entry.radius = Mathf.Clamp(
                    seed.y + deltaRadius, minEditRadius, maxEditRadius);
                entry.angleDegrees = Mathf.Repeat(
                    seed.z + deltaAngle, 360f);
            }
            EditorUtility.SetDirty(level);
            validation = null;
        }

        private void EndDrag()
        {
            draggingPlanet = false;
            dragSeeds.Clear();
            dragUndoRecorded = false;
        }

        private void BeginBoxSelection(Vector2 start, bool additive)
        {
            boxSelecting = true;
            boxStart = start;
            boxEnd = start;
            boxAdditive = additive;
            EndDrag();
        }

        private void DrawSelectionBox(Rect viewRect)
        {
            if (!boxSelecting)
                return;

            Rect box = Rect.MinMaxRect(
                Mathf.Clamp(Mathf.Min(boxStart.x, boxEnd.x), viewRect.xMin, viewRect.xMax),
                Mathf.Clamp(Mathf.Min(boxStart.y, boxEnd.y), viewRect.yMin, viewRect.yMax),
                Mathf.Clamp(Mathf.Max(boxStart.x, boxEnd.x), viewRect.xMin, viewRect.xMax),
                Mathf.Clamp(Mathf.Max(boxStart.y, boxEnd.y), viewRect.yMin, viewRect.yMax));
            if (box.width < 1f || box.height < 1f)
                return;

            EditorGUI.DrawRect(box, new Color(0.35f, 0.75f, 1f, 0.16f));
            Color border = new Color(0.4f, 0.8f, 1f, 0.9f);
            EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1f), border);
            EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1f, box.width, 1f), border);
            EditorGUI.DrawRect(new Rect(box.x, box.y, 1f, box.height), border);
            EditorGUI.DrawRect(new Rect(box.xMax - 1f, box.y, 1f, box.height), border);
        }

        private void ApplyCircularBoxSelection(LevelConfig level, Vector2 center,
            float maxPixelRadius, float minRadius, float maxRadius, bool additive)
        {
            Rect box = Rect.MinMaxRect(
                Mathf.Min(boxStart.x, boxEnd.x), Mathf.Min(boxStart.y, boxEnd.y),
                Mathf.Max(boxStart.x, boxEnd.x), Mathf.Max(boxStart.y, boxEnd.y));
            if (box.width < 3f && box.height < 3f)
            {
                if (!additive)
                    ClearSelection();
                return;
            }

            if (!additive)
                ClearSelection();
            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry entry = level.planets[i];
                if (entry == null)
                    continue;
                Vector2 point = CircularPlanetPoint(
                    center, maxPixelRadius, minRadius, maxRadius, entry, angleOffset);
                if (box.Contains(point))
                {
                    selectedPlanets.Add(i);
                    selectedPlanetIndex = i;
                }
            }
            validation = null;
        }

        private void ApplyUnwrappedBoxSelection(LevelConfig level, Rect plot,
            float minRadius, float maxRadius, bool additive)
        {
            Rect box = Rect.MinMaxRect(
                Mathf.Min(boxStart.x, boxEnd.x), Mathf.Min(boxStart.y, boxEnd.y),
                Mathf.Max(boxStart.x, boxEnd.x), Mathf.Max(boxStart.y, boxEnd.y));
            if (box.width < 3f && box.height < 3f)
            {
                if (!additive)
                    ClearSelection();
                return;
            }

            if (!additive)
                ClearSelection();
            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry entry = level.planets[i];
                if (entry == null)
                    continue;
                Vector2 point = PlanetPoint(plot, minRadius, maxRadius, entry, angleOffset);
                if (box.Contains(point))
                {
                    selectedPlanets.Add(i);
                    selectedPlanetIndex = i;
                }
            }
            validation = null;
        }

        private void DrawViewScrollbars(Rect outer, Rect view,
            float dataMin, float dataMax, float visibleMin, float visibleMax)
        {
            if (zoom <= 1.001f)
                return;

            float barSize = Mathf.Min(ScrollbarSize, Mathf.Min(outer.width, outer.height) * 0.25f);
            if (barSize < 4f)
                return;

            float horizontal = GUI.HorizontalScrollbar(
                new Rect(outer.x, view.yMax, view.width, barSize),
                angleOffset, 45f, 0f, 360f);
            if (!Mathf.Approximately(horizontal, angleOffset))
                angleOffset = Mathf.Repeat(horizontal, 360f);

            float span = Mathf.Max(0.01f, visibleMax - visibleMin);
            float vertical = GUI.VerticalScrollbar(
                new Rect(view.xMax, outer.y, barSize, view.height),
                visibleMin, span, dataMin, Mathf.Max(dataMin + 0.01f, dataMax));
            if (!Mathf.Approximately(vertical, visibleMin))
            {
                zoomFocusRadius = Mathf.Clamp(
                    vertical + span * 0.5f, dataMin, dataMax);
            }
        }

        private void HandleCircularInput(Rect rect, Vector2 center,
            float maxPixelRadius, float minRadius, float maxRadius, LevelConfig level)
        {
            Event current = Event.current;
            Vector2 mouse = current.mousePosition;
            if (current.type == EventType.ScrollWheel && rect.Contains(mouse))
            {
                float pixel = (mouse - center).magnitude;
                float focus = Mathf.Lerp(minRadius, maxRadius,
                    Mathf.Clamp01(pixel / Mathf.Max(1f, maxPixelRadius)));
                zoomFocusRadius = focus;
                zoom = Mathf.Clamp(zoom * (current.delta.y < 0f ? 1.15f : 0.87f),
                    1f, 8f);
                if (zoom <= 1.0001f)
                {
                    zoom = 1f;
                    zoomFocusRadius = 0f;
                    angleOffset = 0f;
                }
                current.Use();
                Repaint();
                return;
            }

            if (level.IsEndless)
                return;

            if (current.type == EventType.MouseDown && current.button == 0 &&
                rect.Contains(mouse))
            {
                int hit = FindCircularPlanetAt(
                    level, center, maxPixelRadius, minRadius, maxRadius, mouse);
                bool additive = current.shift || current.control;
                if (hit >= 0)
                {
                    boxSelecting = false;
                    if (additive)
                    {
                        ToggleSelection(hit);
                        EndDrag();
                    }
                    else
                    {
                        if (!IsSelected(hit))
                            SelectOnly(hit);
                        else
                            selectedPlanetIndex = hit;

                        Vector2 delta = mouse - center;
                        float mouseRadius = Mathf.Lerp(minRadius, maxRadius,
                            Mathf.Clamp01(delta.magnitude /
                                Mathf.Max(1f, maxPixelRadius)));
                        float mouseAngle = Mathf.Atan2(
                            center.y - mouse.y, mouse.x - center.x) * Mathf.Rad2Deg -
                            angleOffset;
                        BeginDrag(level, mouseRadius, mouseAngle);
                    }
                }
                else
                {
                    BeginBoxSelection(mouse, additive);
                }
                validation = null;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseDrag && boxSelecting)
            {
                boxEnd = mouse;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseDrag && draggingPlanet)
            {
                Vector2 delta = mouse - center;
                float mouseRadius = Mathf.Lerp(minRadius, maxRadius,
                    Mathf.Clamp01(delta.magnitude / Mathf.Max(1f, maxPixelRadius)));
                float mouseAngle = Mathf.Atan2(
                    center.y - mouse.y, mouse.x - center.x) * Mathf.Rad2Deg -
                    angleOffset;
                ApplyDrag(level, mouseRadius, mouseAngle, minRadius, maxRadius);
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && boxSelecting)
            {
                ApplyCircularBoxSelection(level, center, maxPixelRadius,
                    minRadius, maxRadius, boxAdditive);
                boxSelecting = false;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && draggingPlanet)
            {
                EndDrag();
                current.Use();
                return;
            }

            if (current.type == EventType.KeyDown &&
                GUIUtility.keyboardControl == 0 &&
                (current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace) &&
                selectedPlanets.Count > 0)
            {
                DeleteSelected(level);
                current.Use();
                Repaint();
            }
        }

        private int FindCircularPlanetAt(LevelConfig level, Vector2 center,
            float maxPixelRadius, float minRadius, float maxRadius, Vector2 mouse)
        {
            int best = -1;
            float bestDistance = 12f;
            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry entry = level.planets[i];
                if (entry == null)
                    continue;
                Vector2 point = CircularPlanetPoint(
                    center, maxPixelRadius, minRadius, maxRadius, entry, angleOffset);
                float distance = Vector2.Distance(point, mouse);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private static Vector2 CircularPlanetPoint(Vector2 center, float maxPixelRadius,
            float minRadius, float maxRadius, LevelPlanetEntry entry, float angleOffset)
        {
            float pixel = RadiusToPixel(
                minRadius, maxRadius, maxPixelRadius, entry.radius);
            float angle = (entry.angleDegrees + angleOffset) * Mathf.Deg2Rad;
            return new Vector2(
                center.x + Mathf.Cos(angle) * pixel,
                center.y - Mathf.Sin(angle) * pixel);
        }

        private static float RadiusToPixel(float minRadius, float maxRadius,
            float maxPixelRadius, float radius)
        {
            return Mathf.InverseLerp(minRadius, maxRadius, radius) * maxPixelRadius;
        }

        private void DrawRing(Vector2 center, float pixelRadius, Color color)
        {
            if (ringTexture == null || pixelRadius < 1f)
                return;
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(
                center.x - pixelRadius,
                center.y - pixelRadius,
                pixelRadius * 2f,
                pixelRadius * 2f),
                ringTexture, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        private static Color GetPreviewColor(PlanetTypeDef type)
        {
            if (type == null)
                return Color.magenta;

            switch (type.id)
            {
                case PlanetTypeId.Rock:
                    return new Color(0.62f, 0.74f, 0.88f, 1f);
                case PlanetTypeId.Bomb:
                    return new Color(1f, 0.55f, 0.22f, 1f);
                case PlanetTypeId.Cloud:
                    return new Color(0.86f, 0.94f, 1f, 1f);
                case PlanetTypeId.Ice:
                    return new Color(0.42f, 0.84f, 1f, 1f);
                case PlanetTypeId.Shiny:
                    return new Color(1f, 0.84f, 0.3f, 1f);
                default:
                    return type.color.a > 0f ? type.color : Color.white;
            }
        }

        private void HandlePreviewInput(Rect rect, Rect plot, LevelConfig level,
            float minRadius, float maxRadius)
        {
            Event current = Event.current;
            Vector2 mouse = current.mousePosition;
            if (current.type == EventType.ScrollWheel && rect.Contains(mouse))
            {
                zoomFocusRadius = RadiusFromY(
                    plot, minRadius, maxRadius, mouse.y);
                zoom = Mathf.Clamp(zoom * (current.delta.y < 0f ? 1.15f : 0.87f),
                    1f, 8f);
                if (zoom <= 1.0001f)
                {
                    zoom = 1f;
                    zoomFocusRadius = 0f;
                    angleOffset = 0f;
                }
                current.Use();
                Repaint();
                return;
            }

            if (level.IsEndless)
                return;

            if (current.type == EventType.MouseDown && current.button == 0 &&
                plot.Contains(mouse))
            {
                int hit = FindPlanetAt(level, plot, minRadius, maxRadius, mouse);
                bool additive = current.shift || current.control;
                if (hit >= 0)
                {
                    boxSelecting = false;
                    if (additive)
                    {
                        ToggleSelection(hit);
                        EndDrag();
                    }
                    else
                    {
                        if (!IsSelected(hit))
                            SelectOnly(hit);
                        else
                            selectedPlanetIndex = hit;
                        BeginDrag(level,
                            RadiusFromY(plot, minRadius, maxRadius, mouse.y),
                            AngleFromX(plot, mouse.x) - angleOffset);
                    }
                }
                else
                {
                    BeginBoxSelection(mouse, additive);
                }
                validation = null;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseDrag && boxSelecting)
            {
                boxEnd = mouse;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseDrag && draggingPlanet)
            {
                ApplyDrag(level,
                    RadiusFromY(plot, minRadius, maxRadius, mouse.y),
                    AngleFromX(plot, mouse.x) - angleOffset,
                    minRadius, maxRadius);
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && boxSelecting)
            {
                ApplyUnwrappedBoxSelection(level, plot, minRadius, maxRadius,
                    boxAdditive);
                boxSelecting = false;
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && draggingPlanet)
            {
                EndDrag();
                current.Use();
                return;
            }

            if (current.type == EventType.KeyDown &&
                GUIUtility.keyboardControl == 0 &&
                (current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace) &&
                selectedPlanets.Count > 0)
            {
                DeleteSelected(level);
                current.Use();
                Repaint();
            }
        }

        private int FindPlanetAt(LevelConfig level, Rect plot,
            float minRadius, float maxRadius, Vector2 mouse)
        {
            int best = -1;
            float bestDistance = 12f;
            for (int i = 0; i < level.planets.Count; i++)
            {
                LevelPlanetEntry entry = level.planets[i];
                if (entry == null)
                    continue;
                Vector2 point = PlanetPoint(plot, minRadius, maxRadius, entry, angleOffset);
                float distance = Vector2.Distance(point, mouse);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private static Vector2 PlanetPoint(Rect plot, float minRadius,
            float maxRadius, LevelPlanetEntry entry, float angleOffset)
        {
            return new Vector2(
                plot.x + Mathf.Repeat(entry.angleDegrees + angleOffset, 360f) /
                360f * plot.width,
                YFromRadius(plot, minRadius, maxRadius, entry.radius));
        }

        private static float YFromRadius(Rect plot, float minRadius,
            float maxRadius, float radius)
        {
            float t = Mathf.InverseLerp(minRadius, maxRadius, radius);
            return Mathf.Lerp(plot.yMax, plot.y, t);
        }

        private static float RadiusFromY(Rect plot, float minRadius,
            float maxRadius, float y)
        {
            float t = Mathf.InverseLerp(plot.yMax, plot.y, y);
            return Mathf.Lerp(minRadius, maxRadius, t);
        }

        private static float AngleFromX(Rect plot, float x)
        {
            return Mathf.Repeat((x - plot.x) / Mathf.Max(1f, plot.width) * 360f, 360f);
        }

        private void DrawDisc(Rect rect, Color color)
        {
            if (circleTexture == null)
                return;
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        private static Texture2D CreateCircleTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(Mathf.Clamp01((radius - distance) / 1.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRingTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 2f;
            const float thickness = 1.8f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(Mathf.Clamp01(
                        1f - Mathf.Abs(distance - radius) / thickness) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private void AddPlanet(LevelConfig level)
        {
            PlanetTypeDef type = paletteType;
            if (type == null && planetTypes.Count > 0)
                type = planetTypes[0];

            Undo.RecordObject(level, "Add Level Planet");
            level.planets.Add(new LevelPlanetEntry
            {
                type = type,
                radius = level.startRadius + 0.8f,
                angleDegrees = 0f,
                sizeMultiplier = 1f
            });
            SelectOnly(level.planets.Count - 1);
            EditorUtility.SetDirty(level);
            validation = null;
        }

        private void DeleteSelected(LevelConfig level)
        {
            if (selectedPlanets.Count == 0)
                return;

            Undo.RecordObject(level, "Delete Level Planets");
            var indices = new List<int>(selectedPlanets);
            indices.Sort((a, b) => b.CompareTo(a));
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index >= 0 && index < level.planets.Count)
                    level.planets.RemoveAt(index);
            }
            ClearSelection();
            EditorUtility.SetDirty(level);
            validation = null;
        }

        private void DuplicateSelected(LevelConfig level)
        {
            if (selectedPlanets.Count == 0)
                return;

            Undo.RecordObject(level, "Duplicate Level Planets");
            var indices = new List<int>(selectedPlanets);
            indices.Sort();
            var newIndices = new List<int>();
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index < 0 || index >= level.planets.Count)
                    continue;
                LevelPlanetEntry source = level.planets[index];
                if (source == null)
                    continue;

                level.planets.Add(new LevelPlanetEntry
                {
                    type = source.type,
                    radius = source.radius,
                    angleDegrees = Mathf.Repeat(source.angleDegrees + 8f, 360f),
                    sizeMultiplier = source.sizeMultiplier,
                    swayAmplitude = source.swayAmplitude,
                    swaySpeed = source.swaySpeed,
                    swayPhase = source.swayPhase,
                    note = source.note
                });
                newIndices.Add(level.planets.Count - 1);
            }

            ClearSelection();
            for (int i = 0; i < newIndices.Count; i++)
                selectedPlanets.Add(newIndices[i]);
            selectedPlanetIndex = newIndices.Count > 0
                ? newIndices[newIndices.Count - 1]
                : -1;
            EditorUtility.SetDirty(level);
            validation = null;
        }

        private void Generate(LevelConfig level)
        {
            if (level.IsEndless)
            {
                endlessPreview = LevelLayoutBuilder.Generate(
                    level, planetTypes, level.endlessPreviewRadius);
                ResetPreviewView();
                validation = null;
                return;
            }

            if (level.planets.Count > 0 &&
                !EditorUtility.DisplayDialog(
                    "重新生成",
                    "会覆盖当前固定星球列表，手工修改会丢失。继续？",
                    "生成", "取消"))
            {
                return;
            }

            Undo.RecordObject(level, "Generate Level Planets");
            level.planets = LevelLayoutBuilder.Generate(level, planetTypes);
            ResetPreviewView();
            EditorUtility.SetDirty(level);
            validation = null;
        }

        private void SaveAll()
        {
            if (database != null)
            {
                EditorUtility.SetDirty(database);
                for (int i = 0; i < database.levels.Count; i++)
                {
                    if (database.levels[i] != null)
                        EditorUtility.SetDirty(database.levels[i]);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateDefaultLevels()
        {
            if (AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath) != null)
            {
                RefreshAssets();
                return;
            }

            if (!AssetDatabase.IsValidFolder(LevelsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");
            }

            LevelDatabase db = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
            db.levels = new List<LevelConfig>();

            for (int i = 1; i <= 3; i++)
            {
                LevelConfig level = CreateBakedLevel(i);
                db.levels.Add(level);
            }
            db.levels.Add(CreateEndlessLevel());
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            database = db;
            levelIndex = 0;
            ResetPreviewView();
            validation = null;
            endlessPreview = null;
            Repaint();
        }

        private LevelConfig CreateBakedLevel(int index)
        {
            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            level.levelId = "level_" + index;
            level.displayName = "第" + index + "关";
            level.order = index;
            level.mode = LevelMode.Baked;
            level.goalRadius = 11f;
            level.unlockedByDefault = true;
            level.CopyGenerationFrom(gameConfig);
            FillDefaultTypeWeights(level);
            level.planets = LevelLayoutBuilder.Generate(level, planetTypes);

            string path = $"{LevelsFolder}/Level_{index}.asset";
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        private LevelConfig CreateEndlessLevel()
        {
            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            level.levelId = "endless";
            level.displayName = "无尽";
            level.order = 4;
            level.mode = LevelMode.Endless;
            level.goalRadius = 11f;
            level.unlockedByDefault = true;
            level.CopyGenerationFrom(gameConfig);
            FillDefaultTypeWeights(level);

            string path = LevelsFolder + "/Endless.asset";
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        private void FillDefaultTypeWeights(LevelConfig level)
        {
            level.typeWeights.Clear();
            for (int i = 0; i < planetTypes.Count; i++)
            {
                PlanetTypeDef type = planetTypes[i];
                if (type == null)
                    continue;
                level.typeWeights.Add(new LevelTypeWeight
                {
                    type = type,
                    weight = Mathf.Max(0f, type.spawnWeight)
                });
            }
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var result = new List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    result.Add(asset);
            }
            return result;
        }

        private static T FindFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<T>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
