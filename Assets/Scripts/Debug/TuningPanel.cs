using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BouncingPlanet
{
    /// <summary>
    /// T 键调参面板。用 [Tunable] 自动生成滑条，避免每加一个参数就要改 UI。
    /// 运行时改动只在本次 Play 生效；编辑器下可点“Save Assets”写回资产。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TuningPanel : MonoBehaviour
    {
        private sealed class FieldSpec
        {
            public FieldInfo field;
            public TunableAttribute attr;
        }

        private static readonly Dictionary<Type, FieldSpec[]> fieldCache =
            new Dictionary<Type, FieldSpec[]>();

        public GameBootstrap bootstrap;
        [SerializeField] private bool visibleOnStart = false;

        private bool visible;
        private int tab;
        private int selectedPlanet;
        private Vector2 scroll;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle valueStyle;
        private GUIStyle noteStyle;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle sliderStyle;
        private GUIStyle thumbStyle;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap;
            visible = visibleOnStart;
        }

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
        }

        private void Update()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;

            if (Input.GetKeyDown(KeyCode.T))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || bootstrap == null || bootstrap.gameConfig == null)
                return;

            EnsureStyles();
            float width = Mathf.Min(Screen.width - 16f, Mathf.Clamp(Screen.width * 0.52f, 700f, 940f));
            Rect window = new Rect(
                Mathf.Max(8f, Screen.width - width - 8f),
                8f,
                width,
                Mathf.Max(320f, Screen.height - 16f));

            GUILayout.Window(GetInstanceID(), window, DrawWindow, "Initial Demo v1.0 · Tuning (T)");
        }

        private void DrawWindow(int windowId)
        {
            GameConfig config = bootstrap.gameConfig;
            SimulationClock clock = bootstrap.clock;

            GUILayout.BeginVertical();
            GUILayout.Label("Initial Demo v1.0 · 调参面板", titleStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(tab == 0, "游戏", buttonStyle, GUILayout.Height(38f)))
                tab = 0;
            if (GUILayout.Toggle(tab == 1, "反馈", buttonStyle, GUILayout.Height(38f)))
                tab = 1;
            if (GUILayout.Toggle(tab == 2, "星球", buttonStyle, GUILayout.Height(38f)))
                tab = 2;
            GUILayout.EndHorizontal();

            if (clock != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(clock.IsPaused ? "继续" : "暂停", buttonStyle, GUILayout.Height(38f)))
                    clock.TogglePause();
                if (GUILayout.Button("单步", buttonStyle, GUILayout.Height(38f)))
                    clock.StepOnce();
                GUILayout.Label("慢放", labelStyle, GUILayout.Width(58f));
                if (GUILayout.Button("1x", buttonStyle, GUILayout.Height(38f)))
                    clock.SetSlowMotion(1f);
                if (GUILayout.Button("0.25x", buttonStyle, GUILayout.Height(38f)))
                    clock.SetSlowMotion(0.25f);
                if (GUILayout.Button("0.1x", buttonStyle, GUILayout.Height(38f)))
                    clock.SetSlowMotion(0.1f);
                GUILayout.EndHorizontal();
            }

            if (bootstrap.player != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("重置玩家", buttonStyle, GUILayout.Height(38f)))
                    bootstrap.player.Respawn();
                bool hold = GUILayout.Toggle(bootstrap.player.IsHoldingRadius,
                    "  锁定初始半径 (H)", labelStyle, GUILayout.Height(38f));
                if (hold != bootstrap.player.IsHoldingRadius)
                    bootstrap.player.SetHoldRadius(hold);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("重置游戏", buttonStyle, GUILayout.Height(38f)))
            {
                config.ResetToDefaults();
                MarkDirty(config);
            }
            if (GUILayout.Button("重置反馈", buttonStyle, GUILayout.Height(38f)) && bootstrap.feedbackConfig != null)
            {
                bootstrap.feedbackConfig.ResetToDefaults();
                MarkDirty(bootstrap.feedbackConfig);
            }
            if (bootstrap.score != null &&
                GUILayout.Button("重置分数", buttonStyle, GUILayout.Height(38f)))
            {
                bootstrap.score.ResetScore();
            }
            if (bootstrap.planetWorld != null &&
                GUILayout.Button("重建世界", buttonStyle, GUILayout.Height(38f)))
            {
                bootstrap.planetWorld.Rebuild();
            }
#if UNITY_EDITOR
            if (GUILayout.Button("保存资产", buttonStyle, GUILayout.Height(38f)))
                SaveAssets();
