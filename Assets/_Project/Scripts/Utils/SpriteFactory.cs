using System;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Procedurally generated sprites so the game needs no image assets.
    /// All sprites are white; tint via SpriteRenderer/Image color.
    /// </summary>
    public static class SpriteFactory
    {
        const int Size = 64;

        static Sprite square, roundedSquare, circle, star, uiRounded;

        public static Sprite Square
        {
            get
            {
                if (square == null) square = Generate("Square", (x, y) => 1f, Vector4.zero);
                return square;
            }
        }

        public static Sprite RoundedSquare
        {
            get
            {
                if (roundedSquare == null)
                    roundedSquare = Generate("RoundedSquare", RoundedBoxAlpha(14f), Vector4.zero);
                return roundedSquare;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    float r = Size * 0.5f - 1.5f;
                    circle = Generate("Circle", (x, y) =>
                    {
                        float d = Mathf.Sqrt(x * x + y * y);
                        return Mathf.Clamp01(r - d + 0.5f);
                    }, Vector4.zero);
                }
                return circle;
            }
        }

        /// <summary>Rounded rect with a 9-slice border, for UI panels and buttons (Image.type = Sliced).</summary>
        public static Sprite UIRounded
        {
            get
            {
                if (uiRounded == null)
                    uiRounded = Generate("UIRounded", RoundedBoxAlpha(20f), new Vector4(24, 24, 24, 24));
                return uiRounded;
            }
        }

        public static Sprite Star
        {
            get
            {
                if (star == null)
                {
                    var pts = new Vector2[10];
                    float outer = Size * 0.5f - 2f, inner = outer * 0.5f;
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rad = (i % 2 == 0) ? outer : inner;
                        pts[i] = new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
                    }
                    star = Generate("Star", (x, y) =>
                    {
                        // 2x2 supersampling of a point-in-polygon test
                        float a = 0f;
                        for (int sx = 0; sx < 2; sx++)
                            for (int sy = 0; sy < 2; sy++)
                                if (InPolygon(pts, new Vector2(x + sx * 0.5f - 0.25f, y + sy * 0.5f - 0.25f)))
                                    a += 0.25f;
                        return a;
                    }, Vector4.zero);
                }
                return star;
            }
        }

        static Func<float, float, float> RoundedBoxAlpha(float radius)
        {
            float half = Size * 0.5f - 1f;
            return (x, y) =>
            {
                float qx = Mathf.Max(Mathf.Abs(x) - (half - radius), 0f);
                float qy = Mathf.Max(Mathf.Abs(y) - (half - radius), 0f);
                float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                return Mathf.Clamp01(0.5f - d);
            };
        }

        static bool InPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>alpha(x, y) receives pixel coords relative to the texture center.</summary>
        static Sprite Generate(string name, Func<float, float, float> alpha, Vector4 border)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
#if !UNITY_LUNA
            // Luna's bridged Object.name is getter-only; the name is cosmetic anyway.
            tex.name = name;
#endif
            var pixels = new Color[Size * Size];
            float half = Size * 0.5f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha(x - half + 0.5f, y - half + 0.5f));
            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f),
                Size, 0, SpriteMeshType.FullRect, border);
#if !UNITY_LUNA
            sprite.name = name;
#endif
            return sprite;
        }
    }
}
