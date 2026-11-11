using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// The battle background, built in code and parented to the camera so it always fills the view:
// a twilight sky (indigo -> lavender -> amber), faint stars,
// layered mountains fading into the haze with ancient pillars, a ground band, drifting shape
// outlines and rising golden dust, and a soft vignette. It's kept light in the middle so the
// charcoal character art reads clearly, and each character gets a warm backlight halo.
public class BattleBackdrop : MonoBehaviour
{
    private class Floater
    {
        public Transform transform;
        public Vector2 velocity;
        public float spin;
    }

    private class Backlight
    {
        public EntityVisual visual;
        public Transform halo;
        public Transform pool;
    }

    private const float Distance = 60f;

    private Camera cam;
    private Material material;
    private float halfWidth;
    private float halfHeight;
    private readonly List<Floater> floaters = new List<Floater>();
    private readonly List<Floater> motes = new List<Floater>();
    private readonly List<Backlight> backlights = new List<Backlight>();

    public static BattleBackdrop Create(Camera camera, Material spriteMaterial)
    {
        if (camera == null)
        {
            return null;
        }
        GameObject root = new GameObject("BattleBackdrop");
        root.transform.SetParent(camera.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, Distance);
        root.transform.localRotation = Quaternion.identity;

        BattleBackdrop backdrop = root.AddComponent<BattleBackdrop>();
        backdrop.Build(camera, spriteMaterial);
        return backdrop;
    }

    // Height on screen as a fraction (0 = bottom, 1 = top) -> local y on the backdrop.
    private float Y(float fraction)
    {
        return (fraction - 0.5f) * 2f * halfHeight;
    }

    private void Build(Camera camera, Material spriteMaterial)
    {
        cam = camera;
        material = spriteMaterial;
        halfHeight = cam.orthographic ? cam.orthographicSize : Distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        halfWidth = halfHeight * cam.aspect;
        float width = halfWidth * 2.3f;
        float height = halfHeight * 2.3f;

        // Sky.
        SpriteRenderer sky = NewSprite("Sky", UIShapes.Gradient3(UIShapes.Hex("2B2C5A"), UIShapes.Hex("7A68A6"), UIShapes.Hex("F0B48A"), 0.42f), -100, 0f);
        sky.transform.localScale = new Vector3(width, height, 1f);

        // Faint stars in the upper sky, twinkling.
        Random.State saved = Random.state;
        Random.InitState(7082);
        for (int i = 0; i < 22; i++)
        {
            SpriteRenderer star = NewSprite("Star", UIShapes.Glow(32), -99, -0.1f);
            star.transform.localPosition = new Vector3(Random.Range(-halfWidth, halfWidth), Y(Random.Range(0.62f, 1f)), -0.1f);
            star.transform.localScale = Vector3.one * Random.Range(0.12f, 0.35f) * halfHeight * 0.1f;
            float alpha = Random.Range(0.35f, 0.85f);
            star.color = new Color(1f, 0.97f, 0.9f, alpha);
            star.DOFade(alpha * 0.25f, Random.Range(1.2f, 3f)).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(Random.Range(0f, 2f)).SetLink(star.gameObject);
        }

        // Mountains, far to near: lighter and hazier further away. All start at the bottom of the
        // screen so there are no gaps; the ridge line sits at baseLevel of each layer's height.
        AddRidge("FarRidge", 11, UIShapes.Hex("A58FC0"), UIShapes.Hex("F2D2CC"), 0.75f, 0.12f, 0, -95, 1.0f);
        AddRidge("MidRidge", 23, UIShapes.Hex("7A66A0"), UIShapes.Hex("E0BCC6"), 0.68f, 0.08f, 5, -94, 0.8f);
        AddRidge("NearRidge", 37, UIShapes.Hex("54477D"), UIShapes.Hex("B896BE"), 0.6f, 0.1f, 0, -92, 0.5f);

        // Drifting shape outlines (the game's circle, triangle and square) and rising golden dust.
        for (int i = 0; i < 7; i++)
        {
            int shape = i % 3;
            SpriteRenderer sr = NewSprite("Shape", UIShapes.Outline(shape), -91, -0.8f - i * 0.01f);
            sr.color = new Color(1f, 0.95f, 0.85f, Random.Range(0.06f, 0.12f));
            sr.transform.localScale = Vector3.one * Random.Range(halfHeight * 0.15f, halfHeight * 0.4f);
            sr.transform.localPosition = new Vector3(Random.Range(-halfWidth, halfWidth), Random.Range(-halfHeight, halfHeight), sr.transform.localPosition.z);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            floaters.Add(new Floater { transform = sr.transform, velocity = Random.insideUnitCircle * halfHeight * 0.015f, spin = Random.Range(-5f, 5f) });
        }
        for (int i = 0; i < 14; i++)
        {
            SpriteRenderer mote = NewSprite("Dust", UIShapes.Glow(32), -90, -1f);
            mote.color = new Color(1f, 0.85f, 0.5f, Random.Range(0.25f, 0.6f));
            mote.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f) * halfHeight * 0.1f;
            mote.transform.localPosition = new Vector3(Random.Range(-halfWidth, halfWidth), Random.Range(-halfHeight, halfHeight), -1f);
            motes.Add(new Floater
            {
                transform = mote.transform,
                velocity = new Vector2(Random.Range(-0.01f, 0.01f), Random.Range(0.015f, 0.04f)) * halfHeight,
                spin = Random.Range(0f, 10f) // used as a sway phase
            });
        }
        Random.state = saved;