#endif
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll, false, true, GUILayout.ExpandHeight(true));

            switch (tab)
            {
                case 1:
                    if (bootstrap.feedbackConfig != null)
                        DrawTunableObject(bootstrap.feedbackConfig);
                    else
                        GUILayout.Label("缺少 FeedbackConfig。", labelStyle);
                    break;

                case 2:
                    DrawPlanetTab();
                    break;

                default:
                    GUILayout.Label($"规则口径：{config.profile}（Stage 0 固定）", headerStyle);
                    DrawTunableObject(config);
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.Label("打开面板不会改数值；只有拖动滑条/切换开关才会写入。运行时改动只在本次 Play 生效，编辑器下可点“保存资产”。",
                noteStyle);
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 26f));
        }

        private void DrawPlanetTab()
        {
            PlanetTypeDef[] types = bootstrap.planetTypes;
            if (types == null || types.Length == 0)
            {
                GUILayout.Label("没有指定 PlanetTypeDef。", labelStyle);
                return;
            }

            selectedPlanet = Mathf.Clamp(selectedPlanet, 0, types.Length - 1);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", buttonStyle, GUILayout.Width(52f), GUILayout.Height(40f)))
                selectedPlanet = (selectedPlanet - 1 + types.Length) % types.Length;
            PlanetTypeDef selected = types[selectedPlanet];
            GUILayout.Label(selected != null
                ? $"{selected.displayName} ({selected.id})"
                : $"缺失 [{selectedPlanet}]", headerStyle);
            if (GUILayout.Button(">", buttonStyle, GUILayout.Width(52f), GUILayout.Height(40f)))
                selectedPlanet = (selectedPlanet + 1) % types.Length;
            GUILayout.EndHorizontal();

            if (selected == null)
                return;

            Rect swatch = GUILayoutUtility.GetRect(44f, 28f, GUILayout.Width(44f));
            Color oldColor = GUI.color;
            GUI.color = selected.color;
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUILayout.Space(2f);
            GUILayout.Label($"颜色 #{ColorUtility.ToHtmlStringRGBA(selected.color)}  行为：{selected.behavior}",
                labelStyle);

            DrawTunableObject(selected);

            if (GUILayout.Button("重置当前星球类型", buttonStyle, GUILayout.Height(38f)))
            {
                selected.ResetToDefaults(selected.id);
                MarkDirty(selected);
            }
        }

        private void DrawTunableObject(object target)
        {
            FieldSpec[] specs = GetSpecs(target.GetType());
            string currentGroup = null;

            for (int i = 0; i < specs.Length; i++)
            {
                FieldSpec spec = specs[i];
                string group = string.IsNullOrEmpty(spec.attr.Group) ? "General" : spec.attr.Group;
                if (group != currentGroup)
                {
                    if (currentGroup != null)
                        GUILayout.Space(8f);
                    GUILayout.Label(GroupDisplayName(group), headerStyle);
                    currentGroup = group;
                }

                DrawField(target, spec);
            }
        }

        private void DrawField(object target, FieldSpec spec)
        {
            if (spec.field.FieldType == typeof(float))
            {
                float current = (float)spec.field.GetValue(target);
                float min = spec.attr.Min;
                float max = Mathf.Max(min + 0.0001f, spec.attr.Max);
                float step = Mathf.Max(0.0001f, spec.attr.Step);

                GUILayout.BeginHorizontal();
                GUILayout.Label(spec.attr.Label ?? spec.field.Name, labelStyle, GUILayout.Width(330f));

                bool previousChanged = GUI.changed;
                GUI.changed = false;
                float sliderValue = GUILayout.HorizontalSlider(current, min, max, sliderStyle, thumbStyle,
                    GUILayout.ExpandWidth(true));
                bool sliderChanged = GUI.changed;
                GUI.changed = previousChanged || sliderChanged;

                // 只看 GUI.changed：控件处理完鼠标事件后 Event.current.type 会变成 Used，
                // 再按事件类型判断会把真实拖动误判成非用户输入。
                float displayed = current;
                if (sliderChanged)
                {
                    displayed = sliderValue;
                    if (step > 0f)
                        displayed = Mathf.Round(displayed / step) * step;
                    displayed = Mathf.Clamp(displayed, min, max);
                    spec.field.SetValue(target, displayed);
                    MarkDirty(target);
                }

                GUILayout.Label(displayed.ToString("0.###"), valueStyle, GUILayout.Width(92f));
                GUILayout.EndHorizontal();
            }
            else if (spec.field.FieldType == typeof(int))
            {
                int current = (int)spec.field.GetValue(target);
                int min = Mathf.RoundToInt(spec.attr.Min);
                int max = Mathf.Max(min + 1, Mathf.RoundToInt(spec.attr.Max));
                int step = Mathf.Max(1, Mathf.RoundToInt(spec.attr.Step));

                GUILayout.BeginHorizontal();
                GUILayout.Label(spec.attr.Label ?? spec.field.Name, labelStyle, GUILayout.Width(330f));

                bool previousChanged = GUI.changed;
                GUI.changed = false;
                int sliderValue = Mathf.RoundToInt(
                    GUILayout.HorizontalSlider(current, min, max, sliderStyle, thumbStyle,
                        GUILayout.ExpandWidth(true)));
                bool sliderChanged = GUI.changed;
                GUI.changed = previousChanged || sliderChanged;

                int displayed = current;
                if (sliderChanged)
                {
                    displayed = min + Mathf.RoundToInt((sliderValue - min) / (float)step) * step;
                    displayed = Mathf.Clamp(displayed, min, max);
                    spec.field.SetValue(target, displayed);
                    MarkDirty(target);
                }

                GUILayout.Label(displayed.ToString(), valueStyle, GUILayout.Width(92f));
                GUILayout.EndHorizontal();
            }
            else if (spec.field.FieldType == typeof(bool))
            {
                bool current = (bool)spec.field.GetValue(target);

                bool previousChanged = GUI.changed;
                GUI.changed = false;
                bool toggled = GUILayout.Toggle(current, "  " + (spec.attr.Label ?? spec.field.Name), labelStyle,
                    GUILayout.Height(32f));
                bool toggleChanged = GUI.changed;
                GUI.changed = previousChanged || toggleChanged;

                if (toggleChanged && toggled != current)
                {
                    spec.field.SetValue(target, toggled);
                    MarkDirty(target);
                }
            }
        }

        private static FieldSpec[] GetSpecs(Type type)
        {
            if (fieldCache.TryGetValue(type, out FieldSpec[] cached))
                return cached;

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            List<FieldSpec> specs = new List<FieldSpec>();
            for (int i = 0; i < fields.Length; i++)
            {
                object[] attributes = fields[i].GetCustomAttributes(typeof(TunableAttribute), false);
                if (attributes.Length == 0)
                    continue;
                specs.Add(new FieldSpec
                {
                    field = fields[i],
                    attr = (TunableAttribute)attributes[0]
                });
            }

            specs.Sort((a, b) => a.field.MetadataToken.CompareTo(b.field.MetadataToken));
            FieldSpec[] result = specs.ToArray();
            fieldCache[type] = result;
            return result;
        }

        private void MarkDirty(object target)
        {
#if UNITY_EDITOR
            UnityEngine.Object unityObject = target as UnityEngine.Object;
            if (unityObject != null && AssetDatabase.Contains(unityObject))
                EditorUtility.SetDirty(unityObject);
#endif
        }

