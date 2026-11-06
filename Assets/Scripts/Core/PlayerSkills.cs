using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 初期 Demo 的三个技能：无双、炸弹、钩锁。当前全部默认解锁。
    /// 技能只修改玩家/世界，不直接播放表现；通过事件交给 Feedback/VFX/Audio。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSkills : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        private GameConfig config;
        private PlayerController player;
        private PlayerInputReader input;
        private ScoreSystem score;
        private PlanetWorld world;
        private GameEventBus events;
        private bool initialized;

        private PlanetRuntime hookTarget;
        private float hookMinDistance;
        private float hookSpeedCap;
        private float bombCooldown;
        private float hookCooldown;
        private float musouRemaining;
        private bool musouActive;
        private bool musouEnabled = true;
        private bool bombEnabled = true;
        private bool hookEnabled = true;

        public bool IsMusouActive => musouActive;
        public float MusouRemaining => musouRemaining;
        public bool IsHookActive => hookTarget != null;
        public PlanetRuntime HookTarget => hookTarget;
        public float BombCooldownRemaining => bombCooldown;
        public float HookCooldownRemaining => hookCooldown;
        public int MusouScore => score != null ? score.Score : 0;
        public bool MusouEnabled => musouEnabled;
        public bool BombEnabled => bombEnabled;
        public bool HookEnabled => hookEnabled;

        public void SetSkillFlags(bool musou, bool bomb, bool hook)
        {
            musouEnabled = musou;
            bombEnabled = bomb;
            hookEnabled = hook;

            if (!musouEnabled && musouActive)
            {
                musouActive = false;
                musouRemaining = 0f;
                events?.Emit(new GameEvent(GameEventType.MusouEnd,
                    player != null ? player.State.WorldPosition : Vector2.zero));
            }
            if (!hookEnabled)
                ReleaseHook();
        }

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null)
                return;

            config = bootstrap.gameConfig;
            player = bootstrap.player;
            input = bootstrap.inputReader;
            score = bootstrap.score;
            world = bootstrap.planetWorld;
            events = bootstrap.Events;
            initialized = true;
            ResetSkills();
        }

        public void ResetSkills()
        {
            musouActive = false;
            musouRemaining = 0f;
            bombCooldown = 0f;
            hookCooldown = 0f;
            hookTarget = null;
            hookMinDistance = 0f;
            hookSpeedCap = config != null ? config.hookMaxSpeed : 1.9f;
        }

        /// <summary>由 PlayerController 在每步移动前调用。</summary>
        public void TickSkills(PlayerState state, float dt)
        {
            if (!initialized || config == null)
                return;

            if (bombCooldown > 0f)
                bombCooldown = Mathf.Max(0f, bombCooldown - dt);
            if (hookCooldown > 0f)
                hookCooldown = Mathf.Max(0f, hookCooldown - dt);

            if (musouActive)
            {
                musouRemaining -= dt;
                if (musouRemaining <= 0f)
                {
                    musouActive = false;
                    musouRemaining = 0f;
                    events?.Emit(new GameEvent(GameEventType.MusouEnd, player.State.WorldPosition));
                }
                else if (config.musouScoreThreshold > 0 &&
                         score != null && score.Score >= config.musouScoreThreshold &&
                         score.TryConsume(config.musouScoreThreshold))
                {
                    musouRemaining = config.musouDuration;
                }
            }
            else if (musouEnabled &&
                     config.musouScoreThreshold > 0 &&
                     score != null && score.Score >= config.musouScoreThreshold)
            {
                if (score.TryConsume(config.musouScoreThreshold))
                    ActivateMusou();
            }

            // 无双期间禁用其他技能；关卡未开放的技能也不响应输入。
            if (!musouActive && input != null)
            {
                if (bombEnabled && input.BombPressed)
                    TryPlaceBomb();
                if (hookEnabled && input.HookPressed)
                    TryHook();
            }

            if (hookTarget != null && (!hookTarget.alive || hookTarget.def == null))
                ReleaseHook();
        }

        /// <summary>由 PlayerController 在径向积分前调用。</summary>
        public void ApplyHookForce(PlayerState state, float dt)
        {
            if (!hookEnabled || hookTarget == null || player == null || config == null)
                return;
            if (!hookTarget.alive)
            {
                ReleaseHook();
                return;
            }

            Vector2 playerPosition = state.WorldPosition;
            if (IsBlockedBySolidPlanet(playerPosition))
            {
                ReleaseHook();
                return;
            }

            Vector2 targetPosition = hookTarget.WorldPosition;
            Vector2 delta = targetPosition - playerPosition;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
                return;

            if (distance > config.hookBreakDistance)
            {
                ReleaseHook();
                return;
            }

            float worldAngle = state.WorldAngle;
            Vector2 radial = new Vector2(Mathf.Cos(worldAngle), Mathf.Sin(worldAngle));
            Vector2 tangent = new Vector2(-radial.y, radial.x);
            Vector2 direction = delta / distance;

            // 网页 demo：越接近目标拉力越强，避免绕着目标转圈。
            float nearBoost = distance < 0.30f ? 7f : distance < 0.80f ? 4f : 1f;
            float acceleration = config.hookAcceleration * nearBoost;
            float radialAcceleration = Vector2.Dot(direction, radial) * acceleration;
            float tangentAcceleration = Vector2.Dot(direction, tangent) * acceleration;

            state.radialVelocity += radialAcceleration * dt;
            // 世界角度 = -θ，切向世界加速度对应的是 -dω，取反才是朝目标拉。
            state.angularVelocity -=
                tangentAcceleration / Mathf.Max(0.3f, state.radius) * dt;

            float tangentialVelocity = state.angularVelocity * state.radius;
            float totalSpeed = Mathf.Sqrt(
                state.radialVelocity * state.radialVelocity +
                tangentialVelocity * tangentialVelocity);
            float speedCap = hookSpeedCap > 0.0001f
                ? hookSpeedCap
                : config.hookMaxSpeed;
            if (totalSpeed > speedCap && totalSpeed > 0.0001f)
            {
                float scale = speedCap / totalSpeed;
                state.radialVelocity *= scale;
                state.angularVelocity *= scale;
            }

            if (distance < hookMinDistance)
                hookMinDistance = distance;
            else if (distance > hookMinDistance + config.hookLooseDistance)
                ReleaseHook();
        }

        /// <summary>
        /// 钩锁不应拖着玩家穿过固体星球。目标本人交给碰撞系统处理，
        /// 这里只检查挡路星球；即使对方处于受击冷却也会断钩。
        /// </summary>
        private bool IsBlockedBySolidPlanet(Vector2 playerPosition)
        {
            if (world == null || config == null)
                return false;

            float playerRadius = config.EffectivePlayerRadius;
            var planets = world.Planets;
            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                if (!planet.alive || planet == hookTarget || planet.def == null)
                    continue;
                if (planet.def.behavior == PlanetBehavior.Cloud)
                    continue;

                float contact = playerRadius + planet.worldRadius;
                if ((planet.WorldPosition - playerPosition).sqrMagnitude <=
                    contact * contact)
                {
                    return true;
                }
            }
            return false;
        }

        private void ActivateMusou()
        {
            if (!musouEnabled)
                return;
            musouActive = true;
            musouRemaining = config.musouDuration;

            // 进入无双时断开已有钩锁，避免无双期间仍被其他技能影响。
            if (hookTarget != null)
                ReleaseHook();

            events?.Emit(new GameEvent(GameEventType.MusouStart, player.State.WorldPosition));
        }

        private void TryPlaceBomb()
        {
            if (!bombEnabled || musouActive || bombCooldown > 0f ||
                world == null || player == null)
                return;

            float radius = player.State.radius - config.bombSkillSpawnBelow;
            PlanetRuntime bomb = world.SpawnSkillBomb(radius, player.State.WorldAngle);
            if (bomb == null)
                return;

            bombCooldown = config.bombSkillCooldown;
            events?.Emit(new GameEvent(GameEventType.BombPlaced, bomb.WorldPosition));
        }

        /// <summary>
        /// 网页 demo 版选目标：
        /// 1. 鼠标 48 逻辑像素内最近的一颗星球（按屏幕距离）；
        /// 2. 鼠标没指到星球时，回退到离玩家最近的一颗；
        /// 两种情况都要求世界距离在 hookRange 内，否则本次不勾。
        /// 只做查询，不修改技能状态，供输入和瞄准 UI 共用。
        /// </summary>
        public PlanetRuntime FindHookCandidateAtScreen(Vector2 screenPosition)
        {
            if (!initialized || !hookEnabled || world == null ||
                player == null || config == null)
            {
                return null;
            }

            PlayerState state = player.State;
            if (state.dead)
                return null;

            Vector2 playerPosition = state.WorldPosition;
            var planets = world.Planets;
            Camera camera = bootstrap != null && bootstrap.cameraRig != null
                ? bootstrap.cameraRig.ActiveCamera
                : Camera.main;

            if (camera != null)
            {
                float pickRadius = 48f * Screen.height / 720f;
                PlanetRuntime nearestToPointer = null;
                float nearestScreenDistance = float.PositiveInfinity;
                for (int i = 0; i < planets.Count; i++)
                {
                    PlanetRuntime planet = planets[i];
                    if (!planet.alive)
                        continue;

                    Vector2 world = planet.WorldPosition;
                    Vector3 screen = camera.WorldToScreenPoint(
                        new Vector3(world.x, world.y, 0f));
                    if (screen.z <= 0f)
                        continue;

                    float dx = screen.x - screenPosition.x;
                    float dy = screen.y - screenPosition.y;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance < nearestScreenDistance)
                    {
                        nearestScreenDistance = distance;
                        nearestToPointer = planet;
                    }
                }

                if (nearestToPointer != null && nearestScreenDistance < pickRadius)
                {
                    float worldDistance = Vector2.Distance(
                        playerPosition, nearestToPointer.WorldPosition);
                    return worldDistance <= config.hookRange ? nearestToPointer : null;
                }
            }

            // 空点：先取绝对最近的一颗，再检查范围（与 demo 的 hookPlanet 一致）。
            PlanetRuntime nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < planets.Count; i++)
            {
                PlanetRuntime planet = planets[i];
                if (!planet.alive)
                    continue;

                float distance = Vector2.Distance(playerPosition, planet.WorldPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = planet;
                }
            }

            return nearest != null && nearestDistance <= config.hookRange ? nearest : null;
        }

        private void TryHook()
        {
            if (!hookEnabled || musouActive || hookCooldown > 0f ||
                world == null || player == null)
                return;

            PlanetRuntime target = FindHookCandidateAtScreen(new Vector2(
                Input.mousePosition.x, Input.mousePosition.y));
            if (target == null)
                return;

            PlayerState state = player.State;
            Vector2 playerPosition = state.WorldPosition;
            hookTarget = target;
            hookMinDistance = Vector2.Distance(playerPosition, target.WorldPosition);
            // 网页 demo 的 1.5/1.9 比例改为跟随 hookMaxSpeed：
            // 否则把限速调高时，低速上绳仍会被写死的 1.5 卡住，调参看起来无效。
            float minSpeedCap = config.hookMaxSpeed * 0.79f;
            hookSpeedCap = Mathf.Min(config.hookMaxSpeed,
                Mathf.Max(minSpeedCap, Mathf.Abs(state.radialVelocity) * 1.05f));
            hookCooldown = config.hookCooldown;
            events?.Emit(new GameEvent(GameEventType.HookAttach, target.WorldPosition));
        }

        public void ReleaseHook()
        {
            if (hookTarget == null)
                return;
            hookTarget = null;
            events?.Emit(new GameEvent(GameEventType.HookRelease,
                player != null ? player.State.WorldPosition : Vector2.zero));
        }

        /// <summary>
        /// 无双撞碎星球后调用：只延长持续时间，无双期间不积分。
        /// 剩余时间最多封顶到一次完整无双时长，不能靠连续撞碎无限续杯。
        /// </summary>
        public void OnMusouSmash()
        {
            if (!musouActive || config == null)
                return;

            float cap = Mathf.Max(0f, config.musouDuration);
            musouRemaining = Mathf.Min(
                cap,
                musouRemaining + Mathf.Max(0f, config.musouExtension));
        }
    }
}
