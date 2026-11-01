using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BouncingPlanet.EditorTools
{
    /// <summary>
    /// 一键生成 Initial Demo v1.0 的配置资产与 Game 场景。
    /// 美术/策划不需要手写场景层级。
    /// </summary>
    public static class ProjectBootstrapper
    {
        private const string ConfigFolder = "Assets/Config";
        private const string PlanetTypeFolder = "Assets/Config/PlanetTypes";
        private const string SceneFolder = "Assets/Scenes";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string GameConfigPath = "Assets/Config/GameConfig.asset";
        private const string FeedbackConfigPath = "Assets/Config/FeedbackConfig.asset";

        [MenuItem("Tools/BouncingPlanet/Setup Initial Demo (v1.0)", priority = 0)]
        public static void SetupInitialDemo()
        {
            if (!EditorUtility.DisplayDialog(
                    "BouncingPlanet · Initial Demo v1.0",
                    "将创建/补齐 Config 资产，并新建或覆盖 Assets/Scenes/Game.unity。\n" +
                    "已有 Config 数值会保留；当前场景若有未保存改动会提示。\n\n继续？",
                    "继续", "取消"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateOrUpdateConfigAssets();
            CreateScene();

            EditorUtility.DisplayDialog(
                "完成",
                "已生成 Config 与 Game.unity。\n\n打开 Game 场景后按 Play：\n" +
                "F1 调试叠层，T 调参面板，P 暂停，O 单步。",
                "好");
        }

        [MenuItem("Tools/BouncingPlanet/Create Or Update Config Assets", priority = 10)]
        public static void CreateOrUpdateConfigAssets()
        {
            EnsureFolder(ConfigFolder);
            EnsureFolder(PlanetTypeFolder);

            GameConfig gameConfig = LoadOrCreateAsset<GameConfig>(GameConfigPath);
            if (gameConfig.profile != RulesProfile.InitialDemoV1)
                gameConfig.profile = RulesProfile.InitialDemoV1;
            EditorUtility.SetDirty(gameConfig);

            FeedbackConfig feedbackConfig = LoadOrCreateAsset<FeedbackConfig>(FeedbackConfigPath);
            EditorUtility.SetDirty(feedbackConfig);

            foreach (PlanetTypeId id in Enum.GetValues(typeof(PlanetTypeId)))
            {
                string path = $"{PlanetTypeFolder}/{id}.asset";
                PlanetTypeDef def = AssetDatabase.LoadAssetAtPath<PlanetTypeDef>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<PlanetTypeDef>();
                    def.ResetToDefaults(id);
                    def.name = id.ToString();
                    AssetDatabase.CreateAsset(def, path);
                }
                else if (def.id != id)
                {
                    def.id = id;
                    EditorUtility.SetDirty(def);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BouncingPlanet] Config assets ready under " + ConfigFolder);
        }

        [MenuItem("Tools/BouncingPlanet/Open Game Scene", priority = 20)]
        public static void OpenGameScene()
        {
            if (!System.IO.File.Exists(GameScenePath))
            {
                EditorUtility.DisplayDialog("提示", "Game.unity 不存在，请先执行 Tools/BouncingPlanet/Setup Initial Demo (v1.0)。", "好");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/BouncingPlanet/Reset Configs To Defaults", priority = 30)]
        public static void ResetConfigsToDefaults()
        {
            if (!EditorUtility.DisplayDialog("重置配置", "把所有 Config 数值恢复为 Initial Demo v1.0 默认值？", "重置", "取消"))
                return;

            GameConfig gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (gameConfig != null)
            {
                gameConfig.ResetToDefaults();
                EditorUtility.SetDirty(gameConfig);
            }

            FeedbackConfig feedbackConfig = AssetDatabase.LoadAssetAtPath<FeedbackConfig>(FeedbackConfigPath);
            if (feedbackConfig != null)
            {
                feedbackConfig.ResetToDefaults();
                EditorUtility.SetDirty(feedbackConfig);
            }

            foreach (PlanetTypeId id in Enum.GetValues(typeof(PlanetTypeId)))
            {
                string path = $"{PlanetTypeFolder}/{id}.asset";
                PlanetTypeDef def = AssetDatabase.LoadAssetAtPath<PlanetTypeDef>(path);
                if (def == null)
                    continue;
                def.ResetToDefaults(id);
                EditorUtility.SetDirty(def);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BouncingPlanet] Config assets reset to Initial Demo defaults.");
        }

        private static void CreateScene()
        {
            EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameConfig gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            FeedbackConfig feedbackConfig = AssetDatabase.LoadAssetAtPath<FeedbackConfig>(FeedbackConfigPath);
            PlanetTypeDef[] planetTypes = LoadPlanetTypes();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = gameConfig != null ? gameConfig.cameraOrthographicSize : 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = gameConfig != null ? gameConfig.backgroundColor : Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            GameObject root = new GameObject("GameRoot");
            GameBootstrap bootstrap = root.AddComponent<GameBootstrap>();
            SimulationClock clock = root.AddComponent<SimulationClock>();
            ObjectPoolService pool = root.AddComponent<ObjectPoolService>();
            PlayerInputReader inputReader = root.AddComponent<PlayerInputReader>();
            PlayerController player = root.AddComponent<PlayerController>();
            ScoreSystem score = root.AddComponent<ScoreSystem>();
            ImpactVfxService vfx = root.AddComponent<ImpactVfxService>();
            ProceduralAudioService audio = root.AddComponent<ProceduralAudioService>();
            FeedbackDirector feedbackDirector = root.AddComponent<FeedbackDirector>();
            PlanetWorld planetWorld = root.AddComponent<PlanetWorld>();
            CollisionSystem collisionSystem = root.AddComponent<CollisionSystem>();
            CameraRig cameraRig = root.AddComponent<CameraRig>();
            Stage1WorldView worldView = root.AddComponent<Stage1WorldView>();
            GameFlow gameFlow = root.AddComponent<GameFlow>();
            DebugOverlay debugOverlay = root.AddComponent<DebugOverlay>();
            TuningPanel tuningPanel = root.AddComponent<TuningPanel>();

            bootstrap.gameConfig = gameConfig;
            bootstrap.feedbackConfig = feedbackConfig;
            bootstrap.planetTypes = planetTypes;
            bootstrap.clock = clock;
            bootstrap.pool = pool;
            bootstrap.inputReader = inputReader;
            bootstrap.player = player;
            bootstrap.score = score;
            bootstrap.vfx = vfx;
            bootstrap.audioService = audio;
            bootstrap.feedbackDirector = feedbackDirector;
            bootstrap.planetWorld = planetWorld;
            bootstrap.collisionSystem = collisionSystem;
            bootstrap.cameraRig = cameraRig;
            bootstrap.worldView = worldView;
            bootstrap.gameFlow = gameFlow;
            bootstrap.debugOverlay = debugOverlay;
            bootstrap.tuningPanel = tuningPanel;

            player.bootstrap = bootstrap;
            player.input = inputReader;
            vfx.bootstrap = bootstrap;
            audio.bootstrap = bootstrap;
            feedbackDirector.bootstrap = bootstrap;
            planetWorld.bootstrap = bootstrap;
            collisionSystem.bootstrap = bootstrap;
            cameraRig.bootstrap = bootstrap;
            cameraRig.targetCamera = camera;
            worldView.bootstrap = bootstrap;
            gameFlow.bootstrap = bootstrap;
            debugOverlay.bootstrap = bootstrap;
            tuningPanel.bootstrap = bootstrap;

            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddSceneToBuildSettings(GameScenePath);
            Selection.activeObject = root;

            Debug.Log("[BouncingPlanet] Game scene created: " + GameScenePath);
        }

        private static PlanetTypeDef[] LoadPlanetTypes()
        {
            List<PlanetTypeDef> types = new List<PlanetTypeDef>();
            foreach (PlanetTypeId id in Enum.GetValues(typeof(PlanetTypeId)))
            {
                PlanetTypeDef def = AssetDatabase.LoadAssetAtPath<PlanetTypeDef>(
                    $"{PlanetTypeFolder}/{id}.asset");
                if (def != null)
                    types.Add(def);
            }
            return types.ToArray();
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);

            if (asset is GameConfig gameConfig)
                gameConfig.ResetToDefaults();
            else if (asset is FeedbackConfig feedbackConfig)
                feedbackConfig.ResetToDefaults();

            return asset;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    found = true;
                    break;
                }
            }

            if (!found)
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
