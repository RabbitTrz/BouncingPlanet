using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 关卡内玩家分层渲染：7 个 SpriteRenderer 跟随 SpriteStateMachine 的状态切换，
    /// 每层 Sprite 来自 Wardrobe。根节点的位移/缩放/旋转仍由状态机驱动。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LayeredPlayerRenderer : MonoBehaviour
    {
        public SpriteStateMachine stateMachine;
        public SpriteRenderer[] layers;

        private Wardrobe wardrobe;
        private string currentState = "happy";
        private bool initialized;

        public void Initialize(SpriteStateMachine machine)
        {
            if (initialized)
                return;
            initialized = true;

            stateMachine = machine;
            if (stateMachine != null)
            {
                stateMachine.StateChanged += OnStateChanged;
                currentState = stateMachine.CurrentStateName;
            }

            GameBootstrap bootstrap = GameBootstrap.Instance;
            wardrobe = bootstrap != null ? bootstrap.wardrobe : null;
            if (wardrobe != null)
                wardrobe.Changed += Apply;

            Apply();
        }

        private void OnDestroy()
        {
            if (stateMachine != null)
                stateMachine.StateChanged -= OnStateChanged;
            if (wardrobe != null)
                wardrobe.Changed -= Apply;
        }

        private void OnStateChanged(string stateName)
        {
            currentState = stateName;
            Apply();
        }

        private void Apply()
        {
            if (wardrobe == null || layers == null || layers.Length == 0)
                return;

            Sprite[] sprites = wardrobe.GetLayers(currentState);
            for (int i = 0; i < layers.Length; i++)
            {
                SpriteRenderer renderer = layers[i];
                if (renderer == null)
                    continue;
                Sprite sprite = i < sprites.Length ? sprites[i] : null;
                renderer.sprite = sprite;
                renderer.enabled = sprite != null;
            }
        }
    }
}
