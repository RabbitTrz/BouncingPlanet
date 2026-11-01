using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Stage 0 占位图。生成 1x1 世界单位的圆形 Sprite，之后美术可直接替换。
    /// </summary>
    public static class ProceduralSprites
    {
        private static Sprite circleSprite;

        public static Sprite Circle
        {
            get
            {
                if (circleSprite == null)
                    circleSprite = CreateCircleSprite(64);
                return circleSprite;
            }
        }

        public static Sprite CreateCircleSprite(int size)
        {
            size = Mathf.Clamp(size, 8, 512);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ProceduralCircle_" + size;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float normalized = distance / Mathf.Max(0.001f, radius);
                    float alpha = Mathf.Clamp01((1f - normalized) * radius * 0.9f);
                    if (alpha <= 0.001f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // 中心略亮、边缘略暗，SpriteRenderer.color 会整体染色。
                    float shade = Mathf.Lerp(1.0f, 0.62f, Mathf.Clamp01(normalized));
                    pixels[y * size + x] = new Color(shade, shade, shade, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            // PPU = size => Sprite 原始尺寸正好 1x1 世界单位，缩放即直径。
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "ProceduralCircle";
            return sprite;
        }
    }
}
