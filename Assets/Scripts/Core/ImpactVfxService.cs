using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 轻量程序化撞击特效：池化粒子 + 扩散圆环，不依赖外部美术资源。
    /// 由 FeedbackDirector 在消费碰撞事件时调用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ImpactVfxService : MonoBehaviour
    {
        private sealed class Particle
        {
            public GameObject go;
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public float size;
            public Color color;
        }

        private sealed class Ring
        {
            public GameObject go;
            public Transform transform;
            public LineRenderer line;
            public float life;
            public float maxLife;
            public float maxRadius;
            public Color color;
        }

        public GameBootstrap bootstrap;

        private readonly List<Particle> activeParticles = new List<Particle>();
        private readonly Stack<Particle> particlePool = new Stack<Particle>();
        private readonly List<Ring> activeRings = new List<Ring>();
        private readonly Stack<Ring> ringPool = new Stack<Ring>();

        private Transform root;
        private Material ringMaterial;
        private System.Random rng;
        private bool initialized;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            if (initialized)
                return;

            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            root = new GameObject("VfxRoot").transform;
            root.SetParent(transform, false);
            ringMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "ImpactRingMaterial"
            };
            rng = new System.Random(20261102);
            initialized = true;
        }

        public void PlayBounce(Vector2 position, PlanetTypeId type, float speed)
        {
            Color color = GetColor(type);
            SpawnParticles(position, color, 8, 0.6f + speed * 0.15f,
                0.005f, 0.014f, 0.22f, 0.42f);
            SpawnRing(position, color, 0.20f + speed * 0.06f, 0.28f);
        }

        public void PlayCloud(Vector2 position, PlanetTypeId type)
        {
            SpawnParticles(position, new Color(0.85f, 0.93f, 1f, 1f), 8, 0.9f,
                0.01f, 0.025f, 0.20f, 0.35f);
            SpawnRing(position, new Color(0.8f, 0.92f, 1f, 1f), 0.35f, 0.25f);
        }

        public void PlayIce(Vector2 position, PlanetTypeId type)
        {
            Color color = GetColor(type);
            SpawnParticles(position, color, 14, 1.4f, 0.006f, 0.018f, 0.25f, 0.5f);
            SpawnRing(position, color, 0.45f, 0.3f);
        }

        public void PlayBomb(Vector2 position, PlanetTypeId type)
        {
            Color color = GetColor(type);
            SpawnParticles(position, new Color(1f, 0.55f, 0.2f, 1f), 26, 2.8f,
                0.008f, 0.025f, 0.3f, 0.65f);
            SpawnParticles(position, new Color(1f, 0.95f, 0.6f, 1f), 16, 1.8f,
                0.006f, 0.018f, 0.25f, 0.5f);
            SpawnRing(position, color, 1.1f, 0.45f);
            SpawnRing(position, new Color(1f, 0.92f, 0.65f, 1f), 1.7f, 0.55f);
        }

        public void PlayDeath(Vector2 position)
        {
            SpawnParticles(position, new Color(0.65f, 0.3f, 0.9f, 1f), 22, 1.4f,
                0.008f, 0.02f, 0.35f, 0.65f);
            SpawnRing(position, new Color(0.7f, 0.35f, 1f, 1f), 1.0f, 0.5f);
        }

        private void SpawnParticles(Vector2 position, Color color, int count,
            float speed, float minSize, float maxSize, float minLife, float maxLife)
        {
            if (!initialized)
                return;

            for (int i = 0; i < count; i++)
            {
                Particle particle = GetParticle();
                if (particle == null)
                    return;

                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float magnitude = speed * (0.4f + (float)rng.NextDouble() * 0.9f);
                particle.life = minLife + (float)rng.NextDouble() * (maxLife - minLife);
                particle.maxLife = particle.life;
                particle.size = minSize + (float)rng.NextDouble() * (maxSize - minSize);
                particle.color = color;
                particle.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * magnitude;
                particle.transform.position = new Vector3(position.x, position.y, 0f);
                particle.go.SetActive(true);
                particle.renderer.color = color;
                particle.transform.localScale = new Vector3(particle.size, particle.size, 1f);
                activeParticles.Add(particle);
            }
        }

        private void SpawnRing(Vector2 position, Color color, float maxRadius, float life)
        {
            if (!initialized)
                return;

            Ring ring = GetRing();
            if (ring == null)
                return;

            ring.life = life;
            ring.maxLife = life;
            ring.maxRadius = maxRadius;
            ring.color = color;
            ring.transform.position = new Vector3(position.x, position.y, 0f);
            ring.transform.localScale = Vector3.one * 0.05f;
            ring.line.startColor = color;
            ring.line.endColor = color;
            ring.go.SetActive(true);
            activeRings.Add(ring);
        }

        private Particle GetParticle()
        {
            if (particlePool.Count > 0)
                return particlePool.Pop();

            GameObject go = new GameObject("ImpactParticle");
            go.transform.SetParent(root, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralSprites.Circle;
            renderer.sortingOrder = 40;
            go.SetActive(false);
            return new Particle
            {
                go = go,
                transform = go.transform,
                renderer = renderer
            };
        }

        private Ring GetRing()
        {
            if (ringPool.Count > 0)
                return ringPool.Pop();

            GameObject go = new GameObject("ImpactRing");
            go.transform.SetParent(root, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.sortingOrder = 35;
            line.startWidth = 0.012f;
            line.endWidth = 0.012f;
            line.material = ringMaterial;

            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }

            go.SetActive(false);
            return new Ring
            {
                go = go,
                transform = go.transform,
                line = line
            };
        }

        private Color GetColor(PlanetTypeId type)
        {
            PlanetTypeDef def = bootstrap != null ? bootstrap.GetPlanetType(type) : null;
            return def != null ? def.color : Color.white;
        }

        private void Update()
        {
            if (!initialized)
                return;

            float dt = Time.unscaledDeltaTime;

            for (int i = activeParticles.Count - 1; i >= 0; i--)
            {
                Particle particle = activeParticles[i];
                particle.life -= dt;
                if (particle.life <= 0f)
                {
                    particle.go.SetActive(false);
                    particlePool.Push(particle);
                    activeParticles.RemoveAt(i);
                    continue;
                }

                particle.velocity *= 1f - Mathf.Min(0.9f, 2.2f * dt);
                particle.transform.position += (Vector3)(particle.velocity * dt);
                float lifeRatio = particle.life / particle.maxLife;
                Color color = particle.color;
                color.a *= lifeRatio;
                particle.renderer.color = color;
                float scale = particle.size * (0.45f + 0.55f * lifeRatio);
                particle.transform.localScale = new Vector3(scale, scale, 1f);
            }

            for (int i = activeRings.Count - 1; i >= 0; i--)
            {
                Ring ring = activeRings[i];
                ring.life -= dt;
                if (ring.life <= 0f)
                {
                    ring.go.SetActive(false);
                    ringPool.Push(ring);
                    activeRings.RemoveAt(i);
                    continue;
                }

                float t = 1f - ring.life / ring.maxLife;
                float radius = Mathf.Lerp(0.05f, ring.maxRadius, 1f - Mathf.Pow(1f - t, 3f));
                ring.transform.localScale = Vector3.one * radius;
                Color color = ring.color;
                color.a *= Mathf.Clamp01(ring.life / ring.maxLife);
                ring.line.startColor = color;
                ring.line.endColor = color;
            }
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
                Destroy(ringMaterial);
        }
    }
}
