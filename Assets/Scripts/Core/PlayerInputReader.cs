using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 旧版 Input Manager 的薄封装。Stage 1 只支持键盘左右；
    /// 后续可在这里接手柄/触屏而不影响 PlayerController。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public float Horizontal { get; private set; }
        public bool RespawnPressed { get; private set; }
        public bool ToggleHoldPressed { get; private set; }

        private GameConfig config;

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        private void Awake()
        {
            if (config == null && GameBootstrap.Instance != null)
                config = GameBootstrap.Instance.gameConfig;
        }

        private void Update()
        {
            if (config == null)
                return;

            float horizontal = 0f;
            if (IsHeld(config.moveLeftKey) || IsHeld(config.moveLeftAltKey))
                horizontal -= 1f;
            if (IsHeld(config.moveRightKey) || IsHeld(config.moveRightAltKey))
                horizontal += 1f;

            Horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            RespawnPressed = WasPressed(config.respawnKey);
            ToggleHoldPressed = WasPressed(config.holdRadiusKey);
        }

        private static bool IsHeld(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKey(key);
        }

        private static bool WasPressed(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }
    }
}
