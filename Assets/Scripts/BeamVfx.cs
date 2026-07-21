using UnityEngine;

/// Procedural VFX for the Kamehameha Beam — a solid rectangle stretched
/// along the aim direction, built from the same shared-sprite technique as
/// HitFlashFx/ScreamVfx (no art asset). Since a beam's shape is just a
/// non-uniformly scaled square, no new texture-building code is needed.
public class BeamVfx : MonoBehaviour
{
    private const float Lifetime = 0.2f;

    private static Sprite solidSprite;

    private SpriteRenderer sr;
    private Color baseColor;
    private float timer;

    public static void Spawn(Vector3 origin, Vector2 aimDir, float length, float width, Color color)
    {
        GameObject go = new GameObject("BeamVfx");
        go.transform.position = origin + (Vector3)(aimDir.normalized * (length * 0.5f));
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = new Vector3(length, width, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = color;
        sr.sortingOrder = 13;

        go.AddComponent<BeamVfx>().Init(sr);
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
