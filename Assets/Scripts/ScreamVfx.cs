using System.Collections.Generic;
using UnityEngine;

/// Procedural VFX for the Scream ability — a directional wedge for the arc
/// mode, a full circle for the Radius Scream upgrade. Built entirely at
/// runtime via pixel tests (no art asset needed), same approach as
/// HitFlashFx/DamageNumber. Unlike HitFlashFx's hand-tuned hit-flash scale,
/// these sprites are built with pixelsPerUnit = size/2 so a caller's
/// `localScale = range` maps directly to the real hitbox radius — the VFX
/// truthfully shows the attack's actual reach.
public class ScreamVfx : MonoBehaviour
{
    private const float Lifetime = 0.3f;
    private const int TextureSize = 128;

    private static readonly Dictionary<int, Sprite> wedgeCache = new Dictionary<int, Sprite>();
    private static Sprite circleSprite;

    private SpriteRenderer sr;
    private Color baseColor;
    private float timer;

    public static void SpawnCone(Vector3 origin, Vector2 aimDir, float arcDegrees, float range, Color color)
    {
        GameObject go = new GameObject("ScreamConeVfx");
        go.transform.position = origin;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = Vector3.one * range;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWedgeSprite(arcDegrees);
        sr.color = color;
        sr.sortingOrder = 12;

        go.AddComponent<ScreamVfx>().Init(sr);
    }

    public static void SpawnRing(Vector3 origin, float range, Color color)
    {
        GameObject go = new GameObject("ScreamRingVfx");
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * range;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = color;
        sr.sortingOrder = 12;

        go.AddComponent<ScreamVfx>().Init(sr);
    }

    private void Init(SpriteRenderer renderer)
    {
        sr = renderer;
        baseColor = sr.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Lifetime);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t));

        if (timer >= Lifetime) Destroy(gameObject);
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        circleSprite = BuildSprite((dist, angle, radius) => dist <= radius ? 1f : 0f);
        return circleSprite;
    }

    private static Sprite GetWedgeSprite(float arcDegrees)
    {
        int key = Mathf.RoundToInt(arcDegrees);
        if (wedgeCache.TryGetValue(key, out var cached)) return cached;

        float halfArc = arcDegrees * 0.5f;
        var sprite = BuildSprite((dist, angle, radius) => dist <= radius && Mathf.Abs(angle) <= halfArc ? 1f : 0f);

        wedgeCache[key] = sprite;
        return sprite;
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