#if UNITY_EDITOR
        private void SaveAssets()
        {
            bool any = false;
            if (bootstrap.gameConfig != null && AssetDatabase.Contains(bootstrap.gameConfig))
            {
                EditorUtility.SetDirty(bootstrap.gameConfig);
                any = true;
            }
            if (bootstrap.feedbackConfig != null && AssetDatabase.Contains(bootstrap.feedbackConfig))
            {
                EditorUtility.SetDirty(bootstrap.feedbackConfig);
                any = true;
            }
            if (bootstrap.planetTypes != null)
            {
                for (int i = 0; i < bootstrap.planetTypes.Length; i++)
                {
                    PlanetTypeDef def = bootstrap.planetTypes[i];
                    if (def != null && AssetDatabase.Contains(def))
                    {
                        EditorUtility.SetDirty(def);
                        any = true;
                    }
                }
            }

            if (!any)
            {
                Debug.LogWarning("[TuningPanel] No persistent config assets to save.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TuningPanel] Config assets saved.");
        }
#endif

        private void EnsureStyles()
        {
            if (labelStyle != null)
                return;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                wordWrap = false
            };
            labelStyle.normal.textColor = new Color(0.86f, 0.91f, 1f, 1f);

            headerStyle = new GUIStyle(labelStyle)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1f, 0.84f, 0.55f, 1f);

            titleStyle = new GUIStyle(headerStyle)
            {
                fontSize = 28
            };
            titleStyle.normal.textColor = new Color(1f, 0.92f, 0.72f, 1f);

            valueStyle = new GUIStyle(labelStyle)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleRight
            };
            valueStyle.normal.textColor = new Color(0.72f, 0.95f, 0.82f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fixedHeight = 38f,
                padding = new RectOffset(12, 12, 6, 6)
            };

            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = 24f
            };

            thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedWidth = 28f,
                fixedHeight = 28f
            };

            noteStyle = new GUIStyle(labelStyle)
            {
                fontSize = 16,
                wordWrap = true
            };
            noteStyle.normal.textColor = new Color(0.65f, 0.70f, 0.78f, 1f);
        }

        private static string GroupDisplayName(string group)
        {
            switch (group)
            {
                case "Simulation": return "仿真";
                case "Camera": return "相机";
                case "Player": return "玩家";
                case "Bounce": return "弹跳";
                case "World": return "星环与生成";
                case "Collision": return "碰撞";
                case "Score": return "分数";
                case "Smash": return "撞碎";
                case "Audio": return "音频";
                case "Musou": return "无双";
                case "BombSkill": return "炸弹技能";
                case "Hook": return "钩锁";
                case "Landing": return "落地反馈";
                case "Bomb": return "炸弹";
                case "Other": return "其他反馈";
                case "Decay": return "衰减";
                case "Planet": return "星球属性";
                case "Ice": return "冰冰星";
                case "Cloud": return "云朵星";
                default: return string.IsNullOrEmpty(group) ? "通用" : group;
            }
        }
    }
}
