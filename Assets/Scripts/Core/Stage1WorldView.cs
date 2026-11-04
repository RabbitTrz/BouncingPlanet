using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Stage 1 占位视图：玩家、黑洞、参考星环、屏幕星空。
    /// 真实可碰撞行星由 PlanetWorld 提供，这里不再放“参考天体”，
    /// 避免出现看得见但撞不到的假星球。
    /// </summary>
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    public sealed class Stage1WorldView : MonoBehaviour
    {
        private sealed class StarLayer
        {
            public Transform root;
            public float rotationFactor;
        }

        public GameBootstrap bootstrap;
        [Tooltip("可选的玩家静态 Sprite 状态配置；留空使用运行时默认状态机。")]
        public SpriteStateProfile playerProfile;

        public BlackHoleView BlackHole => blackHoleView;

        private GameConfig config;
        private PlayerController player;
        private PlayerSkills skills;
        private Camera targetCamera;
        private Transform worldRoot;
        private Transform playerRoot;
        private Transform playerMotionPivot;
        private Transform playerVisual;
        private TrailRenderer playerTrail;
        private Material playerTrailMaterial;
        private LineRenderer goalRing;
        private Material goalRingMaterial;
        private float smoothedPlayerTurn;
        private float playerTurnVelocity;
        private SpriteRenderer playerRenderer;
        private SpriteStateMachine playerStateMachine;
        private Transform holeBody;
        private Transform holeGlow;
        private BlackHoleView blackHoleView;
        private TiledBackgroundView backgroundView;

        private readonly List<StarLayer> starLayers = new List<StarLayer>();
        private FeedbackDirector feedback;
        private ScoreSystem score;
        private GUIStyle hintStyle;
        private GUIStyle scoreStyle;
        private bool initialized;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.gameConfig == null || bootstrap.player == null)
            {
                Debug.LogWarning("[Stage1WorldView] Missing bootstrap/config/player, skipped.");
                return;
            }

            config = bootstrap.gameConfig;
            player = bootstrap.player;
            skills = bootstrap.skills;
            feedback = bootstrap.feedbackDirector;
            score = bootstrap.score;
            targetCamera = bootstrap.cameraRig != null ? bootstrap.cameraRig.ActiveCamera : Camera.main;
            if (targetCamera == null)
            {
                Debug.LogWarning("[Stage1WorldView] No camera, skipped.");
                return;
            }

            worldRoot = new GameObject("Stage1WorldRoot").transform;
            worldRoot.SetParent(transform, false);

            CreateBlackHole();
            CreateBackground();
            CreatePlayer();
            if (backgroundView == null)
                CreateStarfield();
            CreateGoalRing();

            if (player != null)
            {
                player.CollisionReaction += OnPlayerCollisionReaction;
                player.Respawned += OnPlayerRespawned;
            }

            initialized = true;
            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.CollisionReaction -= OnPlayerCollisionReaction;
                player.Respawned -= OnPlayerRespawned;
            }
            if (playerTrailMaterial != null)
                Destroy(playerTrailMaterial);
            if (goalRingMaterial != null)
                Destroy(goalRingMaterial);
            if (backgroundView != null)
                Destroy(backgroundView.gameObject);
            if (blackHoleView != null)
                Destroy(blackHoleView.gameObject);
        }

        private void CreateGoalRing()
        {
            GameObject ringObject = new GameObject("GoalRing");
            ringObject.transform.SetParent(worldRoot, false);

            goalRing = ringObject.AddComponent<LineRenderer>();
            goalRing.useWorldSpace = true;
            goalRing.loop = true;
            goalRing.positionCount = 128;
            goalRing.numCornerVertices = 2;
            goalRing.numCapVertices = 2;
            goalRing.alignment = LineAlignment.View;
            goalRing.sortingOrder = 1;
            goalRing.startColor = new Color(0.35f, 1f, 0.55f, 0.55f);
            goalRing.endColor = goalRing.startColor;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                goalRingMaterial = new Material(shader)
                {
                    name = "GoalRingMaterial"
                };
                goalRing.material = goalRingMaterial;
            }
            goalRing.enabled = false;
        }

        private void Start()
        {
            if (!initialized)
                Initialize(GameBootstrap.Instance);
        }

        private void LateUpdate()
        {
            if (initialized)
                UpdateVisuals();
        }

        private void CreateBlackHole()
        {
            GameObject holeObject = new GameObject("BlackHole");
            holeObject.transform.SetParent(worldRoot, false);
            BlackHoleView view = holeObject.AddComponent<BlackHoleView>();
            if (view.Initialize(config))
            {
                blackHoleView = view;
                return;
            }

            Destroy(holeObject);

            // 美术缺失时回退到原来的程序化圆 + 光晕。
            holeGlow = CreateSprite("BlackHoleGlow", ProceduralSprites.Circle,
                new Color(0.55f, 0.18f, 0.9f, 0.18f), -20, worldRoot);
            holeBody = CreateSprite("BlackHoleBody", ProceduralSprites.Circle,
                Color.black, -10, worldRoot);
        }

        private void CreateBackground()
        {
            GameObject backgroundObject = new GameObject("TiledBackground");
            TiledBackgroundView view = backgroundObject.AddComponent<TiledBackgroundView>();
            if (view.Initialize(bootstrap))
            {
                backgroundView = view;
                return;
            }

            Destroy(backgroundObject);
        }

        private void CreatePlayer()
        {
            GameObject rootObject = new GameObject("PlayerRoot");
            rootObject.transform.SetParent(worldRoot, false);
            playerRoot = rootObject.transform;

            GameObject pivotObject = new GameObject("PlayerMotionPivot");
            pivotObject.transform.SetParent(playerRoot, false);
            playerMotionPivot = pivotObject.transform;

            GameObject visualObject = new GameObject("PlayerVisual");
            visualObject.transform.SetParent(playerMotionPivot, false);
            playerVisual = visualObject.transform;
            playerRenderer = visualObject.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = ProceduralSprites.Circle;
            playerRenderer.color = Color.white;
            playerRenderer.sortingOrder = 20;

            playerStateMachine = visualObject.AddComponent<SpriteStateMachine>();
            SpriteStateProfile profile = playerProfile != null
                ? playerProfile
                : SpriteStateDefaults.GetPlayerProfile();
            playerStateMachine.Initialize(profile, playerRenderer, playerVisual);
            playerStateMachine.SetState("happy");

            // 让 Sprite 的 native size 归一化为 1 世界单位，兼容不同导入 PPU；
            // 同一状态的帧请保持相同像素尺寸。
            Sprite initialSprite = playerRenderer.sprite;
            float nativeSize = initialSprite != null
                ? Mathf.Max(0.0001f, initialSprite.bounds.size.x)
                : 1f;
            playerVisual.localScale = Vector3.one / nativeSize;
            playerStateMachine.CaptureBase();

            // 一个屏幕正上方的小点，用来验证“径向朝上”和左右手性。
            Transform marker = CreateSprite("PlayerUpMarker", ProceduralSprites.Circle,
                new Color(1f, 0.97f, 0.82f, 0.95f), 21, playerRoot);
            marker.localPosition = new Vector3(0f, 0.62f, 0f);
            marker.localScale = new Vector3(0.22f, 0.22f, 1f);

            CreatePlayerTrail();
        }

        private void CreatePlayerTrail()
        {
            GameObject trailObject = new GameObject("PlayerAscendTrail");
            trailObject.transform.SetParent(worldRoot, false);

            playerTrail = trailObject.AddComponent<TrailRenderer>();
            playerTrail.autodestruct = false;
            playerTrail.emitting = false;
            playerTrail.time = config.playerTrailTime;
            playerTrail.minVertexDistance = 0.01f;
            playerTrail.numCapVertices = 2;
            playerTrail.numCornerVertices = 2;
            playerTrail.alignment = LineAlignment.View;
            playerTrail.textureMode = LineTextureMode.Stretch;
            playerTrail.sortingOrder = 19;
            playerTrail.startWidth = config.playerTrailWidth;
            playerTrail.endWidth = 0f;
            playerTrail.startColor = config.playerTrailColor;
            Color endColor = config.playerTrailColor;
            endColor.a = 0f;
            playerTrail.endColor = endColor;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                playerTrailMaterial = new Material(shader)
                {
                    name = "PlayerTrailMaterial"
                };
                playerTrail.material = playerTrailMaterial;
            }
        }

        private void CreateStarfield()
        {
            CreateStarLayer(0.10f, 120, 20261101, 0.0035f, 0.010f, -101,
                new Color(0.82f, 0.9f, 1f, 0.55f));
            CreateStarLayer(0.26f, 90, 77123, 0.005f, 0.016f, -100,
                new Color(1f, 0.93f, 0.78f, 0.8f));
        }

        private void CreateStarLayer(float rotationFactor, int count, int seed,
            float minSize, float maxSize, int sortingOrder, Color color)
        {
            Transform root = new GameObject("StarLayer_" + rotationFactor).transform;
            root.SetParent(targetCamera.transform, false);
            root.localPosition = new Vector3(0f, 0f, 20f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            System.Random rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float x = -2.8f + (float)rng.NextDouble() * 5.6f;
                float y = -1.65f + (float)rng.NextDouble() * 3.3f;
                float size = minSize + (float)rng.NextDouble() * (maxSize - minSize);
                Color starColor = color;
                starColor.a *= 0.35f + (float)rng.NextDouble() * 0.65f;
                if (i % 17 == 0)
                    starColor = new Color(1f, 0.82f, 0.48f, 0.9f);

                Transform star = CreateSprite("Star_" + i, ProceduralSprites.Circle,
                    starColor, sortingOrder, root);
                star.localPosition = new Vector3(x, y, 0f);
                star.localScale = new Vector3(size, size, 1f);
            }

            starLayers.Add(new StarLayer
            {
                root = root,
                rotationFactor = rotationFactor
            });
        }

        private Transform CreateSprite(string objectName, Sprite sprite, Color color, int sortingOrder,
            Transform parent)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go.transform;
        }

        private void UpdateVisuals()
        {
            if (config == null || player == null || targetCamera == null)
                return;

            PlayerState state = player.State;
            Vector2 worldPosition = state.WorldPosition;

            if (playerRoot != null)
            {
                playerRoot.gameObject.SetActive(!state.dead);
                playerRoot.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
                float diameter = Mathf.Max(0.001f, config.PlayerDiameterWorld);
                float squash = feedback != null ? feedback.Squash : 0f;
                float scaleX = diameter * (1f + 0.22f * squash);
                float scaleY = diameter * (1f - 0.26f * squash);
                playerRoot.localScale = new Vector3(scaleX, scaleY, 1f);
                playerRoot.rotation = Quaternion.Euler(
                    0f, 0f, state.WorldAngle * Mathf.Rad2Deg - 90f);

                // 纯视觉转向：完全由实际角速度驱动，并按当前半径的最大角速度归一化。
                bool playing = bootstrap == null || bootstrap.gameFlow == null ||
                    bootstrap.gameFlow.State == GameFlow.FlowState.Playing;
                float maxOmega = Mathf.Min(
                    config.maxAngularSpeed,
                    config.tangentialSpeed / Mathf.Max(0.3f, state.radius));
                float targetTurn = (!playing || state.dead || maxOmega <= 0.0001f)
                    ? 0f
                    : Mathf.Clamp(state.angularVelocity / maxOmega, -1f, 1f);
                smoothedPlayerTurn = Mathf.SmoothDamp(
                    smoothedPlayerTurn, targetTurn, ref playerTurnVelocity,
                    Mathf.Max(0.01f, config.playerMotionSmoothing),
                    Mathf.Infinity, Time.unscaledDeltaTime);

                if (playerMotionPivot != null)
                {
                    float rootScaleX = Mathf.Max(0.0001f, playerRoot.localScale.x);
                    playerMotionPivot.localPosition = new Vector3(
                        smoothedPlayerTurn * config.playerLateralOffset / rootScaleX,
                        playerMotionPivot.localPosition.y,
                        0f);
                    playerMotionPivot.localRotation = Quaternion.Euler(
                        0f, 0f, -smoothedPlayerTurn * config.playerBankAngle);
                }

                if (playerStateMachine != null)
                {
                    string current = playerStateMachine.CurrentStateName;
                    if (state.dead)
                    {
                        if (current != "dead")
                            playerStateMachine.SetState("dead");
                    }
                    else if (skills != null && skills.IsMusouActive)
                    {
                        if (current != "override")
                            playerStateMachine.SetState("override");
                    }
                    else if (current == "override" || current == "dead" ||
                             string.IsNullOrEmpty(current))
                    {
                        playerStateMachine.SetState("happy");
                    }
                }

                if (playerTrail != null)
                {
                    playerTrail.transform.position = playerMotionPivot != null
                        ? playerMotionPivot.position
                        : playerRoot.position;
                    bool musou = skills != null && skills.IsMusouActive;
                    bool ascending = state.radialVelocity > config.playerTrailMinAscendSpeed;
                    bool emit = config.playerTrailEnabled && playing && !state.dead &&
                                (musou || ascending);
                    playerTrail.emitting = emit;

                    if (emit)
                    {
                        float width = Mathf.Max(0.001f, config.playerTrailWidth) *
                                      (musou ? 1.6f : 1f);
                        playerTrail.startWidth = width;
                        playerTrail.endWidth = 0f;
                        playerTrail.time = Mathf.Max(0.05f, config.playerTrailTime) *
                                           (musou ? 1.4f : 1f);

                        Color color = musou
                            ? config.musouTrailColor
                            : config.playerTrailColor;
                        playerTrail.startColor = color;
                        Color endColor = color;
                        endColor.a = 0f;
                        playerTrail.endColor = endColor;
                    }
                }
            }

            // 星空以相机为父级，但按世界角度的 0.10 / 0.26 倍旋转，
            // 给 A/D 提供明显的屏幕参照，而不是完全静止。
            float worldAngleDegrees = state.WorldAngle * Mathf.Rad2Deg;
            for (int i = 0; i < starLayers.Count; i++)
            {
                StarLayer layer = starLayers[i];
                if (layer.root != null)
                    layer.root.localRotation = Quaternion.Euler(
                        0f, 0f, worldAngleDegrees * layer.rotationFactor);
            }

            float holeDiameter = 52f / Mathf.Max(1f, config.cameraPixelsPerUnit);
            if (holeBody != null)
            {
                holeBody.position = Vector3.zero;
                holeBody.localScale = new Vector3(holeDiameter, holeDiameter, 1f);
            }
            if (holeGlow != null)
            {
                holeGlow.position = Vector3.zero;
                float glowDiameter = holeDiameter * 3.5f;
                holeGlow.localScale = new Vector3(glowDiameter, glowDiameter, 1f);
            }

            GameFlow flow = bootstrap != null ? bootstrap.gameFlow : null;
            LevelConfig level = flow != null ? flow.CurrentLevel : null;
            bool showGoal = level != null && !level.IsEndless && flow != null &&
                (flow.State == GameFlow.FlowState.Playing ||
                 flow.State == GameFlow.FlowState.Paused ||
                 flow.State == GameFlow.FlowState.Win);
            if (goalRing != null)
            {
                goalRing.enabled = showGoal;
                if (showGoal)
                {
                    float width = 3f / Mathf.Max(1f, config.cameraPixelsPerUnit);
                    goalRing.startWidth = width;
                    goalRing.endWidth = width;
                    Color color = new Color(0.35f, 1f, 0.55f, 0.55f);
                    goalRing.startColor = color;
                    goalRing.endColor = color;
                    for (int i = 0; i < goalRing.positionCount; i++)
                    {
                        float angle = i / (float)goalRing.positionCount *
                                      Mathf.PI * 2f;
                        goalRing.SetPosition(i, new Vector3(
                            Mathf.Cos(angle) * level.goalRadius,
                            Mathf.Sin(angle) * level.goalRadius,
                            0f));
                    }
                }
            }
        }

        private void OnPlayerCollisionReaction(bool ascending)
        {
            if (playerStateMachine == null || player == null || player.IsDead)
                return;
            if (skills != null && skills.IsMusouActive)
                return;

            string current = playerStateMachine.CurrentStateName;
            if (current == "override" || current == "dead")
                return;

            // 先回到 happy，保证 up/down 播完后返回 happy，而不是上一段表情。
            playerStateMachine.SetState("happy", true);
            playerStateMachine.PlayOneShot(ascending ? "up" : "down");
        }

        private void OnPlayerRespawned()
        {
            if (playerStateMachine != null)
                playerStateMachine.SetState("happy", true);
            if (playerTrail != null)
                playerTrail.Clear();
        }

        private void OnGUI()
        {
            if (!initialized)
                return;

            // 原生 HUD 存在时，旧的分数文字和操作提示不再绘制。
            if (GameHudView.Instance != null)
                return;

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                };
                hintStyle.normal.textColor = new Color(0.82f, 0.9f, 1f, 0.85f);
            }

            GUI.Label(new Rect(16f, Screen.height - 38f, 980f, 28f),
                "先点击 Game 视图 → A/D 环绕 · H 锁定半径 · B 背景有线/无线 · R 重生 · F1 调试 · T 调参",
                hintStyle);

            if (config != null && config.showScoreHud && score != null)
            {
                if (scoreStyle == null)
                {
                    scoreStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 30,
                        alignment = TextAnchor.UpperRight,
                        fontStyle = FontStyle.Bold
                    };
                    scoreStyle.normal.textColor = new Color(1f, 0.9f, 0.62f, 0.95f);
                }

                GUI.Label(new Rect(Screen.width - 226f, 16f, 210f, 48f),
                    score.Score.ToString(), scoreStyle);
            }
        }

    }
}
