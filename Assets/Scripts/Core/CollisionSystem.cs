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

            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                if (!planet.alive || planet.cooldown > 0f)
                    continue;

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

            planet.cooldown = config.planetHitCooldown;
            planet.flash = 1f;

            switch (def.behavior)
            {
                case PlanetBehavior.Cloud:
                    planet.cooldown = config.cloudHitCooldown;
                    float sign = state.radialVelocity < 0f ? -1f : 1f;
                    state.radialVelocity = sign * Mathf.Min(
                        impactSpeed * def.cloudSpeedMultiplier,
                        def.cloudSpeedCap);
                    events.Emit(new GameEvent(GameEventType.CloudPass, impactPoint,
                        impactSpeed, def.id));
                    return;

                case PlanetBehavior.Bomb:
                    planet.alive = false;
                    state.radialVelocity = Mathf.Max(state.radialVelocity, 0f) + def.bombPushSpeed;
                    events.Emit(new GameEvent(GameEventType.BombExplode, impactPoint,
                        impactSpeed, def.id));
                    return;

                case PlanetBehavior.Ice:
                    bool descendingIce = state.radialVelocity < 0f;
                    if (descendingIce)
                    {
                        float iceRaw = impactSpeed * config.bounceGain + config.baseImpulse;
                        iceRaw *= Mathf.Max(0f, def.bounceMultiplier);
                        state.radialVelocity = config.ClampLaunch(iceRaw);
                        events.Emit(new GameEvent(GameEventType.Bounce, impactPoint,
                            impactSpeed, def.id));
                    }
                    else
                    {
                        state.radialVelocity = -impactSpeed * config.ascendBlockFactor;
                        events.Emit(new GameEvent(GameEventType.Wall, impactPoint,
                            impactSpeed, def.id));
                    }

                    if (def.iceBreaksOnFirstHit)
                        planet.alive = false;
                    else
                        planet.cooldown = config.planetHitCooldown;
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
                            impactSpeed, def.id));
                    }
                    else
                    {
                        state.radialVelocity = -impactSpeed * config.ascendBlockFactor;
                        AddScore(def);
                        events.Emit(new GameEvent(GameEventType.Wall, impactPoint,
                            impactSpeed, def.id));
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
