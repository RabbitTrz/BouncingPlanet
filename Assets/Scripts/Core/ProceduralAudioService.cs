using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 程序化占位音效。后续可以直接替换成正式 AudioClip，
    /// 调用方只依赖 PlayBounce/PlayIce/... 这些语义接口。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralAudioService : MonoBehaviour
    {
        private const int SampleRate = 44100;

        public GameBootstrap bootstrap;

        private AudioSource[] voices;
        private int voiceCursor;
        private AudioClip bounceClip;
        private AudioClip iceClip;
        private AudioClip cloudClip;
        private AudioClip bombClip;
        private AudioClip deathClip;
        private GameConfig config;
        private bool initialized;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            config = bootstrap != null ? bootstrap.gameConfig : null;

            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0.7f;
                voices[i] = source;
            }

            bounceClip = MakeTone("Bounce", 240f, 140f, 0.16f, 0.55f);
            iceClip = MakeNoise("IceCrack", 0.13f, 0.4f);
            cloudClip = MakeNoise("CloudWhoosh", 0.28f, 0.25f);
            bombClip = MakeTone("Bomb", 110f, 38f, 0.55f, 0.9f);
            deathClip = MakeTone("Death", 300f, 55f, 0.65f, 0.6f);

            initialized = true;
        }

        public void PlayBounce(float speed, PlanetTypeId type)
        {
            if (!initialized)
                return;
            float pitch = Mathf.Clamp(0.75f + speed * 0.08f, 0.65f, 1.8f);
            if (type == PlanetTypeId.Shiny)
                pitch *= 1.12f;
            PlayClip(bounceClip, 0.35f, pitch);
        }

        public void PlayIce()
        {
            PlayClip(iceClip, 0.35f, 1.1f);
        }

        public void PlayCloud()
        {
            PlayClip(cloudClip, 0.28f, 1f);
        }

        public void PlayBomb()
        {
            PlayClip(bombClip, 0.6f, 1f);
        }

        public void PlayDeath()
        {
            PlayClip(deathClip, 0.5f, 1f);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (!initialized || clip == null || voices == null || voices.Length == 0)
                return;

            float master = config != null ? config.audioMasterVolume : 0.65f;
            AudioSource source = voices[voiceCursor];
            voiceCursor = (voiceCursor + 1) % voices.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(volume * master));
        }

        private static AudioClip MakeTone(string name, float startFrequency, float endFrequency,
            float duration, float volume)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] data = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency / SampleRate * Mathf.PI * 2f;
                float envelope = Mathf.Exp(-t * 5f) * Mathf.Clamp01(t * 80f);
                data[i] = Mathf.Sin(phase) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeNoise(string name, float duration, float volume)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] data = new float[sampleCount];
            System.Random rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float envelope = Mathf.Exp(-t * 4f) * Mathf.Clamp01(t * 60f);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (bounceClip != null) Destroy(bounceClip);
            if (iceClip != null) Destroy(iceClip);
            if (cloudClip != null) Destroy(cloudClip);
            if (bombClip != null) Destroy(bombClip);
            if (deathClip != null) Destroy(deathClip);
        }
    }
}
