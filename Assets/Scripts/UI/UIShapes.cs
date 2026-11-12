using System.Collections.Generic;
using UnityEngine;

// Generates clean, anti-aliased sprites in code (rounded panels, circles, shape outlines, gradients),
// so the HUD looks crisp without needing any image files.
public static class UIShapes
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static readonly Color Panel = Hex("1C1F2BE6");
    public static readonly Color PanelBorder = Hex("343A52");
    public static readonly Color TextLight = Hex("F4F1EA");
    public static readonly Color TextMuted = Hex("A9AEC2");
    public static readonly Color HpFill = Hex("E5484D");
    public static readonly Color HpChip = Hex("FFD166");
    public static readonly Color HpBack = Hex("0F1118");
    public static readonly Color BlockBlue = Hex("3E8EF7");
    public static readonly Color EnergyYellow = Hex("FFC93C");
    public static readonly Color HealGreen = Hex("3DBE6E");
    public static readonly Color Accent = Hex("5B5FE3");

    // Mythic palette: antique gold on deep navy.
    public static readonly Color Gold = Hex("D9B55A");
    public static readonly Color GoldLight = Hex("F7E3A6");
    public static readonly Color GoldDark = Hex("7A5A22");
    public static readonly Color Navy = Hex("161930");
    public static readonly Color NavyLight = Hex("2A2E52");

    // ---------- Mythic pieces (colours baked in; use Image.color = white) ----------

    // Dark rounded panel with a gold frame and a thin inner gold line. 9-sliced.
    public static Sprite FramedPanel(int radius = 22, int frame = 3)
    {
        string key = $"framed{radius}_{frame}";
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        int size = radius * 2 + 4;
        float half = size / 2f;
        Color fill = new Color(Navy.r, Navy.g, Navy.b, 0.92f);
        Color line = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
        Texture2D tex = Paint(size, size, (x, y) =>
        {
            float d = RoundedBox(new Vector2(x - half, y - half), half - 1f, half - 1f, radius);
            Color c = Color.clear;
            c = Over(c, WithAlpha(Gold, Cov(d)));
            c = Over(c, WithAlpha(fill, Cov(d + frame)));
            c = Over(c, WithAlpha(line, Cov(Mathf.Abs(d + frame + 4f) - 0.7f)));
            return c;
        });
        int border = radius + 2;
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        cache[key] = sprite;
        return sprite;
    }

    // A plaque with pointed ends (like an old sign): gold frame, navy gradient, inner line and
    // little gold diamonds near the points. Drawn at 2x the given size for crisp edges.
    public static Sprite Plaque(int width, int height)
    {
        string key = $"plaque{width}x{height}";
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        const float s = 2f;
        int w = Mathf.RoundToInt(width * s);
        int h = Mathf.RoundToInt(height * s);
        float hw = w / 2f - 2f;
        float hh = h / 2f - 2f;
        float k = 0.6f; // how pointy the ends are
        float frame = 5f * s;
        float tipX = hw - hh * k * 0.75f;
        Texture2D tex = Paint(w, h, (x, y) =>
        {
            Vector2 p = new Vector2(x - w / 2f, y - h / 2f);
            float d = PointedBox(p, hw, hh, k);
            float t = y / (float)h; // 0 bottom, 1 top
            Color frameColor = Color.Lerp(GoldDark, GoldLight, t);
            Color fillColor = Color.Lerp(Hex("10122A"), NavyLight, t);
            Color c = Color.clear;
            c = Over(c, WithAlpha(frameColor, Cov(d)));
            c = Over(c, WithAlpha(fillColor, Cov(d + frame)));
            c = Over(c, WithAlpha(new Color(Gold.r, Gold.g, Gold.b, 0.55f), Cov(Mathf.Abs(d + frame + 4f * s) - 0.8f * s)));
            // Diamonds near each point.
            Vector2 q = new Vector2(Mathf.Abs(p.x) - tipX, p.y);
            float diamond = (Mathf.Abs(q.x) + Mathf.Abs(q.y) - 7f * s) * 0.7071f;
            c = Over(c, WithAlpha(Gold, Cov(diamond)));
            return c;
        });
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f * s);
        cache[key] = sprite;
        return sprite;
    }

    // White rune ring: an outer ring, a thin inner ring, and small diamonds between them. Tint it.
    public static Sprite RuneRing(int size = 256, int marks = 12)
    {
        string key = $"rune{size}_{marks}";
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        float half = size / 2f;
        float outer = half - 4f;
        float inner = outer - 18f;
        float markRadius = (outer + inner) / 2f;
        float step = Mathf.PI * 2f / marks;
        Texture2D tex = Paint(size, size, (x, y) =>
        {
            Vector2 p = new Vector2(x - half, y - half);
            float r = p.magnitude;
            float a = Mathf.Clamp01(0.5f - (Mathf.Abs(r - outer) - 2.5f));
            a = Mathf.Max(a, 0.6f * Mathf.Clamp01(0.5f - (Mathf.Abs(r - inner) - 1f)));
            float angle = Mathf.Atan2(p.y, p.x);
            float nearest = Mathf.Round(angle / step) * step;
            Vector2 centre = new Vector2(Mathf.Cos(nearest), Mathf.Sin(nearest)) * markRadius;
            Vector2 d = p - centre;
            Vector2 radial = centre.normalized;
            float u = Vector2.Dot(d, radial);
            float v = Vector2.Dot(d, new Vector2(-radial.y, radial.x));
            float diamond = (Mathf.Abs(u) / 6f + Mathf.Abs(v) / 3.5f - 1f) * 3.5f;
            a = Mathf.Max(a, Mathf.Clamp01(0.5f - diamond));
            return new Color(1f, 1f, 1f, a);
        });
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cache[key] = sprite;
        return sprite;
    }

    // The energy gem: a glowing amber core with a gold rim and a glint.
    public static Sprite EnergyCore(int size = 256)
    {
        string key = "core" + size;
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        float half = size / 2f;
        float radius = half - 3f;
        Color centre = Hex("FFF1C2");
        Color mid = Hex("F4B23E");
        Color edge = Hex("8C3F12");
        Texture2D tex = Paint(size, size, (x, y) =>
        {
            Vector2 p = new Vector2(x - half, y - half);
            float r = p.magnitude;
            float t = Mathf.Clamp01(r / radius);
            Color body = t < 0.45f ? Color.Lerp(centre, mid, t / 0.45f) : Color.Lerp(mid, edge, (t - 0.45f) / 0.55f);
            Color c = WithAlpha(body, Cov(r - radius));
            c = Over(c, WithAlpha(Gold, Cov(Mathf.Abs(r - radius + 5f) - 4f)));
            c = Over(c, WithAlpha(GoldLight, 0.8f * Cov(Mathf.Abs(r - radius + 11f) - 0.8f)));
            // Glint, upper left.
            Vector2 g = new Vector2((p.x + radius * 0.35f) / (radius * 0.32f), (p.y - radius * 0.4f) / (radius * 0.16f));
            float glint = Mathf.Clamp01(1f - g.magnitude);
            c = Over(c, new Color(1f, 1f, 1f, glint * 0.55f));
            return c;
        });
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cache[key] = sprite;
        return sprite;
    }

    // Three-stop vertical gradient (top, middle, bottom), 1 unit square.
    public static Sprite Gradient3(Color top, Color middle, Color bottom, float middleAt = 0.5f)
    {
        const int size = 128;
        Texture2D tex = Paint(size, size, (x, y) =>
        {
            float t = y / size; // 0 bottom, 1 top
            return t < middleAt ? Color.Lerp(bottom, middle, t / middleAt) : Color.Lerp(middle, top, (t - middleAt) / (1f - middleAt));
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // A mountain ridge silhouette (optionally with ancient pillars on it), solid colour with a soft
    // lighter rim along the top. Pivot at the bottom centre; 1 unit wide, `aspect` units tall.
    public static Sprite Ridge(int seed, Color color, Color rim, float baseLevel, float roughness, int pillars, float aspect = 0.25f)
    {
        const int w = 1024;
        int h = Mathf.RoundToInt(w * aspect);
        System.Random rng = new System.Random(seed);
        float[] phase = new float[6];
        for (int i = 0; i < phase.Length; i++)
        {
            phase[i] = (float)rng.NextDouble() * 10f;
        }

        // Ridge height per column (0..1 of the texture height).
        float[] height = new float[w];
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w * Mathf.PI * 2f;
            float n = 0.5f * Mathf.Sin(u * 1f + phase[0])
                    + 0.25f * Mathf.Sin(u * 2.3f + phase[1])
                    + 0.14f * Mathf.Sin(u * 5.1f + phase[2])
                    + 0.07f * Mathf.Abs(Mathf.Sin(u * 11.7f + phase[3]))
                    + 0.04f * Mathf.Abs(Mathf.Sin(u * 23.3f + phase[4]));
            height[x] = baseLevel + n * roughness;
        }

        // Capitals overhang the column, so they're kept as separate bands (top and bottom per column).
        float[] capTop = new float[w];
        float[] capBase = new float[w];
        for (int x = 0; x < w; x++)
        {
            capTop[x] = -1f;
        }

        // Pillars: ancient columns standing on the ridge, some with a capital, some broken off.
        for (int p = 0; p < pillars; p++)
        {
            int cx = (int)(w * (0.1f + 0.8f * (p + 0.2f + (float)rng.NextDouble() * 0.6f) / Mathf.Max(1, pillars)));
            int halfWidth = 8 + rng.Next(5);
            float top = Mathf.Min(0.97f, height[Mathf.Clamp(cx, 0, w - 1)] + 0.12f + (float)rng.NextDouble() * 0.14f);
            bool broken = rng.NextDouble() < 0.4;
            for (int x = cx - halfWidth; x <= cx + halfWidth; x++)
            {
                if (x < 0 || x >= w)
                {
                    continue;
                }
                // Broken columns get a jagged top; whole ones a flat top with a slightly wider cap.
                float columnTop = broken ? top - 0.1f - 0.07f * Mathf.Abs(Mathf.Sin(x * 0.9f)) : top;
                height[x] = Mathf.Max(height[x], columnTop);
            }
            if (!broken)
            {
                for (int x = cx - halfWidth - 5; x <= cx + halfWidth + 5; x++)
                {
                    if (x >= 0 && x < w)
                    {
                        // The cap sits on top, so the silhouette steps out only in the top band.
                        float capBottom = top - 0.035f;
                        if (height[x] < capBottom)
                        {
                            capTop[x] = Mathf.Min(0.99f, top + 0.02f);
                            capBase[x] = capBottom;
                        }
                    }
                }
            }
        }

        Texture2D tex = Paint(w, h, (x, y) =>
        {
            int column = Mathf.Clamp((int)x, 0, w - 1);
            float ridge = height[column] * h;
            float d = y - ridge; // negative = inside the hill
            if (capTop[column] > 0f)
            {
                float cap = Mathf.Max(capBase[column] * h - y, y - capTop[column] * h);
                d = Mathf.Min(d, cap);
            }
            Color c = WithAlpha(color, Cov(d));
            float rimBand = Mathf.Clamp01(1f - Mathf.Abs(d + 2f) / 3f);
            c = Over(c, WithAlpha(rim, rimBand * 0.6f));
            return c;
        });
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w);
    }

    // Dark soft edges, clear in the middle (a vignette). 1 unit square.
    public static Sprite Vignette(int size = 256)
    {
        string key = "vignette" + size;
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        float half = size / 2f;
        Texture2D tex = Paint(size, size, (x, y) =>
        {
            Vector2 p = new Vector2((x - half) / half, (y - half) / half);
            float d = Mathf.Clamp01((p.magnitude - 0.55f) / 0.75f);
            return new Color(0.04f, 0.03f, 0.08f, d * d * 0.85f);
        });
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cache[key] = sprite;
        return sprite;
    }

    // A small diamond, filled or outlined (energy pips). White, tint it.
    public static Sprite Diamond(bool filled, int size = 64)
    {
        float r = size / 2f - 3f;
        return Shape((filled ? "dfill" : "dline") + size, size, filled ? 0f : 6f,
            p => (Mathf.Abs(p.x) + Mathf.Abs(p.y) - r) * 0.7071f);
    }

    private static Texture2D Paint(int width, int height, System.Func<float, float, Color> pixel)
    {
        Texture2D tex = NewTexture(width, height);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = pixel(x + 0.5f, y + 0.5f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static float Cov(float distance)
    {
        return Mathf.Clamp01(0.5f - distance);
    }

    private static Color WithAlpha(Color c, float coverage)
    {
        return new Color(c.r, c.g, c.b, c.a * coverage);
    }

    // Paints src over dst.
    private static Color Over(Color dst, Color src)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0.0001f)
        {
            return Color.clear;
        }
        Color rgb = (src * src.a + dst * dst.a * (1f - src.a)) / a;
        return new Color(rgb.r, rgb.g, rgb.b, a);
    }

    private static float RoundedBox(Vector2 p, float hw, float hh, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(hw - radius, hh - radius);
        return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
    }

    // Box with pointed left and right ends.
    private static float PointedBox(Vector2 p, float hw, float hh, float k)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
        float top = q.y - hh;
        float side = (q.x + k * q.y - hw) / Mathf.Sqrt(1f + k * k);
        return Mathf.Max(top, side);
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }

    // Bright UI version of each game colour.
    public static Color ForCardColor(CardColor color)
    {
        switch (color)
        {
            case CardColor.Red: return Hex("E5484D");
            case CardColor.Orange: return Hex("F7883B");
            case CardColor.Yellow: return Hex("FFC93C");
            case CardColor.Green: return Hex("3DBE6E");
            case CardColor.Blue: return Hex("3E8EF7");
            case CardColor.Purple: return Hex("9B5DE5");
            case CardColor.Black: return Hex("2B2B33");
            default: return Hex("E8E8E8"); // White
        }
    }

    // A white rounded rectangle, 9-sliced so it stretches to any size. Tint it with Image.color.
    public static Sprite RoundedRect(int radius = 24)
    {
        string key = "rr" + radius;
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        int size = radius * 2 + 4;
        float half = size / 2f;
        Texture2D tex = NewTexture(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(half - radius - 1, half - radius - 1);
                float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
        }
        tex.Apply();
        int border = radius + 2;
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        cache[key] = sprite;
        return sprite;
    }

    // A filled white circle.
    public static Sprite Circle(int size = 128)
    {
        return Shape("circle" + size, size, 0f, p => p.magnitude - (size / 2f - 2));
    }

    // White outlines used by the background: 0 = circle, 1 = triangle, 2 = square.
    public static Sprite Outline(int shape, int size = 256, float thickness = 10f)
    {
        float r = size / 2f - thickness - 2;
        switch (shape)
        {
            case 1: return Shape("tri" + size, size, thickness, p => Triangle(new Vector2(p.x, p.y + r * 0.2f), r * 0.85f));
            case 2: return Shape("sq" + size, size, thickness, p => Box(p, new Vector2(r * 0.8f, r * 0.8f)));
            default: return Shape("ring" + size, size, thickness, p => p.magnitude - r);
        }
    }

    // Soft round glow, white in the middle fading to clear at the edge.
    public static Sprite Glow(int size = 128)
    {
        string key = "glow" + size;
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        Texture2D tex = NewTexture(size, size);
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
            }
        }
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cache[key] = sprite;
        return sprite;
    }

    // Vertical gradient, 1 world unit square (scale it to fit).
    public static Sprite VerticalGradient(Color top, Color bottom)
    {
        const int size = 128;
        Texture2D tex = NewTexture(size, size);
        for (int y = 0; y < size; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (size - 1f));
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ---------- helpers ----------

    private delegate float Sdf(Vector2 p);

    private static Sprite Shape(string key, int size, float thickness, Sdf sdf)
    {
        if (cache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }
        Texture2D tex = NewTexture(size, size);
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = sdf(new Vector2(x + 0.5f - half, y + 0.5f - half));
                if (thickness > 0f)
                {
                    d = Mathf.Abs(d) - thickness / 2f; // outline instead of fill
                }
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
        }
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cache[key] = sprite;
        return sprite;
    }

    private static Texture2D NewTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    private static float Box(Vector2 p, Vector2 b)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
        return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0);
    }

    // Equilateral triangle, point up.
    private static float Triangle(Vector2 p, float r)
    {
        const float k = 1.7320508f;
        p.x = Mathf.Abs(p.x) - r;
        p.y = p.y + r / k;
        if (p.x + k * p.y > 0f)
        {
            p = new Vector2(p.x - k * p.y, -k * p.x - p.y) / 2f;
        }
        p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
        return -p.magnitude * Mathf.Sign(p.y);
    }
}
