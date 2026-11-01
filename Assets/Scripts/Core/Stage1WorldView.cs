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

        private GameConfig config;
        private PlayerController player;
        private Camera targetCamera;
        private Transform worldRoot;
        private Transform playerRoot;
        private Transform holeBody;
        private Transform holeGlow;

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
            CreatePlayer();
            CreateStarfield();

            initialized = true;
            UpdateVisuals();
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
            holeGlow = CreateSprite("BlackHoleGlow", ProceduralSprites.Circle,
                new Color(0.55f, 0.18f, 0.9f, 0.18f), -20, worldRoot);
            holeBody = CreateSprite("BlackHoleBody", ProceduralSprites.Circle,
                Color.black, -10, worldRoot);
        }

        private void CreatePlayer()
        {
            playerRoot = CreateSprite("PlayerView", ProceduralSprites.Circle,
                new Color(1f, 0.82f, 0.40f, 1f), 20, worldRoot);

            // 一个屏幕正上方的小点，用来验证“径向朝上”和左右手性。
            Transform marker = CreateSprite("PlayerUpMarker", ProceduralSprites.Circle,
                new Color(1f, 0.97f, 0.82f, 0.95f), 21, playerRoot);
            marker.localPosition = new Vector3(0f, 0.62f, 0f);
            marker.localScale = new Vector3(0.22f, 0.22f, 1f);
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

        }

        private void OnGUI()
        {
            if (!initialized)
                return;

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16
                };
                hintStyle.normal.textColor = new Color(0.82f, 0.9f, 1f, 0.85f);
            }

            GUI.Label(new Rect(16f, Screen.height - 38f, 900f, 28f),
                "先点击 Game 视图 → A/D 环绕 · H 锁定半径 · R 重生 · F1 调试 · T 调参",
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
