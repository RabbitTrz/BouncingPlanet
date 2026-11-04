using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 循环星空背景：相机下的一个屏幕四边形，UV 平铺并随玩家移动反向滚动。
    /// 只做表现，不参与仿真/碰撞；支持带线/无线两版贴图即时切换。
    /// </summary>
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public sealed class TiledBackgroundView : MonoBehaviour
    {
        private const string LineSpritePath = "Image/Background/backgroud_line";
        private const string NoLineSpritePath = "Image/Background/backgroud_noline";
        private const float ScreenMargin = 1.15f;

        private GameConfig config;
        private PlayerController player;
        private Camera targetCamera;
        private Sprite lineSprite;
        private Sprite noLineSprite;
        private Material material;
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private readonly Vector2[] uvs = new Vector2[4];
        private Vector2 uvOffset;
        private Vector2 lastPlayerPosition;
        private bool hasLastPosition;
        private bool usingLineVariant;

        /// <summary>成功创建返回 true；贴图缺失时返回 false，让调用方保留程序化星空。</summary>
        public bool Initialize(GameBootstrap bootstrap)
        {
            if (bootstrap == null)
                return false;

            config = bootstrap.gameConfig;
            player = bootstrap.player;
            targetCamera = bootstrap.cameraRig != null
                ? bootstrap.cameraRig.ActiveCamera
                : Camera.main;
            if (config == null || player == null || targetCamera == null)
                return false;

            lineSprite = Resources.Load<Sprite>(LineSpritePath);
            noLineSprite = Resources.Load<Sprite>(NoLineSpritePath);
            if (lineSprite == null && noLineSprite == null)
            {
                Debug.LogWarning(
                    "[TiledBackgroundView] 未找到背景图，保留程序化星空。");
                return false;
            }

            transform.SetParent(targetCamera.transform, false);
            transform.localPosition = new Vector3(0f, 0f, 20f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            BuildQuad();
            ApplyVariant(config.backgroundUseLineVariant);
            ApplyScreenSize();

            lastPlayerPosition = player.State.WorldPosition;
            hasLastPosition = true;
            player.Respawned += OnPlayerRespawned;
            return true;
        }

        private void OnDestroy()
        {
            if (player != null)
                player.Respawned -= OnPlayerRespawned;
            if (material != null)
                Destroy(material);
            if (mesh != null)
                Destroy(mesh);
        }

        private void OnPlayerRespawned()
        {
            if (player == null)
                return;
            lastPlayerPosition = player.State.WorldPosition;
            hasLastPosition = true;
        }

        private void BuildQuad()
        {
            mesh = new Mesh { name = "TiledBackgroundQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            // 正面朝 -Z，正好面向相机；顶点色给 Sprites/Default 用。
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 0.1f));

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sortingOrder = -200;
            meshRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Sprites/Default 在 2D 工程里必然会被打包；Unlit/Transparent 作为后备。
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            material = shader != null ? new Material(shader) : null;
            if (material != null)
                material.name = "TiledBackgroundMaterial";
            meshRenderer.sharedMaterial = material;
        }

        private void ApplyVariant(bool useLine)
        {
            usingLineVariant = useLine;
            Sprite sprite = useLine ? lineSprite : noLineSprite;
            if (sprite == null)
                sprite = useLine ? noLineSprite : lineSprite;
            if (sprite == null || material == null)
                return;

            Texture texture = sprite.texture;
            if (texture != null)
                texture.wrapMode = TextureWrapMode.Repeat;
            material.mainTexture = texture;
            material.color = Color.white;
        }

        private void ApplyScreenSize()
        {
            if (targetCamera == null)
                return;
            float height = Mathf.Max(0.01f, targetCamera.orthographicSize * 2f) *
                           ScreenMargin;
            float width = height * Mathf.Max(0.1f, targetCamera.aspect);
            transform.localScale = new Vector3(width, height, 1f);
        }

        private void LateUpdate()
        {
            if (config == null || player == null || targetCamera == null)
                return;

            if (Input.GetKeyDown(KeyCode.B))
                config.backgroundUseLineVariant = !config.backgroundUseLineVariant;

            bool active = config.backgroundEnabled;
            if (meshRenderer != null)
                meshRenderer.enabled = active;
            if (!active)
                return;

            if (config.backgroundUseLineVariant != usingLineVariant)
                ApplyVariant(config.backgroundUseLineVariant);
            ApplyScreenSize();

            Vector2 current = player.State.WorldPosition;
            if (!hasLastPosition)
            {
                lastPlayerPosition = current;
                hasLastPosition = true;
                return;
            }

            Vector2 worldDelta = current - lastPlayerPosition;
            lastPlayerPosition = current;
            Vector3 localDelta = targetCamera.transform.InverseTransformVector(
                new Vector3(worldDelta.x, worldDelta.y, 0f));

            float tileSize = Mathf.Max(0.01f, config.backgroundTileSize);
            uvOffset += new Vector2(localDelta.x, localDelta.y) *
                        Mathf.Max(0f, config.backgroundParallax) / tileSize;
            uvOffset.x = Mathf.Repeat(uvOffset.x, 1f);
            uvOffset.y = Mathf.Repeat(uvOffset.y, 1f);

            float repeatX = transform.localScale.x / tileSize;
            float repeatY = transform.localScale.y / tileSize;
            uvs[0] = new Vector2(uvOffset.x, uvOffset.y);
            uvs[1] = new Vector2(repeatX + uvOffset.x, uvOffset.y);
            uvs[2] = new Vector2(repeatX + uvOffset.x, repeatY + uvOffset.y);
            uvs[3] = new Vector2(uvOffset.x, repeatY + uvOffset.y);
            mesh.SetUVs(0, uvs);
        }
    }
}
