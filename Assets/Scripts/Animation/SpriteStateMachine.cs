using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 自己写的轻量状态机：
    /// - 静态 Sprite 状态/帧序列切换；
    /// - 每个状态可用 AnimationCurve 生成位移、缩放、旋转；
    /// - 一次性状态自动回到上一个状态。
    /// 不依赖 Unity Animator，也不修改游戏判定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteStateMachine : MonoBehaviour
    {
        public SpriteStateProfile profile;
        public SpriteRenderer targetRenderer;
        public Transform motionTarget;
        public bool playDefaultOnEnable = true;
        public bool useUnscaledTime = true;

        [Header("Idle Sway")]
        [Tooltip("Idle 状态下额外叠加的左右摆动角度；0 表示关闭。")]
        public float idleSwayAngle;
        [Tooltip("待机摆动频率（Hz）。默认约等于黑洞 7 帧 / 10fps 的循环频率。")]
        public float idleSwayFrequency = 1.43f;
        [Min(2)]
        [Tooltip("一个循环使用几个离散姿势，形成帧动画式摆动。")]
        public int idleSwaySteps = 7;
        [Tooltip("每个实例的起始步偏移，用于错开摆动。")]
        public int idleSwayStepOffset;

        public string CurrentStateName { get; private set; }
        public SpriteStateDefinition CurrentState { get; private set; }
        public event Action<string> StateChanged;

        private float stateTime;
        private int frameIndex = -1;
        private string returnStateName;

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale = Vector3.one;
        private Quaternion baseLocalRotation = Quaternion.identity;
        private bool baseCaptured;

        private void Awake()
        {
            ResolveReferences();
            CaptureBase();
        }

        private void OnEnable()
        {
            if (playDefaultOnEnable && CurrentState == null && profile != null)
                SetState(profile.defaultState);
        }

        public void Initialize(SpriteStateProfile stateProfile, SpriteRenderer renderer, Transform motion)
        {
            profile = stateProfile;
            targetRenderer = renderer;
            motionTarget = motion;
            ResolveReferences();
            CaptureBase();
            CurrentState = null;
            CurrentStateName = null;
            returnStateName = null;
            if (profile != null)
                SetState(profile.defaultState);
        }

        public void SetState(string stateName, bool restart = false)
        {
            if (profile == null)
                return;
            SpriteStateDefinition state = profile.Find(stateName);
            if (state == null)
                return;
            if (!restart && CurrentState == state)
                return;

            returnStateName = null;
            Activate(state);
        }

        public void PlayOneShot(string stateName)
        {
            if (profile == null)
                return;
            SpriteStateDefinition state = profile.Find(stateName);
            if (state == null)
                return;

            returnStateName = CurrentState != null ? CurrentState.stateName : profile.defaultState;
            Activate(state);
        }

        private void Activate(SpriteStateDefinition state)
        {
            CurrentState = state;
            CurrentStateName = state.stateName;
            stateTime = 0f;
            frameIndex = -1;
            ApplySpriteForTime(0f);
            StateChanged?.Invoke(CurrentStateName);
        }

        private void Update()
        {
            if (CurrentState == null)
                return;
            Tick(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (CurrentState == null)
                return;

            stateTime += Mathf.Max(0f, dt);
            float duration = Mathf.Max(0.0001f, CurrentState.StateDuration);
            float normalized = stateTime / duration;

            ApplySpriteForTime(normalized);
            ApplyMotion(normalized);

            if (!CurrentState.loop && normalized >= 1f)
            {
                if (returnStateName != null && CurrentState.autoReturn)
                {
                    string next = returnStateName;
                    returnStateName = null;
                    SetState(next, true);
                }
            }
        }

        private void ApplySpriteForTime(float normalized)
        {
            if (targetRenderer == null || CurrentState == null || !CurrentState.HasFrames)
                return;

            Sprite[] frames = CurrentState.frames;
            int index;
            if (CurrentState.loop)
            {
                int fpsIndex = CurrentState.fps > 0f
                    ? Mathf.FloorToInt(stateTime * CurrentState.fps)
                    : 0;
                index = Mathf.Abs(fpsIndex) % frames.Length;
            }
            else
            {
                int fpsIndex = CurrentState.fps > 0f
                    ? Mathf.FloorToInt(stateTime * CurrentState.fps)
                    : 0;
                index = Mathf.Clamp(fpsIndex, 0, frames.Length - 1);
            }

            if (index == frameIndex)
                return;
            frameIndex = index;
            if (frames[index] != null)
                targetRenderer.sprite = frames[index];
        }

        private void ApplyMotion(float normalized)
        {
            if (motionTarget == null || CurrentState == null)
                return;

            float t = CurrentState.loop ? Mathf.Repeat(normalized, 1f) : Mathf.Clamp01(normalized);
            float ox = Evaluate(CurrentState.offsetX, t, 0f) + CurrentState.baseOffset.x;
            float oy = Evaluate(CurrentState.offsetY, t, 0f) + CurrentState.baseOffset.y;

            // Unity 在 Inspector 里添加列表元素时可能把 baseScale 序列化成 (0,0)；
            // 这种未初始化值按 1 处理，避免整张 Sprite 被缩成不可见。
            Vector2 baseScale = CurrentState.baseScale;
            if (Mathf.Approximately(baseScale.x, 0f) &&
                Mathf.Approximately(baseScale.y, 0f))
            {
                baseScale = Vector2.one;
            }

            float sx = baseScale.x * Evaluate(CurrentState.scaleX, t, 1f);
            float sy = baseScale.y * Evaluate(CurrentState.scaleY, t, 1f);
            float rz = CurrentState.baseRotation + Evaluate(CurrentState.rotation, t, 0f);

            // 待机摆动按离散帧推进，不做连续插值，呈现帧动画的顿挫感。
            if (CurrentStateName == "Idle" && idleSwayAngle != 0f &&
                idleSwayFrequency > 0f)
            {
                float time = useUnscaledTime ? Time.unscaledTime : Time.time;
                int steps = Mathf.Max(2, idleSwaySteps);
                int step = Mathf.FloorToInt(time * idleSwayFrequency * steps) +
                           idleSwayStepOffset;
                step %= steps;
                if (step < 0)
                    step += steps;

                float steppedPhase = step / (float)steps * Mathf.PI * 2f;
                rz += Mathf.Sin(steppedPhase) * idleSwayAngle;
            }

            motionTarget.localPosition = baseLocalPosition + new Vector3(ox, oy, 0f);
            motionTarget.localScale = new Vector3(
                baseLocalScale.x * sx,
                baseLocalScale.y * sy,
                baseLocalScale.z);
            motionTarget.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, rz);
        }

        private static float Evaluate(AnimationCurve curve, float t, float fallback)
        {
            if (curve == null || curve.length == 0)
                return fallback;
            return curve.Evaluate(t);
        }

        public void CaptureBase()
        {
            if (motionTarget == null)
                return;
            baseLocalPosition = motionTarget.localPosition;
            baseLocalScale = motionTarget.localScale;
            baseLocalRotation = motionTarget.localRotation;
            baseCaptured = true;
        }

        private void ResolveReferences()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            if (motionTarget == null)
                motionTarget = targetRenderer != null ? targetRenderer.transform : transform;
            if (!baseCaptured)
                CaptureBase();
        }
    }
}
