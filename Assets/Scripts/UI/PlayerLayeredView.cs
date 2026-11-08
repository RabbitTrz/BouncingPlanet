using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// uGUI 玩家分层视图：按 paper/body/face/hair/cloth/decoration/hand 叠 7 张图。
    /// 用于选关玩家贴纸和换装页预览；状态名支持 happy/down/up/overdrive。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLayeredView : MonoBehaviour
    {
        public string initialState = "happy";

        private Image[] layers;
        private Wardrobe wardrobe;
        private string stateName;
        private bool initialized;

        private void Awake()
        {
            BuildLayers();
            if (string.IsNullOrEmpty(stateName))
                stateName = initialState;
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (wardrobe != null)
                wardrobe.Changed -= Apply;
        }

        private void Update()
        {
            if (!initialized)
                EnsureInitialized();
        }

        public void SetState(string state)
        {
            stateName = string.IsNullOrEmpty(state) ? "happy" : state;
            if (initialized)
                Apply();
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;
            GameBootstrap bootstrap = GameBootstrap.Instance;
            wardrobe = bootstrap != null ? bootstrap.wardrobe : null;
            if (wardrobe == null)
                return;
            wardrobe.Changed += Apply;
            initialized = true;
            Apply();
        }

        private void BuildLayers()
        {
            if (layers != null)
                return;

            layers = new Image[Wardrobe.LayerCount];
            for (int i = 0; i < layers.Length; i++)
            {
                GameObject go = new GameObject(
                    "Layer_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(transform, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                layers[i] = image;
            }
        }

        private void Apply()
        {
            if (wardrobe == null || layers == null)
                return;

            Sprite[] sprites = wardrobe.GetLayers(stateName);
            for (int i = 0; i < layers.Length; i++)
            {
                Sprite sprite = i < sprites.Length ? sprites[i] : null;
                layers[i].sprite = sprite;
                layers[i].enabled = sprite != null;
            }
        }
    }
}
