using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 行星表现层：根节点负责世界位置/尺寸，Visual 子节点由 SpriteStateMachine
    /// 做状态切换和程序化缩放/位移/旋转。不修改行星仿真数据。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlanetView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteStateMachine stateMachine;

        public SpriteStateMachine StateMachine => stateMachine;

        private PlanetTypeDef definition;
        private float worldDiameter = 1f;

        private void Awake()
        {
            EnsureVisual();
        }

        public void Bind(PlanetTypeDef def)
        {
            definition = def;
            EnsureVisual();

            Sprite sprite = def != null && def.bodySprite != null
                ? def.bodySprite
                : ProceduralSprites.Circle;
            body.sprite = sprite;
            body.color = def != null ? def.color : Color.white;
            body.sortingOrder = 5;

            // 从对象池复用时，先把上一次状态的位移/缩放/旋转清掉。
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localScale = Vector3.one;
            visualRoot.localRotation = Quaternion.identity;

            SpriteStateProfile profile = SpriteStateDefaults.GetPlanetProfile(def);
            stateMachine.Initialize(profile, body, visualRoot);
            stateMachine.SetState("Idle");
            stateMachine.idleSwayAngle =
                def != null && def.idleSwayEnabled ? def.idleSwayAngle : 0f;
            stateMachine.idleSwayFrequency = def != null
                ? Mathf.Max(0.01f, def.idleSwayFrequency)
                : 0f;
            stateMachine.idleSwaySteps = def != null
                ? Mathf.Max(2, def.idleSwaySteps)
                : 7;
            stateMachine.idleSwayStepOffset = Random.Range(
                0, stateMachine.idleSwaySteps);
            ApplyDiameter();
        }

        public void SetWorldDiameter(float diameter)
        {
            worldDiameter = Mathf.Max(0.0001f, diameter);
            ApplyDiameter();
        }

        public void SetTint(Color color)
        {
            if (body != null)
                body.color = color;
        }

        public void PlayHit()
        {
            stateMachine?.PlayOneShot("Hit");
        }

        public void PlayShatter()
        {
            stateMachine?.PlayOneShot("Shattered");
        }

        public void SetState(string stateName)
        {
            stateMachine?.SetState(stateName);
        }

        private void LateUpdate()
        {
            if (definition == null || stateMachine == null)
                return;

            // 允许 T 面板实时调参；相位在 Bind 时已经随机分配，这里只同步幅度/频率。
            stateMachine.idleSwayAngle = definition.idleSwayEnabled
                ? definition.idleSwayAngle
                : 0f;
            stateMachine.idleSwayFrequency = Mathf.Max(
                0.01f, definition.idleSwayFrequency);
            stateMachine.idleSwaySteps = Mathf.Max(2, definition.idleSwaySteps);
        }

        private void ApplyDiameter()
        {
            if (body == null)
                return;
            float native = body.sprite != null ? Mathf.Max(0.0001f, body.sprite.bounds.size.x) : 1f;
            float scale = worldDiameter / native;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void EnsureVisual()
        {
            if (visualRoot != null && body != null && stateMachine != null)
                return;

            if (visualRoot == null)
            {
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(transform, false);
                visualRoot = visual.transform;
            }

            if (body == null)
                body = visualRoot.GetComponent<SpriteRenderer>();
            if (body == null)
                body = visualRoot.gameObject.AddComponent<SpriteRenderer>();

            if (stateMachine == null)
                stateMachine = visualRoot.GetComponent<SpriteStateMachine>();
            if (stateMachine == null)
                stateMachine = visualRoot.gameObject.AddComponent<SpriteStateMachine>();

            stateMachine.targetRenderer = body;
            stateMachine.motionTarget = visualRoot;
            stateMachine.CaptureBase();
        }
    }
}
