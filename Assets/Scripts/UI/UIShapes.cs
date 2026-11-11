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
