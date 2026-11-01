using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 固定步长仿真时钟。默认 1/120s，不受 Time.timeScale 影响。
    /// 顿帧/暂停只停止逻辑步进，渲染和 UI 继续。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationClock : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        public event Action<float> Tick;

        public bool IsPaused { get; private set; }
        public float SimulationTime { get; private set; }
        public float HitstopRemaining { get; private set; }
        public float SlowMotionScale { get; private set; } = 1f;
        public int StepsLastFrame { get; private set; }
        public float FixedDelta => config != null ? config.FixedDeltaTime : 1f / 120f;
        public GameConfig Config => config;

        private float accumulator;

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            accumulator = 0f;
            StepsLastFrame = 0;
        }

        public void ResetClock()
        {
            SimulationTime = 0f;
            accumulator = 0f;
            HitstopRemaining = 0f;
            SlowMotionScale = 1f;
            IsPaused = false;
            StepsLastFrame = 0;
        }

        private void Update()
        {
            if (IsPaused || SlowMotionScale <= 0f)
                return;

            float unscaledDelta = Time.unscaledDeltaTime;

            if (HitstopRemaining > 0f)
            {
                HitstopRemaining = Mathf.Max(0f, HitstopRemaining - unscaledDelta);
                StepsLastFrame = 0;
                return;
            }

            accumulator += Mathf.Min(unscaledDelta * SlowMotionScale, 0.05f);

            int maxSteps = config != null ? Mathf.Max(1, config.maxStepsPerFrame) : 8;
            int steps = 0;
            float fixedDelta = FixedDelta;
            while (accumulator >= fixedDelta && steps < maxSteps)
            {
                SimulateStep(fixedDelta);
                accumulator -= fixedDelta;
                steps++;
            }

            StepsLastFrame = steps;
            if (steps >= maxSteps)
                accumulator = 0f;
        }

        private void SimulateStep(float dt)
        {
            SimulationTime += dt;
            Tick?.Invoke(dt);
        }

        public void RequestHitstop(float seconds)
        {
            if (seconds <= 0f)
                return;
            HitstopRemaining = Mathf.Max(HitstopRemaining, seconds);
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public void SetSlowMotion(float scale)
        {
            SlowMotionScale = Mathf.Clamp(scale, 0f, 1f);
        }

        public void StepOnce()
        {
            if (!IsPaused)
                IsPaused = true;
            SimulateStep(FixedDelta);
            StepsLastFrame = 1;
        }
    }
}
