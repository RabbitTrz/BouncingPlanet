using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>设置弹窗里的音量图标 + 滑块，绑定 FeedbackConfig。</summary>
    [DisallowMultipleComponent]
    public sealed class VolumeSettingView : MonoBehaviour
    {
        private const string VolumePrefKey = "BouncingPlanet.SfxVolume";

        public Slider volumeSlider;
        public Image volumeIcon;
        public Button muteButton;
        public Sprite volumeNo;
        public Sprite volumeLow;
        public Sprite volumeMid;
        public Sprite volumeHigh;

        private FeedbackConfig config;
        private float lastNonZero = 0.65f;

        private void Awake()
        {
            bootstrapConfig();
            float value = config != null ? config.sfxMasterVolume : 0.65f;
            if (PlayerPrefs.HasKey(VolumePrefKey))
                value = PlayerPrefs.GetFloat(VolumePrefKey, value);

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            if (muteButton != null)
                muteButton.onClick.AddListener(ToggleMute);

            ApplyVolume(value);
        }

        private void bootstrapConfig()
        {
            GameBootstrap bootstrap = GameBootstrap.Instance;
            config = bootstrap != null ? bootstrap.feedbackConfig : null;
        }

        private void OnDestroy()
        {
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            if (muteButton != null)
                muteButton.onClick.RemoveListener(ToggleMute);
        }

        private void OnVolumeChanged(float value)
        {
            ApplyVolume(value);
        }

        private void ToggleMute()
        {
            if (volumeSlider == null)
                return;
            if (volumeSlider.value > 0.01f)
                ApplyVolume(0f);
            else
                ApplyVolume(Mathf.Max(0.1f, lastNonZero));
        }

        private void ApplyVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (value > 0.01f)
                lastNonZero = value;

            if (config != null)
                config.sfxMasterVolume = value;
            PlayerPrefs.SetFloat(VolumePrefKey, value);
            PlayerPrefs.Save();

            if (volumeSlider != null &&
                Mathf.Abs(volumeSlider.value - value) > 0.001f)
            {
                volumeSlider.SetValueWithoutNotify(value);
            }

            if (volumeIcon != null)
            {
                volumeIcon.sprite = value <= 0.01f
                    ? volumeNo
                    : value < 0.35f
                        ? volumeLow
                        : value < 0.7f
                            ? volumeMid
                            : volumeHigh;
            }
        }
    }
}
