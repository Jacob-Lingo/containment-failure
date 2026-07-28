using UnityEngine;

/// Procedural VFX for the Dragonfire Beam — a two-layer flame jet: a wide, dim outer
/// plume in the caller's colour with a narrow white-hot core riding inside it,
/// both flickering and fading out. Built from one shared solid sprite (no art
/// asset); a beam's shape is just a non-uniformly scaled square, so no
/// texture-building code beyond the 4x4 white block is needed.
public class BeamVfx : MonoBehaviour
{
    private const float Lifetime = 0.28f;

    private static Sprite solidSprite;

    private SpriteRenderer sr;
    private Color baseColor;
    private Vector3 baseScale;
    private float flickerSeed;
    private float timer;

    public static void Spawn(Vector3 origin, Vector2 aimDir, float length, float width, Color color)
    {
        Vector2 dir = aimDir.normalized;
        GameObject go = new GameObject("BeamVfx");
        go.transform.position = origin + (Vector3)(dir * (length * 0.5f));
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        go.transform.localScale = new Vector3(length, width, 1f);
        AddLayer(go, color, 13);

        // Inner core: a child so it inherits the plume's length/rotation and
        // only needs its own (thinner, brighter) local scale.
        GameObject core = new GameObject("BeamCore");
        core.transform.SetParent(go.transform, false);
        core.transform.localScale = new Vector3(1f, 0.38f, 1f);
        AddLayer(core, new Color(1f, 0.97f, 0.75f, Mathf.Min(1f, color.a + 0.15f)), 14);

        // Muzzle burst where the flame leaves the monster's mouth.
        HitFlashFx.Spawn(origin, new Color(color.r, color.g, color.b, 0.9f), width * 1.4f);
    }

    private static void AddLayer(GameObject go, Color color, int sortingOrder)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<BeamVfx>().Init(sr);
    }

    private void Init(SpriteRenderer renderer)
    {
        sr = renderer;
        baseColor = sr.color;
        baseScale = transform.localScale;
        flickerSeed = Random.value * 100f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Lifetime);

        // Flame breath doesn't hold a constant width — noise, not a random
        // per-frame value, so it wobbles instead of strobing.
        float flicker = 0.8f + Mathf.PerlinNoise(flickerSeed, Time.time * 25f) * 0.45f;
        transform.localScale = new Vector3(baseScale.x, baseScale.y * flicker, baseScale.z);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t));

        if (timer >= Lifetime) Destroy(gameObject);
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        // pixelsPerUnit = size so the sprite is exactly 1x1 world units at
        // scale 1 — callers scale directly by (length, width).
        solidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return solidSprite;
    }
}