        // Soft vignette over the whole backdrop (still behind the characters).
        SpriteRenderer vignette = NewSprite("Vignette", UIShapes.Vignette(256), -80, -1.5f);
        vignette.transform.localScale = new Vector3(halfWidth * 2.05f, halfHeight * 2.05f, 1f);
    }

    private void AddRidge(string name, int seed, Color color, Color rim, float baseLevel, float roughness, int pillars, int order, float heightFraction)
    {
        float width = halfWidth * 2.3f;
        const float aspect = 0.25f;
        SpriteRenderer ridge = NewSprite(name, UIShapes.Ridge(seed, color, rim, baseLevel, roughness, pillars, aspect), order, -0.5f - (order + 100) * 0.01f);
        // The sprite is 1 unit wide and `aspect` tall; stretch it to the screen width and the wanted height.
        ridge.transform.localScale = new Vector3(width, halfHeight * heightFraction / aspect, 1f);
        ridge.transform.localPosition = new Vector3(0f, Y(0f), ridge.transform.localPosition.z);
    }

    // A warm halo behind a character and a lit pool under their feet, so dark art stands out.
    public void AddBacklight(EntityVisual visual)
    {
        if (visual == null)
        {
            return;
        }
        GameObject root = new GameObject("Backlight_" + visual.name);

        SpriteRenderer halo = NewWorldSprite(root.transform, "Halo", UIShapes.Glow(256), -20);
        halo.color = new Color(1f, 0.9f, 0.75f, 0.55f);

        SpriteRenderer pool = NewWorldSprite(root.transform, "Pool", UIShapes.Glow(256), -21);
        pool.color = new Color(1f, 0.85f, 0.65f, 0.45f);

        backlights.Add(new Backlight { visual = visual, halo = halo.transform, pool = pool.transform });
    }

    private SpriteRenderer NewSprite(string name, Sprite sprite, int order, float z)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (material != null)
        {
            sr.sharedMaterial = material;
        }
        sr.sortingOrder = order;
        return sr;
    }

    private SpriteRenderer NewWorldSprite(Transform parent, string name, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (material != null)
        {
            sr.sharedMaterial = material;
        }
        sr.sortingOrder = order;
        return sr;
    }

    void Update()
    {
        float marginX = halfWidth * 1.2f;
        float marginY = halfHeight * 1.2f;
        foreach (Floater f in floaters)
        {
            Vector3 p = f.transform.localPosition + (Vector3)(f.velocity * Time.deltaTime);
            if (p.x > marginX) p.x = -marginX;
            if (p.x < -marginX) p.x = marginX;
            if (p.y > marginY) p.y = -marginY;
            if (p.y < -marginY) p.y = marginY;
            f.transform.localPosition = p;
            f.transform.Rotate(0f, 0f, f.spin * Time.deltaTime);
        }
        foreach (Floater m in motes)
        {
            Vector3 p = m.transform.localPosition + (Vector3)(m.velocity * Time.deltaTime);
            p.x += Mathf.Sin(Time.time * 0.8f + m.spin) * halfHeight * 0.002f;
            if (p.y > marginY)
            {
                p.y = -marginY;
                p.x = Random.Range(-halfWidth, halfWidth);
            }
            m.transform.localPosition = p;
        }
    }

    void LateUpdate()
    {
        foreach (Backlight b in backlights)
        {
            if (b.visual == null || b.halo == null)
            {
                continue;
            }
            Bounds bounds = b.visual.VisibleBounds();
            float size = Mathf.Max(bounds.size.x, bounds.size.y);
            // Halo: centred on the character, a bit behind it (further from the camera).
            Vector3 away = cam != null ? cam.transform.forward * 0.5f : Vector3.forward * 0.5f;
            b.halo.position = bounds.center + away;
            b.halo.localScale = Vector3.one * size * 1.6f * (1f + 0.03f * Mathf.Sin(Time.time * 1.5f));
            // Pool: a flat ellipse at the feet.
            b.pool.position = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.04f, bounds.center.z) + away;
            b.pool.localScale = new Vector3(bounds.size.x * 1.4f, bounds.size.x * 0.3f, 1f);
        }
    }
}
