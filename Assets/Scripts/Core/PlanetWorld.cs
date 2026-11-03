using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 正式星环世界：初始铺场、上下流式生成、远上方回收、视图池。
    /// 仿真行星与视图分离：远处行星只存在数据，不为它们保留 GameObject。
    /// </summary>
    [DefaultExecutionOrder(1500)]
    [DisallowMultipleComponent]
    public sealed class PlanetWorld : MonoBehaviour
    {
        private sealed class BandView
        {
            public float radius;
            public LineRenderer line;
            public Material material;
        }

        private const float PlanetViewRadius = 4.0f;
        private const float RingVisibleRadius = 3.8f;
        private const int MaxViewObjects = 512;

        public GameBootstrap bootstrap;

        private readonly List<PlanetRuntime> planets = new List<PlanetRuntime>();
        private readonly List<BandView> bands = new List<BandView>();
        private readonly Stack<GameObject> freeViews = new Stack<GameObject>();
        private readonly List<GameObject> allViews = new List<GameObject>();

        private GameConfig config;
        private PlayerController player;
        private Transform viewRoot;
        private Transform poolRoot;
        private Transform bandRoot;
        private System.Random rng;
        private float spawnTop;
        private float lowR;
        private float lastBandPixelsPerUnit;
        private bool initialized;

        public IReadOnlyList<PlanetRuntime> Planets => planets;
        public int TotalCount => planets.Count;

        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < planets.Count; i++)
                {
                    if (planets[i].alive)
                        count++;
                }
                return count;
            }
        }

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.gameConfig == null || bootstrap.player == null)
            {
                Debug.LogWarning("[PlanetWorld] Missing bootstrap/config/player, skipped.");
                return;
            }

            config = bootstrap.gameConfig;
            player = bootstrap.player;

            viewRoot = new GameObject("PlanetViews").transform;
            viewRoot.SetParent(transform, false);
            poolRoot = new GameObject("PlanetViewPool").transform;
            poolRoot.SetParent(transform, false);
            bandRoot = new GameObject("BandViews").transform;
            bandRoot.SetParent(transform, false);

            initialized = true;
            Rebuild();
        }

        private void Start()
        {
            if (!initialized)
                Initialize(GameBootstrap.Instance);
        }

        private void LateUpdate()
        {
            if (initialized)
                SyncViews();
        }

        /// <summary>重开一局/换种子时重建。不会重置玩家或分数，由 GameFlow 处理。</summary>
        public void Rebuild()
        {
            if (config == null || player == null)
                return;

            ClearWorld();
            rng = new System.Random(config.generationSeed);

            lowR = Mathf.Max(config.minGenerationRadius,
                config.startRadius - config.initialBelow);
            spawnTop = config.startRadius + config.initialAbove;

            for (float radius = lowR; radius < spawnTop; radius += config.laneSpacing)
                AddBand(radius);

            AddSafeCatchPlanet();
            SyncViews();
        }

        /// <summary>由 CollisionSystem 每个仿真步调用。</summary>
        public void Step(float dt)
        {
            if (!initialized || config == null || player == null)
                return;

            float playerRadius = player.State.radius;
            Extend(playerRadius);
            CleanupFarAbove(playerRadius);

            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                if (planet.cooldown > 0f)
                    planet.cooldown -= dt;
                if (planet.flash > 0f)
                    planet.flash = Mathf.Max(0f, planet.flash - dt * 2.5f);
                if (planet.deathViewTimer > 0f)
                    planet.deathViewTimer = Mathf.Max(0f, planet.deathViewTimer - dt);
                if (!planet.alive || planet.swayAmplitude <= 0f)
                    continue;
                planet.swayPhase += planet.swaySpeed * dt;
                planet.angle = planet.baseAngle + Mathf.Sin(planet.swayPhase) * planet.swayAmplitude;
            }
        }

        private void Extend(float playerRadius)
        {
            float lane = config.laneSpacing;

            while (spawnTop < playerRadius + config.spawnAhead)
            {
                AddBand(spawnTop);
                spawnTop += lane;
            }

            float lowerLimit = Mathf.Max(config.minGenerationRadius,
                playerRadius - config.backfillBehind);
            while (lowR > lowerLimit)
            {
                float next = Mathf.Max(config.minGenerationRadius, lowR - lane);
                if (next >= lowR)
                    break;
                lowR = next;
                AddBand(lowR);
            }
        }

        private void CleanupFarAbove(float playerRadius)
        {
            float keep = playerRadius + config.recycleAbove;

            for (int i = planets.Count - 1; i >= 0; i--)
            {
                if (planets[i].radius <= keep)
                    continue;
                ReleasePlanetView(planets[i]);
                planets.RemoveAt(i);
            }

            for (int i = bands.Count - 1; i >= 0; i--)
            {
                if (bands[i].radius <= keep)
                    continue;
                DestroyBand(bands[i]);
                bands.RemoveAt(i);
            }

            if (spawnTop > keep)
                spawnTop = keep;
        }

        private void AddBand(float radius)
        {
            if (radius < config.minGenerationRadius)
                return;

            for (int i = 0; i < bands.Count; i++)
            {
                if (Mathf.Abs(bands[i].radius - radius) < 0.001f)
                    return;
            }

            AddBandView(radius);

            int count = config.GetRingCount(radius);
            float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
            float swayAmplitude = config.GetSwayAmplitude(count);

            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * (Mathf.PI * 2f / count);
                if (InSpawnClearZone(radius, angle))
                    continue;
                AddPlanet(radius, angle, PickType(), swayAmplitude);
            }

            float gapRadius = radius - config.laneSpacing * 0.5f;
            if (gapRadius >= config.minGenerationRadius)
            {
                int gapCount = config.GetGapCount(radius);
                float gapSway = Mathf.Min(config.swayMax, 0.08f);
                for (int i = 0; i < gapCount; i++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float jitter = ((float)rng.NextDouble() * 2f - 1f) * config.gapRadiusJitter;
                    float r = gapRadius + jitter;
                    if (r < config.minGenerationRadius || InSpawnClearZone(r, angle))
                        continue;
                    AddPlanet(r, angle, PickType(), gapSway);
                }
            }
        }

        private void AddSafeCatchPlanet()
        {
            float radius = Mathf.Max(config.minGenerationRadius,
                config.startRadius - 0.35f);
            AddPlanet(radius, 0f, PlanetTypeId.Rock, 0f);
        }

        private PlanetTypeId PickType()
        {
            PlanetTypeDef[] types = bootstrap.planetTypes;
            if (types == null || types.Length == 0)
                return PlanetTypeId.Rock;

            float total = 0f;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != null)
                    total += Mathf.Max(0f, types[i].spawnWeight);
            }

            if (total <= 0f)
                return PlanetTypeId.Rock;

            float roll = (float)rng.NextDouble() * total;
            for (int i = 0; i < types.Length; i++)
            {
                PlanetTypeDef def = types[i];
                if (def == null)
                    continue;
                roll -= Mathf.Max(0f, def.spawnWeight);
                if (roll <= 0f)
                    return def.id;
            }

            return PlanetTypeId.Rock;
        }

        private void AddPlanet(float radius, float baseAngle, PlanetTypeId type, float swayAmplitude)
        {
            PlanetTypeDef def = bootstrap.GetPlanetType(type);
            if (def == null)
                return;

            float sizeRoll = 1f + ((float)rng.NextDouble() * 2f - 1f) * config.sizeJitter;
            float worldRadius = Mathf.Max(0.005f, def.baseSize * config.sizeScale * sizeRoll);
            float swaySpeed = swayAmplitude > 0f ? config.GetSwaySpeed(rng) : 0f;
            float swayPhase = (float)rng.NextDouble() * Mathf.PI * 2f;

            planets.Add(new PlanetRuntime
            {
                def = def,
                radius = radius,
                baseAngle = baseAngle,
                angle = baseAngle + Mathf.Sin(swayPhase) * swayAmplitude,
                worldRadius = worldRadius,
                swayPhase = swayPhase,
                swaySpeed = swaySpeed,
                swayAmplitude = swayAmplitude
            });
        }

        /// <summary>炸弹技能生成的特殊炸弹星，持久存在直到被碰撞。</summary>
        public PlanetRuntime SpawnSkillBomb(float radius, float worldAngle)
        {
            if (bootstrap == null || config == null)
                return null;

            PlanetTypeDef def = bootstrap.GetPlanetType(PlanetTypeId.Bomb);
            if (def == null)
                return null;

            float r = Mathf.Max(config.minGenerationRadius, radius);
            PlanetRuntime planet = new PlanetRuntime
            {
                def = def,
                radius = r,
                baseAngle = worldAngle,
                angle = worldAngle,
                worldRadius = Mathf.Max(0.005f, def.baseSize * config.sizeScale),
                swayAmplitude = 0f,
                fromBombSkill = true
            };
            planets.Add(planet);
            return planet;
        }

        /// <summary>炸弹技能清场：除云朵/果冻外，范围内行星标记死亡；不连锁、不计分。</summary>
        public void ExplodeArea(Vector2 center, float radius, PlanetRuntime ignore = null)
        {
            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                if (!planet.alive || planet == ignore || planet.def == null)
                    continue;
                if (planet.def.behavior == PlanetBehavior.Cloud)
                    continue;
                float distance = Vector2.Distance(center, planet.WorldPosition);
                if (distance > radius + planet.worldRadius)
                    continue;

                planet.alive = false;
                planet.deathViewTimer = 0.28f;
                if (planet.viewComponent != null)
                    planet.viewComponent.PlayShatter();
            }
        }

        private bool InSpawnClearZone(float radius, float angle)
        {
            float radialDelta = radius - config.startRadius;
            if (radialDelta < -0.5f || radialDelta > 0.35f)
                return false;
            return Mathf.Abs(PolarMath.NormalizeAngle(angle)) < 0.28f;
        }

        private void AddBandView(float radius)
        {
            GameObject go = new GameObject("Band_" + radius.ToString("0.00"));
            go.transform.SetParent(bandRoot, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 128;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.sortingOrder = 0;
            line.startColor = new Color(0.58f, 0.78f, 1f, 0.16f);
            line.endColor = line.startColor;

            Shader shader = Shader.Find("Sprites/Default");
            Material material = shader != null ? new Material(shader) : null;
            if (material != null)
            {
                material.name = "BandRingMaterial";
                line.material = material;
            }

            float width = 2f / Mathf.Max(1f, config.cameraPixelsPerUnit);
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

            bands.Add(new BandView
            {
                radius = radius,
                line = line,
                material = material
            });
        }

        private void SyncViews()
        {
            if (!initialized || config == null || player == null)
                return;

            float playerRadius = player.State.radius;
            Camera camera = Camera.main;
            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                bool inRange = Mathf.Abs(planet.radius - playerRadius) <= PlanetViewRadius;

                Vector2 position = planet.WorldPosition;
                bool inView = true;
                if (camera != null)
                {
                    Vector3 viewport = camera.WorldToViewportPoint(
                        new Vector3(position.x, position.y, 0f));
                    inView = viewport.z > 0f &&
                             viewport.x >= -0.15f && viewport.x <= 1.15f &&
                             viewport.y >= -0.15f && viewport.y <= 1.15f;
                }

                bool visible = inRange && inView &&
                    (planet.alive || planet.deathViewTimer > 0f);

                // 死亡且本来没有视图的远处行星不需要补一个碎裂动画。
                if (!visible || (!planet.alive && planet.view == null))
                {
                    ReleasePlanetView(planet);
                    continue;
                }

                if (planet.view == null)
                {
                    GameObject viewObject = AcquireView();
                    if (viewObject != null)
                    {
                        planet.view = viewObject.transform;
                        planet.viewComponent = viewObject.GetComponent<PlanetView>();
                        planet.viewComponent.Bind(planet.def);
                        if (!planet.alive)
                            planet.viewComponent.PlayShatter();
                    }
                }
                if (planet.view == null)
                    continue;

                planet.view.position = new Vector3(position.x, position.y, 0f);
                // 行星贴图保持屏幕正立；状态机只在 Visual 子节点上做相对运动。
                planet.view.rotation = Camera.main != null
                    ? Camera.main.transform.rotation
                    : Quaternion.identity;
                planet.viewComponent.SetWorldDiameter(planet.worldRadius * 2f);
                planet.viewComponent.SetTint(
                    Color.Lerp(planet.def.color, Color.white, planet.flash * 0.7f));
            }

            if (!Mathf.Approximately(lastBandPixelsPerUnit, config.cameraPixelsPerUnit))
            {
                lastBandPixelsPerUnit = config.cameraPixelsPerUnit;
                float width = 2f / Mathf.Max(1f, config.cameraPixelsPerUnit);
                for (int i = 0; i < bands.Count; i++)
                {
                    bands[i].line.startWidth = width;
                    bands[i].line.endWidth = width;
                }
            }

            for (int i = 0; i < bands.Count; i++)
            {
                BandView band = bands[i];
                band.line.enabled = Mathf.Abs(band.radius - playerRadius) <= RingVisibleRadius;
            }
        }

        private GameObject AcquireView()
        {
            while (freeViews.Count > 0)
            {
                GameObject pooled = freeViews.Pop();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    pooled.transform.SetParent(viewRoot, false);
                    return pooled;
                }
            }

            if (allViews.Count >= MaxViewObjects)
                return null;

            GameObject go = new GameObject("PlanetView");
            go.transform.SetParent(viewRoot, false);
            go.AddComponent<PlanetView>();
            allViews.Add(go);
            return go;
        }

        private void ReleasePlanetView(PlanetRuntime planet)
        {
            if (planet.view == null)
                return;

            GameObject view = planet.view.gameObject;
            view.SetActive(false);
            view.transform.SetParent(poolRoot, false);
            freeViews.Push(view);
            planet.view = null;
            planet.viewComponent = null;
        }

        private void DestroyBand(BandView band)
        {
            if (band.line != null)
                Destroy(band.line.gameObject);
            if (band.material != null)
                Destroy(band.material);
        }

        private void ClearWorld()
        {
            for (int i = 0; i < planets.Count; i++)
                ReleasePlanetView(planets[i]);
            planets.Clear();

            for (int i = 0; i < bands.Count; i++)
                DestroyBand(bands[i]);
            bands.Clear();
            freeViews.Clear();

            for (int i = 0; i < allViews.Count; i++)
            {
                if (allViews[i] != null)
                    Destroy(allViews[i]);
            }
            allViews.Clear();
        }

        private void OnDestroy()
        {
            ClearWorld();
        }
    }
}
