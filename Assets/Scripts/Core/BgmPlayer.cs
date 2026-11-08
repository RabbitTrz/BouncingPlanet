using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 主界面 / 选关 / 关卡 BGM 切换。两个 AudioSource 交叉淡入淡出；
    /// 目标音乐与当前一致时不会重新播放。关卡音乐优先取 LevelConfig.bgm。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BgmPlayer : MonoBehaviour
    {
        public enum Area
        {
            StartPage,
            LevelSelect,
            Game
        }

        private const string StartPageResource = "Audio/BGM_StartPage";
        private const string LevelSelectResource = "Audio/BGM_LevelSelect";
        private const string GameResource = "Audio/BGM_Game";

        private GameBootstrap bootstrap;
        private GameConfig config;
        private FeedbackConfig feedback;
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource fadingOutSource;
        private AudioClip currentClip;
        private Area currentArea = (Area)(-1);
        private AudioClip fallbackStartPage;
        private AudioClip fallbackLevelSelect;
        private AudioClip fallbackGame;
        private float fadeT = 1f;

        public void Initialize(GameBootstrap owner)
        {
            bootstrap = owner != null ? owner : GameBootstrap.Instance;
            ResolveConfigs();
            EnsureSources();

            fallbackStartPage = Resources.Load<AudioClip>(StartPageResource);
            fallbackLevelSelect = Resources.Load<AudioClip>(LevelSelectResource);
            fallbackGame = Resources.Load<AudioClip>(GameResource);

            if (config != null &&
                config.bgmStartPage == null &&
                config.bgmLevelSelect == null &&
                config.bgmGame == null &&
                fallbackStartPage == null &&
                fallbackLevelSelect == null &&
                fallbackGame == null)
            {
                Debug.Log(
                    "[BgmPlayer] 未找到 BGM 资源。可将文件放到 " +
                    "Resources/Audio/BGM_StartPage、BGM_LevelSelect、BGM_Game，" +
                    "或在 GameConfig / LevelConfig 中指定 AudioClip。");
            }
        }

        private void Update()
        {
            ResolveConfigs();
            float volume = ComputeVolume();
            UpdateFade(volume);

            Area area = ResolveArea();
            AudioClip desired = ResolveClip(area);
            if (area == currentArea && desired == currentClip)
                return;

            currentArea = area;
            if (desired == currentClip)
                return;

            SwitchTo(desired);
        }

        private void ResolveConfigs()
        {
            if (bootstrap == null)
                bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
                return;
            config = bootstrap.gameConfig;
            feedback = bootstrap.feedbackConfig;
        }

        private void EnsureSources()
        {
            if (sourceA == null)
                sourceA = CreateSource("BgmSourceA");
            if (sourceB == null)
                sourceB = CreateSource("BgmSourceB");
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject go = new GameObject(sourceName, typeof(AudioSource));
            go.transform.SetParent(transform, false);
            AudioSource source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        private float ComputeVolume()
        {
            float volume = config != null ? config.bgmVolume : 0.5f;
            if (feedback != null)
                volume *= feedback.sfxMasterVolume;
            return Mathf.Clamp01(volume);
        }

        private Area ResolveArea()
        {
            GameFlow flow = bootstrap != null ? bootstrap.gameFlow : null;
            if (flow != null && flow.State != GameFlow.FlowState.Start)
                return Area.Game;

            StartPageController startPage = StartPageController.Instance;
            if (startPage != null &&
                startPage.levelSelectPage != null &&
                startPage.levelSelectPage.activeInHierarchy)
            {
                return Area.LevelSelect;
            }

            return Area.StartPage;
        }

        private AudioClip ResolveClip(Area area)
        {
            switch (area)
            {
                case Area.LevelSelect:
                    return config != null && config.bgmLevelSelect != null
                        ? config.bgmLevelSelect
                        : fallbackLevelSelect;

                case Area.Game:
                    LevelConfig level = bootstrap != null &&
                                        bootstrap.gameFlow != null
                        ? bootstrap.gameFlow.CurrentLevel
                        : null;
                    if (level != null && level.bgm != null)
                        return level.bgm;
                    return config != null && config.bgmGame != null
                        ? config.bgmGame
                        : fallbackGame;

                default:
                    return config != null && config.bgmStartPage != null
                        ? config.bgmStartPage
                        : fallbackStartPage;
            }
        }

        private void SwitchTo(AudioClip clip)
        {
            AudioSource outgoing = activeSource;
            AudioSource incoming = outgoing == sourceA ? sourceB : sourceA;

            if (outgoing == null && fadingOutSource != null)
            {
                outgoing = fadingOutSource;
                incoming = outgoing == sourceA ? sourceB : sourceA;
            }

            // 快速连续切换时，旧的在淡出源直接停掉，复用它的 AudioSource。
            if (fadingOutSource != null &&
                fadingOutSource != outgoing &&
                fadingOutSource != incoming)
            {
                fadingOutSource.Stop();
            }

            if (clip == null)
            {
                fadingOutSource = outgoing;
                activeSource = null;
                currentClip = null;
                fadeT = 0f;
                return;
            }

            incoming.Stop();
            incoming.clip = clip;
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();

            fadingOutSource = outgoing != null && outgoing.isPlaying
                ? outgoing
                : null;
            activeSource = incoming;
            currentClip = clip;
            fadeT = 0f;
        }

        private void UpdateFade(float targetVolume)
        {
            float fadeDuration = config != null
                ? Mathf.Max(0.05f, config.bgmFadeDuration)
                : 0.8f;

            if (fadeT < 1f)
            {
                fadeT = Mathf.Min(
                    1f, fadeT + Time.unscaledDeltaTime / fadeDuration);
                float ease = Mathf.SmoothStep(0f, 1f, fadeT);
                if (activeSource != null)
                    activeSource.volume = targetVolume * ease;
                if (fadingOutSource != null)
                    fadingOutSource.volume = targetVolume * (1f - ease);

                if (fadeT >= 1f)
                {
                    if (fadingOutSource != null)
                    {
                        fadingOutSource.Stop();
                        fadingOutSource.volume = 0f;
                        fadingOutSource = null;
                    }
                    if (activeSource != null)
                        activeSource.volume = targetVolume;
                }
            }
            else if (activeSource != null)
            {
                activeSource.volume = targetVolume;
            }
        }
    }
}
