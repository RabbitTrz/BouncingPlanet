using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Stage 0 的静态视觉冒烟测试：
    /// 固定镜头、玩家占位圆、五个行星占位圆、虚线感星环。
    /// 不参与操作与碰撞；Stage 1 会替换成真正的玩家/相机系统。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Stage0Preview : MonoBehaviour
    {
        private sealed class PreviewPlanet
        {
            public Transform transform;
            public PlanetTypeDef def;
            public float angleOffset;
            public float radiusOffsetInLanes;
        }

        public GameBootstrap bootstrap;

        private GameConfig config;
        private Camera cam;
        private Transform previewRoot;
        private Transform player;
        private Transform holeBody;
        private Transform holeGlow;
        private readonly List<PreviewPlanet> previewPlanets = new List<PreviewPlanet>();
        private readonly List<LineRenderer> rings = new List<LineRenderer>();
        private readonly List<Material> ringMaterials = new List<Material>();
        private bool initialized;
        private float lastStartRadius;
        private float lastLaneSpacing;
        private float lastPixelsPerUnit;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.gameConfig == null)
            {
                Debug.LogWarning("[Stage0Preview] No GameBootstrap/GameConfig, preview skipped.");
                return;
            }

            config = bootstrap.gameConfig;
            EnsureCamera();
            BuildPreview();
            initialized = true;
            ApplyConfig();
        }

        private void Start()
        {
            if (!initialized)
                Initialize(GameBootstrap.Instance);
        }

        private void Update()
        {
            if (!initialized || config == null)
                return;

            ApplyConfig();
        }

        private void EnsureCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cam = cameraObject.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            if (cam.GetComponent<AudioListener>() == null && FindObjectOfType<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }

        private void BuildPreview()
        {
            GameObject rootObject = new GameObject("Stage0PreviewRoot");
            rootObject.transform.SetParent(transform, false);
            previewRoot = rootObject.transform;

            holeGlow = CreateSprite("BlackHoleGlow", ProceduralSprites.Circle,
                new Color(0.55f, 0.18f, 0.9f, 0.18f), -20);
            holeBody = CreateSprite("BlackHoleBody", ProceduralSprites.Circle,
                Color.black, -10);

            player = CreateSprite("PlayerPreview", ProceduralSprites.Circle,
                new Color(1f, 0.82f, 0.40f, 1f), 20);

            PlanetTypeDef[] types = bootstrap.planetTypes;
            if (types != null && types.Length > 0)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] == null)
                        continue;

                    Transform t = CreateSprite("Preview_" + types[i].displayName,
                        ProceduralSprites.Circle, types[i].color, 10);
                    previewPlanets.Add(new PreviewPlanet
                    {
                        transform = t,
                        def = types[i],
                        angleOffset = i switch
                        {
                            0 => 0f,
                            1 => 0f,
                            2 => 0.10f,
                            3 => -0.10f,
                            _ => 0.05f
                        },
                        radiusOffsetInLanes = i switch
                        {
                            0 => 1f,
                            1 => -1f,
                            2 => 0f,
                            3 => 0f,
                            _ => 0.5f
                        }
                    });
                }
            }

            CreateRings();
        }

        private Transform CreateSprite(string objectName, Sprite sprite, Color color, int sortingOrder)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(previewRoot, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go.transform;
        }

        private void CreateRings()
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            for (int i = 0; i < 3; i++)
            {
                GameObject ringObject = new GameObject("OrbitRing_" + i);
                ringObject.transform.SetParent(previewRoot, false);
                LineRenderer line = ringObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = true;
                line.positionCount = 128;
                line.numCornerVertices = 2;
                line.numCapVertices = 2;
                line.alignment = LineAlignment.View;
                line.sortingOrder = 0;

                if (spriteShader != null)
                {
                    Material material = new Material(spriteShader);
                    material.name = "Stage0RingMaterial";
                    line.material = material;
                    ringMaterials.Add(material);
                }

                line.startColor = new Color(0.58f, 0.78f, 1f, 0.28f);
                line.endColor = line.startColor;
                rings.Add(line);
            }
        }

        private void ApplyConfig()
        {
            if (config == null)
                return;

            cam.orthographicSize = Mathf.Max(0.01f, config.cameraOrthographicSize);
            cam.backgroundColor = config.backgroundColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            Vector2 playerPosition = Polar(config.startRadius, 0f);
            cam.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
            // worldAngle = -θ = 0；相机 rotationZ = worldAngle - 90°。
            cam.transform.rotation = Quaternion.Euler(0f, 0f, -90f);

            if (player != null)
            {
                player.position = new Vector3(playerPosition.x, playerPosition.y, 0f);
                float diameter = Mathf.Max(0.001f, config.PlayerDiameterWorld);
                player.localScale = new Vector3(diameter, diameter, 1f);
            }

            if (holeBody != null)
            {
                float holeDiameter = 52f / Mathf.Max(1f, config.cameraPixelsPerUnit);
                holeBody.position = Vector3.zero;
                holeBody.localScale = new Vector3(holeDiameter, holeDiameter, 1f);
            }

            if (holeGlow != null)
            {
                float glowDiameter = holeBody != null ? holeBody.localScale.x * 3.5f : 0.5f;
                holeGlow.position = Vector3.zero;
                holeGlow.localScale = new Vector3(glowDiameter, glowDiameter, 1f);
            }

            for (int i = 0; i < previewPlanets.Count; i++)
            {
                PreviewPlanet planet = previewPlanets[i];
                if (planet.transform == null || planet.def == null)
                    continue;

                float radius = Mathf.Max(0.05f,
                    config.startRadius + planet.radiusOffsetInLanes * config.laneSpacing);
                Vector2 position = Polar(radius, planet.angleOffset);
                planet.transform.position = new Vector3(position.x, position.y, 0f);
                float diameter = Mathf.Max(0.001f, config.GetPlanetWorldRadius(planet.def) * 2f);
                planet.transform.localScale = new Vector3(diameter, diameter, 1f);
                SpriteRenderer renderer = planet.transform.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.color = planet.def.color;
            }

            bool ringsDirty = !Mathf.Approximately(lastStartRadius, config.startRadius)
                || !Mathf.Approximately(lastLaneSpacing, config.laneSpacing)
                || !Mathf.Approximately(lastPixelsPerUnit, config.cameraPixelsPerUnit);

            if (ringsDirty)
            {
                lastStartRadius = config.startRadius;
                lastLaneSpacing = config.laneSpacing;
                lastPixelsPerUnit = config.cameraPixelsPerUnit;
                UpdateRings();
            }
        }

        private void UpdateRings()
        {
            float width = 2f / Mathf.Max(1f, config.cameraPixelsPerUnit);
            for (int ringIndex = 0; ringIndex < rings.Count; ringIndex++)
            {
                float radius = Mathf.Max(0.05f,
                    config.startRadius + (ringIndex - 1) * config.laneSpacing);
                LineRenderer line = rings[ringIndex];
                line.startWidth = width;
                line.endWidth = width;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        0f));
                }
            }
        }

        private static Vector2 Polar(float radius, float angle)
        {
            return new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < ringMaterials.Count; i++)
            {
                if (ringMaterials[i] != null)
                    Destroy(ringMaterials[i]);
            }
            ringMaterials.Clear();
        }
    }
}
