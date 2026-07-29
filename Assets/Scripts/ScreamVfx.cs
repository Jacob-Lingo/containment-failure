using System.Collections.Generic;
using UnityEngine;

/// Procedural VFX for Scream — a ghostly shockwave that expands outward and
/// dissipates: a bright leading ring with a faint haze trailing behind it, as a
/// directional wedge for the arc mode and a full ring for the Radius upgrade.
/// Built entirely at runtime via pixel tests (no art asset), same approach as
/// HitFlashFx/DamageNumber. Sprites are built with pixelsPerUnit = size/2 so a
/// caller's `localScale = range` maps directly to the real hitbox radius — the
/// wave expands out to exactly the attack's true reach.
public class ScreamVfx : MonoBehaviour
{
    private const float Lifetime = 0.3f;
    private const int TextureSize = 128;

    private static readonly Dictionary<int, Sprite> wedgeCache = new Dictionary<int, Sprite>();
    private static Sprite circleSprite;

    private SpriteRenderer sr;
    private Color baseColor;
    private float fullScale;
    private float timer;

    public static void SpawnCone(Vector3 origin, Vector2 aimDir, float arcDegrees, float range, Color color)
    {
        GameObject go = new GameObject("ScreamConeVfx");
        go.transform.position = origin;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWedgeSprite(arcDegrees);
        sr.color = color;
        sr.sortingOrder = 12;

        go.AddComponent<ScreamVfx>().Init(sr, range);
    }

    public static void SpawnRing(Vector3 origin, float range, Color color)
    {
        GameObject go = new GameObject("ScreamRingVfx");
        go.transform.position = origin;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = color;
        sr.sortingOrder = 12;

        go.AddComponent<ScreamVfx>().Init(sr, range);
    }

    private void Init(SpriteRenderer renderer, float range)
    {
        sr = renderer;
        baseColor = sr.color;
        fullScale = range;
        transform.localScale = Vector3.one * (range * 0.35f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Lifetime);

        // Eases out, so the wave leaves fast and settles at exactly the real
        // hitbox radius rather than overshooting it.
        float grow = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(fullScale * 0.35f, fullScale, grow);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t));

        if (timer >= Lifetime) Destroy(gameObject);
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        circleSprite = BuildSprite((dist, angle, radius) => RingAlpha(dist, radius));
        return circleSprite;
    }

    private static Sprite GetWedgeSprite(float arcDegrees)
    {
        int key = Mathf.RoundToInt(arcDegrees);
        if (wedgeCache.TryGetValue(key, out var cached)) return cached;

        float halfArc = arcDegrees * 0.5f;
        var sprite = BuildSprite((dist, angle, radius) =>
        {
            float edge = Mathf.Clamp01((halfArc - Mathf.Abs(angle)) / 10f); // soften the wedge's side edges
            return RingAlpha(dist, radius) * edge;
        });

        wedgeCache[key] = sprite;
        return sprite;
    }

    /// Bright leading ring near the outer edge over a faint interior haze —
    /// reads as a shockwave rather than a solid disc of colour.
    private static float RingAlpha(float dist, float radius)
    {
        float r = dist / radius;
        if (r > 1f) return 0f;
        float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.18f);
        return Mathf.Clamp01(ring * ring + 0.16f * r);
    }

    private delegate float ShapeTest(float distance, float angleDegrees, float radius);

    private static Sprite BuildSprite(ShapeTest test)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float center = TextureSize / 2f;
        float radius = TextureSize / 2f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // 0 = facing +x
                float alpha = test(dist, angle, radius);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        // pixelsPerUnit = size/2 so the sprite's world radius is exactly 1
        // unit at localScale=1 — callers can scale directly by world range.
        return Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize / 2f);
    }
}
