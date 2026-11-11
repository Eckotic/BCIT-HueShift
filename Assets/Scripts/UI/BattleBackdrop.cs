using System.Collections.Generic;
using UnityEngine;

// The battle background: a deep navy gradient with faint circles, triangles and squares
// (the game's three shapes, in its three primaries) slowly drifting behind the fight.
// Built in code and parented to the camera, so it always fills the view.
public class BattleBackdrop : MonoBehaviour
{
    private class Floater
    {
        public Transform transform;
        public Vector2 velocity;
        public float spin;
    }

    private const float Distance = 60f;
    private const int ShapeCount = 16;

    private Camera cam;
    private float halfWidth;
    private float halfHeight;
    private readonly List<Floater> floaters = new List<Floater>();

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

    private void Build(Camera camera, Material spriteMaterial)
    {
        cam = camera;
        if (cam.orthographic)
        {
            halfHeight = cam.orthographicSize;
        }
        else
        {
            halfHeight = Distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
        halfWidth = halfHeight * cam.aspect;

        // Gradient sky.
        SpriteRenderer sky = NewSprite("Gradient", UIShapes.VerticalGradient(UIShapes.Hex("2D3154"), UIShapes.Hex("12141C")), spriteMaterial, -100);
        sky.transform.localScale = new Vector3(halfWidth * 2.4f, halfHeight * 2.4f, 1f);

        // A soft glow behind the middle of the fight.
        SpriteRenderer glow = NewSprite("Glow", UIShapes.Glow(256), spriteMaterial, -99);
        glow.color = new Color(0.55f, 0.5f, 1f, 0.18f);
        glow.transform.localPosition = new Vector3(0f, halfHeight * 0.1f, -0.5f);
        glow.transform.localScale = Vector3.one * halfHeight * 2.4f;

        // Drifting shapes.
        Color[] tints = { UIShapes.Hex("E5484D"), UIShapes.Hex("3E8EF7"), UIShapes.Hex("FFC93C") };
        Random.State saved = Random.state;
        Random.InitState(7082);
        for (int i = 0; i < ShapeCount; i++)
        {
            int shape = i % 3;
            SpriteRenderer sr = NewSprite("Shape", UIShapes.Outline(shape), spriteMaterial, -98);
            Color c = tints[(i / 3 + shape) % 3];
            sr.color = new Color(c.r, c.g, c.b, Random.Range(0.07f, 0.14f));
            float size = Random.Range(halfHeight * 0.18f, halfHeight * 0.5f);
            sr.transform.localScale = Vector3.one * size;
            sr.transform.localPosition = new Vector3(Random.Range(-halfWidth, halfWidth), Random.Range(-halfHeight, halfHeight), -1f - i * 0.01f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            floaters.Add(new Floater
            {
                transform = sr.transform,
                velocity = Random.insideUnitCircle * halfHeight * 0.02f,
                spin = Random.Range(-6f, 6f)
            });
        }
        Random.state = saved;
    }

    private SpriteRenderer NewSprite(string name, Sprite sprite, Material material, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
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
        float marginX = halfWidth * 1.25f;
        float marginY = halfHeight * 1.25f;
        foreach (Floater f in floaters)
        {
            Vector3 p = f.transform.localPosition + (Vector3)(f.velocity * Time.deltaTime);
            // Wrap around the edges so the drift never runs out.
            if (p.x > marginX) p.x = -marginX;
            if (p.x < -marginX) p.x = marginX;
            if (p.y > marginY) p.y = -marginY;
            if (p.y < -marginY) p.y = marginY;
            f.transform.localPosition = p;
            f.transform.Rotate(0f, 0f, f.spin * Time.deltaTime);
        }
    }
}
