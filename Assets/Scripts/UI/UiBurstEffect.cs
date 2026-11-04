using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BouncingPlanet
{
    /// <summary>
    /// 纯 uGUI 的小粒子爆点：在技能图标中心生成黄色圆点，向外飞散并淡出。
    /// 不依赖 ParticleSystem，因此可以直接盖在 Screen Space Overlay 的 HUD 上。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiBurstEffect : MonoBehaviour
    {
        public int particleCount = 12;
        public Color color = new Color(1f, 0.85f, 0.25f, 1f);
        public float duration = 0.38f;
        public float speed = 180f;
        public float minSize = 5f;
        public float maxSize = 10f;
        public float gravity = 120f;

        private sealed class Particle
        {
            public RectTransform rect;
            public Image image;
            public Vector2 velocity;
            public float life;
            public float maxLife;
        }

        private readonly List<Particle> particles = new List<Particle>();
        private Sprite particleSprite;

        private void Awake()
        {
            particleSprite = ProceduralSprites.CreateCircleSprite(32);
        }

        public void Burst()
        {
            if (particleSprite == null)
                particleSprite = ProceduralSprites.CreateCircleSprite(32);

            for (int i = 0; i < particleCount; i++)
            {
                float size = Random.Range(minSize, maxSize);
                GameObject go = new GameObject(
                    "ReadyParticle",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(transform, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = Random.insideUnitCircle * 6f;

                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.001f)
                    direction = Vector2.up;
                direction.Normalize();

                Image image = go.GetComponent<Image>();
                image.sprite = particleSprite;
                image.color = color;
                image.preserveAspect = true;
                image.raycastTarget = false;

                particles.Add(new Particle
                {
                    rect = rect,
                    image = image,
                    velocity = direction * speed * Random.Range(0.55f, 1.15f) +
                               Vector2.up * 40f,
                    life = 0f,
                    maxLife = duration * Random.Range(0.8f, 1.2f)
                });
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.life += dt;
                float t = Mathf.Clamp01(
                    particle.life / Mathf.Max(0.0001f, particle.maxLife));

                particle.velocity += Vector2.down * gravity * dt;
                particle.rect.anchoredPosition += particle.velocity * dt;
                float scale = Mathf.Lerp(1f, 0.15f, t);
                particle.rect.localScale = new Vector3(scale, scale, 1f);

                Color tint = color;
                tint.a *= 1f - t;
                particle.image.color = tint;

                if (t >= 1f)
                {
                    Destroy(particle.rect.gameObject);
                    particles.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            if (particleSprite == null)
                return;
            Texture2D texture = particleSprite.texture;
            Destroy(particleSprite);
            if (texture != null)
                Destroy(texture);
        }
    }
}
