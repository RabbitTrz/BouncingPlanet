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

            // 仅无尽模式局部弧生成使用。
            public bool local;
            public int seedKey;
            public int slotCount;
            public float phase;
            public float swayAmplitude;
            public float gapRadius;
            public int gapSlotCount;
            public float gapPhase;
            public readonly Dictionary<int, PlanetRuntime> slots =
                new Dictionary<int, PlanetRuntime>();
            public readonly Dictionary<int, PlanetRuntime> gapSlots =
                new Dictionary<int, PlanetRuntime>();
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
        private LevelConfig currentLevel;
        private LevelGenerationSettings generation;
        private Transform viewRoot;
        private Transform poolRoot;
        private Transform bandRoot;
        private System.Random rng;
        private float spawnTop;
        private float lowR;
        private float startRadius;
        private float lastBandPixelsPerUnit;
        private bool initialized;
        private bool useFixedLayout;
        private bool useLocalArcs;
        private readonly HashSet<int> desiredSlots = new HashSet<int>();
        private readonly List<int> removeSlots = new List<int>();

        public IReadOnlyList<PlanetRuntime> Planets => planets;
        public int TotalCount => planets.Count;
        public LevelConfig CurrentLevel => currentLevel;

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

        /// <summary>切换关卡并重建世界。null 表示沿用旧的单场景程序生成。</summary>
        public void SetLevel(LevelConfig level)
        {
            currentLevel = level;
            if (initialized)
                Rebuild();
        }

        private void LateUpdate()
        {
            if (initialized)
                SyncViews();
        }

        /// <summary>重开一局/换关卡时重建。不会重置玩家或分数，由 GameFlow 处理。</summary>
        public void Rebuild()
        {
            if (config == null || player == null)
                return;

            ClearWorld();
            startRadius = currentLevel != null
                ? currentLevel.startRadius
                : config.startRadius;
            useFixedLayout = currentLevel != null && !currentLevel.IsEndless;
            useLocalArcs = currentLevel != null && currentLevel.IsEndless;

            if (useFixedLayout)
            {
                BuildBakedLevel();
                SyncViews();
                return;
            }

            generation = currentLevel != null
                ? currentLevel.generation
                : new LevelGenerationSettings();
            if (generation == null)
                generation = new LevelGenerationSettings();
            if (currentLevel == null)
                generation.CopyFrom(config);

            rng = new System.Random(generation.seed);
            lowR = Mathf.Max(generation.minGenerationRadius,
                startRadius - generation.initialBelow);
            spawnTop = startRadius + generation.initialAbove;

            for (float radius = lowR; radius < spawnTop;
                 radius += Mathf.Max(0.05f, generation.laneSpacing))
            {
                AddBand(radius);
            }

            AddSafeCatchPlanet();
            SyncViews();
        }

        private void BuildBakedLevel()
        {
            if (currentLevel == null)
                return;

            generation = currentLevel.generation ?? new LevelGenerationSettings();
            float maxRadius = Mathf.Max(startRadius, currentLevel.goalRadius);
            if (currentLevel.planets != null)
            {
                for (int i = 0; i < currentLevel.planets.Count; i++)
                {
                    LevelPlanetEntry entry = currentLevel.planets[i];
                    if (entry == null || entry.type == null)
                        continue;

                    float angle = entry.angleDegrees * Mathf.Deg2Rad;
                    float swayAmplitude = Mathf.Max(0f, entry.swayAmplitude);
                    float swaySpeed = Mathf.Max(0f, entry.swaySpeed);
                    float swayPhase = entry.swayPhase;
                    float worldRadius = Mathf.Max(0.005f,
                        entry.type.baseSize * config.sizeScale *
                        Mathf.Max(0.05f, entry.sizeMultiplier));

                    planets.Add(new PlanetRuntime
                    {
                        def = entry.type,
                        radius = entry.radius,
                        baseAngle = angle,
                        angle = angle + Mathf.Sin(swayPhase) * swayAmplitude,
                        worldRadius = worldRadius,
                        swayPhase = swayPhase,
                        swaySpeed = swaySpeed,
                        swayAmplitude = swayAmplitude
                    });
                    maxRadius = Mathf.Max(maxRadius, entry.radius);
                }
            }

            float lane = Mathf.Max(0.05f, generation.laneSpacing);
            float low = Mathf.Max(generation.minGenerationRadius,
                startRadius - generation.initialBelow);
            for (float radius = low; radius <= maxRadius; radius += lane)
                AddBandView(radius);
        }

        /// <summary>由 CollisionSystem 每个仿真步调用。</summary>
        public void Step(float dt)
        {
            if (!initialized || config == null || player == null)
                return;

            float playerRadius = player.State.radius;
            if (!useFixedLayout)
            {
                Extend(playerRadius);
                CleanupFarAbove(playerRadius);
            }
            if (useLocalArcs)
                UpdateLocalArcs(player.State.WorldAngle);

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
            if (generation == null)
                return;
            float lane = Mathf.Max(0.05f, generation.laneSpacing);

            while (spawnTop < playerRadius + generation.spawnAhead)
            {
                AddBand(spawnTop);
                spawnTop += lane;
            }

            float lowerLimit = Mathf.Max(generation.minGenerationRadius,
                playerRadius - generation.backfillBehind);
            while (lowR > lowerLimit)
            {
                float next = Mathf.Max(generation.minGenerationRadius, lowR - lane);
                if (next >= lowR)
                    break;
                lowR = next;
                AddBand(lowR);
            }
        }

        private void CleanupFarAbove(float playerRadius)
        {
            if (generation == null)
                return;
            float keep = playerRadius + generation.recycleAbove;

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
                RemoveBandPlanets(bands[i]);
                DestroyBand(bands[i]);
                bands.RemoveAt(i);
            }

            if (spawnTop > keep)
                spawnTop = keep;
        }

        private void AddBand(float radius)
        {
            if (generation == null || radius < generation.minGenerationRadius)
                return;

            for (int i = 0; i < bands.Count; i++)
            {
                if (Mathf.Abs(bands[i].radius - radius) < 0.001f)
                    return;
            }

            if (useLocalArcs)
            {
                AddLocalBand(radius);
                return;
            }

            AddBandView(radius);

            int count = GetRingCount(radius);
            if (count <= 0)
                return;

            float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
            float swayAmplitude = generation.GetSwayAmplitude(count);

            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * (Mathf.PI * 2f / count);
                if (InSpawnClearZone(radius, angle))
                    continue;
                AddPlanet(radius, angle, PickType(), swayAmplitude);
            }

            float gapRadius = radius - Mathf.Max(0.05f, generation.laneSpacing) * 0.5f;
            if (gapRadius >= generation.minGenerationRadius)
            {
                int gapCount = generation.GetGapCount(radius);
                float gapSway = Mathf.Min(generation.swayMax, 0.08f);
                for (int i = 0; i < gapCount; i++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float jitter = ((float)rng.NextDouble() * 2f - 1f) *
                                   generation.gapRadiusJitter;
                    float r = gapRadius + jitter;
                    if (r < generation.minGenerationRadius ||
                        InSpawnClearZone(r, angle))
                    {
                        continue;
                    }
                    AddPlanet(r, angle, PickType(), gapSway);
                }
            }
        }

        private void AddLocalBand(float radius)
        {
            BandView band = AddBandView(radius);
            band.local = true;
            band.seedKey = Mathf.RoundToInt(radius * 1000f);

            float spacing = Mathf.Max(0.1f, generation.targetArcSpacing);
            float density = GetLocalDensity(radius);
            int slots = Mathf.RoundToInt(
                Mathf.PI * 2f * radius / spacing * density);
            band.slotCount = Mathf.Max(generation.minPlanetsPerRing, Mathf.Max(1, slots));
            band.phase = Hash01(generation.seed, band.seedKey, 0) * Mathf.PI * 2f;
            band.swayAmplitude = generation.GetSwayAmplitude(band.slotCount);

            float gapRadius = radius -
                Mathf.Max(0.05f, generation.laneSpacing) * 0.5f;
            if (gapRadius >= generation.minGenerationRadius &&
                generation.gapCountMultiplier > 0f)
            {
                band.gapRadius = gapRadius;
                band.gapSlotCount = Mathf.Max(1, Mathf.RoundToInt(
                    band.slotCount * generation.gapCountMultiplier));
                band.gapPhase = Hash01(generation.seed, band.seedKey, 1) *
                                Mathf.PI * 2f;
            }

            UpdateBandWindow(band, player.State.WorldAngle);
        }

        private float GetLocalDensity(float radius)
        {
            float density = Mathf.Max(0.01f, generation.densityMultiplier);
            if (currentLevel != null && currentLevel.IsEndless &&
                currentLevel.endless != null &&
                currentLevel.endless.densityByRadius != null)
            {
                float curve = currentLevel.endless.densityByRadius.Evaluate(radius);
                density *= Mathf.Clamp(curve, 0.1f,
                    currentLevel.endless.maxDensity);
            }
            return density;
        }

        private void UpdateLocalArcs(float playerAngle)
        {
            if (!useLocalArcs)
                return;

            float halfArc = GetScreenHalfWidthWorld() +
                            Mathf.Max(0f, generation.arcWindowMargin);
            for (int i = 0; i < bands.Count; i++)
            {
                BandView band = bands[i];
                if (band.local)
                    UpdateBandWindow(band, playerAngle, halfArc);
            }
        }

        private void UpdateBandWindow(BandView band, float playerAngle)
        {
            float halfArc = GetScreenHalfWidthWorld() +
                            Mathf.Max(0f, generation.arcWindowMargin);
            UpdateBandWindow(band, playerAngle, halfArc);
        }

        private void UpdateBandWindow(BandView band, float playerAngle, float halfArc)
        {
            UpdateSlotWindow(band, playerAngle, halfArc, false);
            if (band.gapSlotCount > 0)
                UpdateSlotWindow(band, playerAngle, halfArc, true);
        }

        private void UpdateSlotWindow(BandView band, float playerAngle,
            float halfArc, bool gap)
        {
            int count = gap ? band.gapSlotCount : band.slotCount;
            if (count <= 0)
                return;

            float radius = gap ? band.gapRadius : band.radius;
            float phase = gap ? band.gapPhase : band.phase;
            float slotArc = Mathf.Max(0.0001f,
                Mathf.PI * 2f * radius / count);
            int halfCount = Mathf.CeilToInt(halfArc / slotArc);

            desiredSlots.Clear();
            if (halfCount * 2 + 1 >= count)
            {
                for (int i = 0; i < count; i++)
                    desiredSlots.Add(i);
            }
            else
            {
                int center = IndexForAngle(playerAngle, phase, count);
                for (int i = -halfCount; i <= halfCount; i++)
                {
                    int index = (center + i) % count;
                    if (index < 0)
                        index += count;
                    desiredSlots.Add(index);
                }
            }

            Dictionary<int, PlanetRuntime> active =
                gap ? band.gapSlots : band.slots;
            foreach (int slot in desiredSlots)
            {
                if (active.ContainsKey(slot))
                    continue;
                PlanetRuntime planet = CreateLocalPlanet(band, slot, gap);
                if (planet != null)
                    active[slot] = planet;
            }

            removeSlots.Clear();
            foreach (KeyValuePair<int, PlanetRuntime> pair in active)
            {
                if (!desiredSlots.Contains(pair.Key))
                    removeSlots.Add(pair.Key);
            }
            for (int i = 0; i < removeSlots.Count; i++)
            {
                int key = removeSlots[i];
                if (!active.TryGetValue(key, out PlanetRuntime planet))
                    continue;
                active.Remove(key);
                RemovePlanet(planet);
            }
        }

        private static int IndexForAngle(float playerAngle, float phase, int count)
        {
            float relative = PolarMath.NormalizeAngle(playerAngle - phase);
            int index = Mathf.FloorToInt(relative / (Mathf.PI * 2f / count));
            index %= count;
            if (index < 0)
                index += count;
            return index;
        }

        private PlanetRuntime CreateLocalPlanet(BandView band, int slot, bool gap)
        {
            int count = gap ? band.gapSlotCount : band.slotCount;
            float radius = gap ? band.gapRadius : band.radius;
            float phase = gap ? band.gapPhase : band.phase;
            float slotAngle = Mathf.PI * 2f / Mathf.Max(1, count);
            int seed = Hash(generation.seed, band.seedKey,
                gap ? -slot - 1 : slot);
            var slotRng = new System.Random(seed);

            PlanetTypeId typeId = PickType(slotRng);
            PlanetTypeDef def = bootstrap.GetPlanetType(typeId);
            if (def == null)
                return null;

            float angleOffset = gap
                ? ((float)slotRng.NextDouble() - 0.5f) * slotAngle * 0.4f
                : 0f;
            float baseAngle = phase + slot * slotAngle + angleOffset;
            if (InSpawnClearZone(radius, baseAngle))
                return null;
            float swayAmplitude = gap
                ? Mathf.Min(generation.swayMax, 0.08f)
                : band.swayAmplitude;
            float swaySpeed = swayAmplitude > 0f
                ? Mathf.Lerp(generation.swaySpeedMin, generation.swaySpeedMax,
                    (float)slotRng.NextDouble())
                : 0f;
            float swayPhase = (float)slotRng.NextDouble() * Mathf.PI * 2f;
            float sizeRoll = 1f +
                ((float)slotRng.NextDouble() * 2f - 1f) * generation.sizeJitter;
            float worldRadius = Mathf.Max(0.005f,
                def.baseSize * config.sizeScale * Mathf.Max(0.05f, sizeRoll));

            PlanetRuntime planet = new PlanetRuntime
            {
                def = def,
                radius = radius,
                baseAngle = baseAngle,
                angle = baseAngle + Mathf.Sin(swayPhase) * swayAmplitude,
                worldRadius = worldRadius,
                swayPhase = swayPhase,
                swaySpeed = swaySpeed,
                swayAmplitude = swayAmplitude
            };
            planets.Add(planet);
            return planet;
        }

        private void RemovePlanet(PlanetRuntime planet)
        {
            if (planet == null)
                return;
            ReleasePlanetView(planet);
            planets.Remove(planet);
        }

        private void RemoveBandPlanets(BandView band)
        {
            if (band == null)
                return;
            foreach (PlanetRuntime planet in band.slots.Values)
                RemovePlanet(planet);
            foreach (PlanetRuntime planet in band.gapSlots.Values)
                RemovePlanet(planet);
            band.slots.Clear();
            band.gapSlots.Clear();
        }

        private float GetScreenHalfWidthWorld()
        {
            Camera camera = Camera.main;
            if (camera != null && camera.orthographic)
                return camera.orthographicSize * camera.aspect;
            return config.cameraOrthographicSize * 1.78f;
        }

        private static int Hash(int seed, int a, int b)
        {
            unchecked
            {
                int value = 17;
                value = value * 31 + seed;
                value = value * 31 + a;
                value = value * 31 + b;
                return value;
            }
        }

        private static float Hash01(int seed, int a, int b)
        {
            uint value = (uint)Hash(seed, a, b) & 0x7fffffffu;
            return value / (float)0x7fffffff;
        }

        private int GetRingCount(float radius)
        {
            int count = generation.GetRingCount(radius);
            if (currentLevel != null && currentLevel.IsEndless &&
                currentLevel.endless != null &&
                currentLevel.endless.densityByRadius != null)
            {
                float density = currentLevel.endless.densityByRadius.Evaluate(radius);
                density = Mathf.Clamp(density, 0.1f, currentLevel.endless.maxDensity);
                count = Mathf.RoundToInt(count * density);
                count = Mathf.Clamp(count,
                    generation.minPlanetsPerRing,
                    generation.maxPlanetsPerRing);
            }
            return count;
        }

        private void AddSafeCatchPlanet()
        {
            float radius = Mathf.Max(generation.minGenerationRadius,
                startRadius - 0.35f);
            AddPlanet(radius, 0f, PlanetTypeId.Rock, 0f);
        }

        private PlanetTypeId PickType()
        {
            return PickType(rng);
        }

        private PlanetTypeId PickType(System.Random source)
        {
            if (currentLevel != null && currentLevel.typeWeights != null &&
                currentLevel.typeWeights.Count > 0)
            {
                float levelTotal = 0f;
                for (int i = 0; i < currentLevel.typeWeights.Count; i++)
                {
                    LevelTypeWeight entry = currentLevel.typeWeights[i];
                    if (entry != null && entry.type != null)
                        levelTotal += Mathf.Max(0f, entry.weight);
                }

                if (levelTotal > 0f)
                {
                    float levelRoll = (float)source.NextDouble() * levelTotal;
                    for (int i = 0; i < currentLevel.typeWeights.Count; i++)
                    {
                        LevelTypeWeight entry = currentLevel.typeWeights[i];
                        if (entry == null || entry.type == null)
                            continue;
                        levelRoll -= Mathf.Max(0f, entry.weight);
                        if (levelRoll <= 0f)
                            return entry.type.id;
                    }
                }
            }

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

            float roll = (float)source.NextDouble() * total;
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

            float sizeRoll = 1f + ((float)rng.NextDouble() * 2f - 1f) *
                             generation.sizeJitter;
            float worldRadius = Mathf.Max(0.005f, def.baseSize * config.sizeScale * sizeRoll);
            float swaySpeed = swayAmplitude > 0f
                ? Mathf.Lerp(generation.swaySpeedMin, generation.swaySpeedMax,
                    (float)rng.NextDouble())
                : 0f;
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

            float r = Mathf.Max(0.05f, radius);
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
            float radialDelta = radius - startRadius;
            if (radialDelta < -0.5f || radialDelta > 0.35f)
                return false;
            return Mathf.Abs(PolarMath.NormalizeAngle(angle)) < 0.28f;
        }

        private BandView AddBandView(float radius)
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

            BandView band = new BandView
            {
                radius = radius,
                line = line,
                material = material
            };
            bands.Add(band);
            return band;
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
                band.line.enabled = config.showRingLines &&
                    Mathf.Abs(band.radius - playerRadius) <= RingVisibleRadius;
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
