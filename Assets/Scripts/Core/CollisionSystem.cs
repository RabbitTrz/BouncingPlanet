using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Stage 2 扫掠圆碰撞。严格按 v1.1 口径：
    /// 普通撞击永不碎球；上升按 ascendBlockFactor 返回；冰一碰碎；
    /// 云加速；炸弹星只爆炸/推玩家/自身消失，不清场；不设上弹上限。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CollisionSystem : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private GameConfig config;
        private PlayerController player;
        private PlanetWorld field;
        private SimulationClock clock;
        private GameEventBus events;
        private ScoreSystem score;
        private PlayerSkills skills;
        private bool subscribed;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null)
                return;

            config = bootstrap.gameConfig;
            player = bootstrap.player;
            field = bootstrap.planetWorld;
            clock = bootstrap.clock;
            events = bootstrap.Events;
            score = bootstrap.score;
            skills = bootstrap.skills;

            if (clock != null && !subscribed)
            {
                clock.Tick += OnTick;
                subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (clock != null && subscribed)
            {
                clock.Tick -= OnTick;
                subscribed = false;
            }
        }

        private void OnTick(float dt)
        {
            if (config == null || player == null || field == null || player.IsDead)
                return;

            field.Step(dt);

            PlayerState state = player.State;
            Vector2 previous = player.HasPreviousPosition ? player.PreviousWorldPosition : state.WorldPosition;
            Vector2 current = state.WorldPosition;
            float playerRadius = config.EffectivePlayerRadius;
            var planets = field.Planets;
            bool ascendingSmashCandidate = config.normalSmashEnabled &&
                config.normalSmashSpeedThreshold > 0f &&
                state.radialVelocity >= config.normalSmashSpeedThreshold;

            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                // 钩锁目标即使处于受击冷却也要检测，否则会被钩锁拉穿；
                // 上升撞碎状态下同理，否则高速上升会从冷却中的星球穿过去。
                bool isHookTarget = skills != null && skills.HookTarget == planet;
                if (!planet.alive ||
                    (planet.cooldown > 0f && !isHookTarget && !ascendingSmashCandidate))
                {
                    continue;
                }

                float radialDistance = Mathf.Abs(planet.radius - state.radius);
                if (radialDistance > playerRadius + planet.worldRadius + 0.05f)
                    continue;

                Vector2 center = planet.WorldPosition;
                float distance = SegmentCircleDistance(previous, current, center);
                if (distance > playerRadius + planet.worldRadius)
                    continue;

                Resolve(planet, state, center, current);
                break;
            }
        }

        private void Resolve(PlanetRuntime planet, PlayerState state, Vector2 center, Vector2 playerPosition)
        {
            PlanetTypeDef def = planet.def;
            if (def == null)
                return;

            float impactSpeed = Mathf.Abs(state.radialVelocity);
            Vector2 impactPoint = (playerPosition + center) * 0.5f;
            bool hooked = skills != null && skills.HookTarget == planet;
            bool heavy = hooked && impactSpeed >= config.hookHeavySpeed;
            bool ascendingSmash = config.normalSmashEnabled &&
                config.normalSmashSpeedThreshold > 0f &&
                state.radialVelocity >= config.normalSmashSpeedThreshold;

            // 撞到任何固体星球都断钩：只有云朵允许继续穿过。
            if (skills != null && skills.IsHookActive &&
                (hooked || def.behavior != PlanetBehavior.Cloud))
            {
                skills.ReleaseHook();
            }

            // 碰撞表情：按碰撞前的径向方向选 up/down；无双期间由 override 接管。
            if (skills == null || !skills.IsMusouActive)
            {
                player?.NotifyCollisionReaction(state.radialVelocity >= 0f);
                // 上升撞碎会直接穿过星球，不额外给横向冲量。
                if (!ascendingSmash)
                    ApplyLateralCollisionImpulse(state, center, playerPosition, def);
            }

            planet.cooldown = config.planetHitCooldown;
            planet.flash = 1f;
            planet.viewComponent?.PlayHit();

            // 无双优先：任何路径星球直接碎，不进特殊星球分支（包括云朵）。
            if (skills != null && skills.IsMusouActive)
            {
                SmashPlanet(planet, def, impactPoint, impactSpeed);
                skills.OnMusouSmash();
                return;
            }

            // 普通上升撞碎：达到阈值后，撞到的任何星球都按无双逻辑碎掉。
            if (ascendingSmash)
            {
                SmashPlanet(planet, def, impactPoint, impactSpeed);
                AddSmashScore(def);
                return;
            }

            switch (def.behavior)
            {
                case PlanetBehavior.Cloud:
                    planet.cooldown = config.cloudHitCooldown;
                    float sign = state.radialVelocity < 0f ? -1f : 1f;
                    // 加算：原速度 + 固定加值，不设上限。
                    state.radialVelocity = sign *
                        (impactSpeed + Mathf.Max(0f, def.cloudSpeedMultiplier));
                    AddScore(def);
                    events.Emit(new GameEvent(GameEventType.CloudPass, impactPoint,
                        impactSpeed, def.id, heavy: heavy));
                    return;

                case PlanetBehavior.Bomb:
                    planet.alive = false;
                    planet.deathViewTimer = 0.28f;
                    planet.viewComponent?.PlayShatter();

                    // 自然炸弹星和技能炸弹现在使用同一套清场规则。
                    field.ExplodeArea(center, config.bombSkillExplosionRadius, planet);

                    // 技能炸弹和自然炸弹统一走同一套径向推力：
                    // 炸弹在玩家外侧（上方）时向下炸，在内侧（下方）时向上炸。
                    // 全程不改角速度，避免爆炸自旋。
                    float pushDirection = GetBombPushDirection(state, planet);
                    float pushSpeed = planet.fromBombSkill
                        ? config.bombSkillPushSpeed
                        : def.bombPushSpeed;
                    ApplyRadialBombPush(state, pushDirection, pushSpeed);

                    AddScore(def);
                    events.Emit(new GameEvent(GameEventType.BombExplode, impactPoint,
                        impactSpeed, def.id));
                    return;

                case PlanetBehavior.Ice:
                    bool descendingIce = state.radialVelocity < 0f;
                    if (descendingIce)
                    {
                        float iceRaw = impactSpeed * config.bounceGain + config.baseImpulse;
                        // 冰星使用专用弹力加成，不再被通用的 bounceMultiplier 覆盖。
                        iceRaw *= Mathf.Max(0f, def.iceBounceBonus);
                        state.radialVelocity = config.ClampLaunch(iceRaw);
                        events.Emit(new GameEvent(GameEventType.Bounce, impactPoint,
                            impactSpeed, def.id, heavy: heavy));
                    }
                    else
                    {
                        state.radialVelocity = -impactSpeed * config.ascendBlockFactor;
                        events.Emit(new GameEvent(GameEventType.Wall, impactPoint,
                            impactSpeed, def.id, heavy: heavy));
                    }

                    if (def.iceBreaksOnFirstHit)
                    {
                        planet.alive = false;
                        planet.deathViewTimer = 0.28f;
                        planet.viewComponent?.PlayShatter();
                    }
                    else
                    {
                        planet.cooldown = config.planetHitCooldown;
                    }
                    AddScore(def);
                    events.Emit(new GameEvent(GameEventType.IceCrack, impactPoint,
                        impactSpeed, def.id));
                    return;

                default:
                    if (state.radialVelocity < 0f)
                    {
                        float raw = impactSpeed * config.bounceGain + config.baseImpulse;
                        raw *= Mathf.Max(0f, def.bounceMultiplier);
                        state.radialVelocity = config.ClampLaunch(raw);
                        AddScore(def);
                        events.Emit(new GameEvent(GameEventType.Bounce, impactPoint,
                            impactSpeed, def.id, heavy: heavy));
                    }
                    else
                    {
                        state.radialVelocity = -impactSpeed * config.ascendBlockFactor;
                        AddScore(def);
                        events.Emit(new GameEvent(GameEventType.Wall, impactPoint,
                            impactSpeed, def.id, heavy: heavy));
                    }
                    return;
            }
        }

        private void AddScore(PlanetTypeDef def)
        {
            int delta = Mathf.RoundToInt(config.normalPlanetScore * def.scoreMultiplier);
            if (delta == 0 || score == null)
                return;
            score.Add(delta);
            events.Emit(GameEvent.Score(delta));
        }

        private void AddSmashScore(PlanetTypeDef def)
        {
            int delta = Mathf.RoundToInt(config.smashScore * Mathf.Max(0f, def.scoreMultiplier));
            if (delta == 0 || score == null)
                return;
            score.Add(delta);
            events.Emit(GameEvent.Score(delta));
        }

        private void SmashPlanet(PlanetRuntime planet, PlanetTypeDef def,
            Vector2 impactPoint, float impactSpeed)
        {
            planet.alive = false;
            planet.deathViewTimer = 0.28f;
            planet.viewComponent?.PlayShatter();
            events.Emit(new GameEvent(GameEventType.Smash, impactPoint,
                impactSpeed, def.id));
        }

        /// <summary>
        /// 爆炸方向与玩家当前的径向运动相反：
        /// 从下往上撞（vr &gt; 0）时向下炸，从上面落下时向上炸。
        /// 径向速度接近 0 时，再按炸弹在玩家内侧/外侧决定。
        /// </summary>
        private static float GetBombPushDirection(PlayerState state, PlanetRuntime planet)
        {
            if (state.radialVelocity > 0.0001f)
                return -1f;
            if (state.radialVelocity < -0.0001f)
                return 1f;
            return state.radius >= planet.radius ? 1f : -1f;
        }

        /// <summary>
        /// 只修改径向速度，保留角速度。与原来的 `max(vr, 0) + pushSpeed`
        /// 是同一套逻辑，只是允许推力方向为内/外。
        /// </summary>
        private static void ApplyRadialBombPush(PlayerState state, float direction,
            float pushSpeed)
        {
            float along = state.radialVelocity * direction;
            state.radialVelocity = direction *
                (Mathf.Max(0f, along) + Mathf.Max(0f, pushSpeed));
        }

        /// <summary>
        /// 普通星/冰冰星的侧向冲量：按碰撞法线的切向分量给一点切向速度。
        /// 云朵是穿过加速、炸弹是爆炸推力，都不参与；无双由撞碎分支接管。
        /// </summary>
        private void ApplyLateralCollisionImpulse(PlayerState state, Vector2 planetCenter,
            Vector2 playerPosition, PlanetTypeDef def)
        {
            if (!config.collisionLateralEnabled || def == null)
                return;
            if (def.behavior == PlanetBehavior.Cloud ||
                def.behavior == PlanetBehavior.Bomb)
                return;

            Vector2 normal = playerPosition - planetCenter;
            if (normal.sqrMagnitude < 1e-8f)
                return;
            normal /= normal.magnitude;

            float worldAngle = state.WorldAngle;
            Vector2 radial = new Vector2(Mathf.Cos(worldAngle), Mathf.Sin(worldAngle));
            Vector2 tangent = new Vector2(-radial.y, radial.x);
            float side = Vector2.Dot(normal, tangent);
            if (Mathf.Abs(side) < 0.0001f)
                return;

            float radius = Mathf.Max(0.3f, state.radius);
            float existingTangential = -state.angularVelocity * radius;
            float totalSpeed = Mathf.Sqrt(
                state.radialVelocity * state.radialVelocity +
                existingTangential * existingTangential);
            float kick = side * Mathf.Min(
                totalSpeed * config.collisionLateralFactor,
                config.collisionLateralMaxSpeed);
            if (Mathf.Abs(kick) < 0.0001f)
                return;

            // 不把玩家推到正常操控上限之外，避免侧撞产生失控角速度。
            float normalCap = Mathf.Min(
                config.tangentialSpeed,
                config.maxAngularSpeed * radius);
            float maxTangential = Mathf.Max(
                Mathf.Abs(existingTangential), normalCap);
            float newTangential = Mathf.Clamp(
                existingTangential + kick, -maxTangential, maxTangential);
            state.angularVelocity = -newTangential / radius;
        }

        private static float SegmentCircleDistance(Vector2 start, Vector2 end, Vector2 center)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float t = 0f;
            if (lengthSquared > 1e-12f)
            {
                t = Vector2.Dot(center - start, segment) / lengthSquared;
                t = Mathf.Clamp01(t);
            }

            Vector2 closest = start + segment * t;
            return Vector2.Distance(closest, center);
        }
    }
}
