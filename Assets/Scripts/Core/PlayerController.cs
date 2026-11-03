using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 真实极坐标移动：左右惯性、恒定引力、径向积分、死亡与重生、
    /// 固定镜头所需的 previous/current 位置。具体流程由 GameFlow 控制。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        public PlayerInputReader input;
        public PlayerSkills skills;
        [Tooltip("调试用：锁定在初始半径，方便长时间测试左右环绕。")]
        public bool holdRadiusForTesting = false;

        [SerializeField] private PlayerState state = new PlayerState();

        private GameConfig config;
        private SimulationClock clock;
        private bool subscribed;
        private bool hasPreviousPosition;
        private Vector2 previousWorldPosition;

        public PlayerState State => state;
        public bool IsDead => state.dead;
        public bool IsHoldingRadius => holdRadiusForTesting;
        public bool HasPreviousPosition => hasPreviousPosition;
        public Vector2 PreviousWorldPosition => previousWorldPosition;
        public event Action<PlayerState> Died;
        public event Action Respawned;
        public event Action<bool> CollisionReaction;

        /// <summary>由碰撞系统调用；ascending = 碰撞前的径向速度是否向上。</summary>
        public void NotifyCollisionReaction(bool ascending)
        {
            CollisionReaction?.Invoke(ascending);
        }

        public void Initialize(GameBootstrap gameBootstrap, PlayerInputReader inputReader)
        {
            bootstrap = gameBootstrap;
            input = inputReader;
            config = bootstrap != null ? bootstrap.gameConfig : null;
            clock = bootstrap != null ? bootstrap.clock : null;

            if (clock != null && !subscribed)
            {
                clock.Tick += OnTick;
                subscribed = true;
            }

            state.ResetRun(config);
            CapturePreviousPosition();
        }

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
        }

        private void OnDestroy()
        {
            if (clock != null && subscribed)
            {
                clock.Tick -= OnTick;
                subscribed = false;
            }
        }

        private void Update()
        {
            if (bootstrap == null)
                return;

            if (input != null && input.ToggleHoldPressed)
                holdRadiusForTesting = !holdRadiusForTesting;
        }

        private void OnTick(float dt)
        {
            if (config == null || state.dead)
                return;

            CapturePreviousPosition();
            skills?.TickSkills(state, dt);

            float inputAxis = input != null ? input.Horizontal : 0f;
            float radiusForTurn = Mathf.Max(0.3f, state.radius);
            float maxAngularSpeed = Mathf.Min(config.maxAngularSpeed, config.tangentialSpeed / radiusForTurn);

            if (Mathf.Abs(inputAxis) < 0.02f)
            {
                float glide = config.glideDeceleration / radiusForTurn;
                state.angularVelocity = Mathf.MoveTowards(state.angularVelocity, 0f, glide * dt);
            }
            else
            {
                float target = inputAxis * maxAngularSpeed;
                float rate = state.angularVelocity * inputAxis < 0f
                    ? config.brakeDeceleration / radiusForTurn
                    : config.turnAcceleration / radiusForTurn;
                state.angularVelocity = Mathf.MoveTowards(state.angularVelocity, target, rate * dt);
            }

            bool musou = skills != null && skills.IsMusouActive;
            if (musou)
                state.radialVelocity = config.musouSpeed;
            else
                state.radialVelocity -= config.gravity * dt;

            skills?.ApplyHookForce(state, dt);
            if (musou)
                state.radialVelocity = config.musouSpeed;

            state.theta += state.angularVelocity * dt;
            state.radius += state.radialVelocity * dt;

            if (holdRadiusForTesting)
            {
                state.radius = config.startRadius;
                state.radialVelocity = 0f;
            }

            if (state.radius > state.maxRadius)
                state.maxRadius = state.radius;

            if (!holdRadiusForTesting && !musou && state.radius < config.deathRadius)
                Die();
        }

        private void Die()
        {
            if (state.dead)
                return;

            state.dead = true;
            state.deaths++;
            state.radialVelocity = 0f;
            state.angularVelocity = 0f;
            bootstrap?.Events?.Emit(GameEvent.Death(state.WorldPosition));
            Died?.Invoke(state);
        }

        public void Respawn()
        {
            state.Reset(config);
            CapturePreviousPosition();
            Respawned?.Invoke();
        }

        public void SetHoldRadius(bool hold)
        {
            holdRadiusForTesting = hold;
        }

        private void CapturePreviousPosition()
        {
            previousWorldPosition = state.WorldPosition;
            hasPreviousPosition = true;
        }
    }
}
