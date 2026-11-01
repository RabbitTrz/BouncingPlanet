using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 固定镜头：玩家始终在屏幕中心，径向朝上，1 世界单位 = cameraPixelsPerUnit 参考像素。
    /// 不做缩放、不做平滑；震屏/punch 留给后续反馈层。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour
    {
        public GameBootstrap bootstrap;
        public Camera targetCamera;

        public Camera ActiveCamera => targetCamera;

        private FeedbackDirector feedback;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap;
            feedback = bootstrap != null ? bootstrap.feedbackDirector : null;
            EnsureCamera();
            ApplyStaticSettings();
        }

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
            feedback = bootstrap != null ? bootstrap.feedbackDirector : null;
            EnsureCamera();
        }

        private void LateUpdate()
        {
            if (bootstrap == null || targetCamera == null || bootstrap.player == null)
                return;

            GameConfig config = bootstrap.gameConfig;
            if (config == null)
                return;

            PlayerState state = bootstrap.player.State;
            Vector2 playerPosition = state.WorldPosition;

            targetCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10f);
            targetCamera.transform.rotation = Quaternion.Euler(
                0f, 0f, state.WorldAngle * Mathf.Rad2Deg - 90f);

            targetCamera.orthographicSize = Mathf.Max(0.01f, config.cameraOrthographicSize);
            targetCamera.backgroundColor = config.backgroundColor;

            if (feedback != null)
            {
                targetCamera.orthographicSize *= 1f + feedback.Punch * 0.03f;

                float shakeAmplitude = feedback.Shake * 9f /
                    Mathf.Max(1f, config.cameraPixelsPerUnit);
                if (shakeAmplitude > 0f)
                {
                    Vector2 offset = Random.insideUnitCircle * shakeAmplitude;
                    targetCamera.transform.position +=
                        targetCamera.transform.right * offset.x +
                        targetCamera.transform.up * offset.y;
                }
            }
        }

        private void EnsureCamera()
        {
            if (targetCamera != null)
                return;

            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                targetCamera = cameraObject.AddComponent<Camera>();
                if (FindObjectOfType<AudioListener>() == null)
                    cameraObject.AddComponent<AudioListener>();
            }

            ApplyStaticSettings();
        }

        private void ApplyStaticSettings()
        {
            if (targetCamera == null)
                return;

            targetCamera.orthographic = true;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.nearClipPlane = 0.1f;
            targetCamera.farClipPlane = 100f;
        }
    }
}
