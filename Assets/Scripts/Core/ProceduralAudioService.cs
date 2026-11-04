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
        private AudioClip hookClip;
        private AudioClip deathClip;
        private AudioClip readyClip;
        private FeedbackConfig feedbackConfig;
        private bool initialized;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            feedbackConfig = bootstrap != null ? bootstrap.feedbackConfig : null;

            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0.7f;
                voices[i] = source;
            }

            bounceClip = MakeBounceClip();
            iceClip = MakeNoise("IceCrack", 0.13f, 0.4f);
            cloudClip = MakeNoise("CloudWhoosh", 0.28f, 0.25f);
            bombClip = MakeBombClip();
            hookClip = MakeTone("HookHeavy", 110f, 38f, 0.55f, 0.9f);
            deathClip = MakeTone("Death", 300f, 55f, 0.65f, 0.6f);
            readyClip = MakeReadyClip();

            initialized = true;
        }

        public void PlayBounce(float speed, PlanetTypeId type)
        {
            if (!initialized)
                return;
            float pitch = Mathf.Clamp(0.75f + speed * 0.08f, 0.65f, 1.8f);
            if (type == PlanetTypeId.Shiny)
                pitch *= 1.12f;
            PlayClip(bounceClip,
                feedbackConfig != null ? feedbackConfig.sfxBounceVolume : 0.35f, pitch);
        }

        public void PlayIce()
        {
            PlayClip(iceClip,
                feedbackConfig != null ? feedbackConfig.sfxIceVolume : 0.35f, 1.1f);
        }

        public void PlayCloud()
        {
            PlayClip(cloudClip,
                feedbackConfig != null ? feedbackConfig.sfxCloudVolume : 0.28f, 1f);
        }

        public void PlayBomb()
        {
            PlayClip(bombClip,
                feedbackConfig != null ? feedbackConfig.sfxBombVolume : 0.60f, 1f);
        }

        public void PlayDeath()
        {
            PlayClip(deathClip,
                feedbackConfig != null ? feedbackConfig.sfxDeathVolume : 0.50f, 1f);
        }

        public void PlayHookHeavy()
        {
            PlayClip(hookClip,
                feedbackConfig != null ? feedbackConfig.sfxHookVolume : 0.28f, 1.2f);
        }

        public void PlayCooldownReady()
        {
            PlayClip(readyClip,
                feedbackConfig != null ? feedbackConfig.sfxReadyVolume : 0.40f, 1f);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (!initialized || clip == null || voices == null || voices.Length == 0)
                return;

            float master = feedbackConfig != null ? feedbackConfig.sfxMasterVolume : 0.65f;
            AudioSource source = voices[voiceCursor];
            voiceCursor = (voiceCursor + 1) % voices.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(volume * master));
        }

        /// <summary>
        /// 弹跳音：加入二次谐波和短噪声瞬态，让短促的“啵”在小音箱上也听得清。
        /// </summary>
        private static AudioClip MakeBounceClip()
        {
            const float duration = 0.18f;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] data = new float[sampleCount];
            float phase = 0f;
            System.Random rng = new System.Random(20261102);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float frequency = Mathf.Lerp(320f, 170f, t);
                phase += frequency / SampleRate * Mathf.PI * 2f;
                float body = Mathf.Sin(phase) * 0.75f;
                float harmonic = Mathf.Sin(phase * 2f) * 0.30f;
                float noise = ((float)rng.NextDouble() * 2f - 1f) *
                              Mathf.Exp(-t * 26f) * 0.35f;
                float envelope = Mathf.Exp(-t * 6f) * Mathf.Clamp01(t * 120f);
                data[i] = Mathf.Clamp((body + harmonic + noise) * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Bounce", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 网页 demo 的爆炸音是 Web Audio 合成，不是音频文件：
        /// noise(0.6s, 0.5, 90Hz highpass) + sine(95→36Hz, 0.55s, 0.38)。
        /// 这里按同样的参数在 Unity 里生成 PCM。
        /// </summary>
        private static AudioClip MakeBombClip()
        {
            const float noiseDuration = 0.6f;
            const float toneDuration = 0.55f;
            const float noiseVolume = 0.5f;
            const float toneVolume = 0.38f;
            const float attackTime = 0.01f;

            float duration = Mathf.Max(noiseDuration, toneDuration);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            int noiseSamples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * noiseDuration));
            float[] data = new float[sampleCount];
            System.Random rng = new System.Random(20261102 + 7);

            // Web Audio BiquadFilter: highpass 90Hz, Q=1。
            float w0 = 2f * Mathf.PI * 90f / SampleRate;
            float cosW0 = Mathf.Cos(w0);
            float alpha = Mathf.Sin(w0) * 0.5f;
            float b0 = (1f + cosW0) * 0.5f;
            float b1 = -(1f + cosW0);
            float b2 = b0;
            float a0 = 1f + alpha;
            float a1 = -2f * cosW0;
            float a2 = 1f - alpha;
            float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;

                // 95Hz → 36Hz 指数扫频 + 指数 attack/decay。
                float t01 = Mathf.Clamp01(time / toneDuration);
                float frequency = 95f * Mathf.Pow(36f / 95f, t01);
                phase += frequency / SampleRate * Mathf.PI * 2f;

                float toneEnvelope;
                if (time < attackTime)
                {
                    toneEnvelope = 0.0001f * Mathf.Pow(
                        toneVolume / 0.0001f, time / attackTime);
                }
                else
                {
                    float d = Mathf.Clamp01(
                        (time - attackTime) / Mathf.Max(0.0001f, toneDuration - attackTime));
                    toneEnvelope = toneVolume * Mathf.Pow(0.0001f / toneVolume, d);
                }

                float sample = Mathf.Sin(phase) * toneEnvelope;

                if (i < noiseSamples)
                {
                    float white = (float)rng.NextDouble() * 2f - 1f;
                    float filtered = (b0 * white + b1 * x1 + b2 * x2 -
                                      a1 * y1 - a2 * y2) / a0;
                    x2 = x1;
                    x1 = white;
                    y2 = y1;
                    y1 = filtered;

                    float fade = 1f - i / (float)noiseSamples;
                    sample += filtered * fade * noiseVolume;
                }

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Bomb", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
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

        /// <summary>冷却完成：两声短促的上行提示音。</summary>
        private static AudioClip MakeReadyClip()
        {
            const float duration = 0.24f;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float sample = ReadyNote(t, 0f, 0.09f, 880f) * 0.22f;
                sample += ReadyNote(t, 0.09f, 0.13f, 1320f) * 0.18f;
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(
                "CooldownReady", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float ReadyNote(float t, float start, float duration,
            float frequency)
        {
            if (t < start || t >= start + duration)
                return 0f;
            float local = (t - start) / duration;
            float envelope = Mathf.Sin(local * Mathf.PI);
            return Mathf.Sin((t - start) * frequency * Mathf.PI * 2f) * envelope;
        }

        private void OnDestroy()
        {
            if (bounceClip != null) Destroy(bounceClip);
            if (iceClip != null) Destroy(iceClip);
            if (cloudClip != null) Destroy(cloudClip);
            if (bombClip != null) Destroy(bombClip);
            if (hookClip != null) Destroy(hookClip);
            if (deathClip != null) Destroy(deathClip);
            if (readyClip != null) Destroy(readyClip);
        }
    }
}
