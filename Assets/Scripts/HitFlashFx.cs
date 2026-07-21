using UnityEngine;

/// Procedural attack/impact flash — builds one shared soft-edged circle
/// sprite at runtime (no art asset needed, same approach as DamageNumber)
/// and reuses it, tinted and sized per call, for muzzle flashes and hits.
public class HitFlashFx : MonoBehaviour
{
    private const float Lifetime = 0.15f;

    private static Sprite sharedCircle;

    private SpriteRenderer sr;
    private Vector3 startScale;
    private Color baseColor;
    private float timer;

    public static void Spawn(Vector3 worldPos, Color color, float scale = 0.5f)
    {
        GameObject go = new GameObject("HitFlashFx");
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = color;
        sr.sortingOrder = 15;

        go.AddComponent<HitFlashFx>();
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
        baseColor = sr.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Lifetime);

        transform.localScale = startScale * (1f + t * 1.5f);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t));

        if (timer >= Lifetime) Destroy(gameObject);
    }

    private static Sprite GetCircleSprite()
    {
        if (sharedCircle != null) return sharedCircle;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(1f - dist / (size / 2f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        tex.Apply();

        sharedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return sharedCircle;
    }
}
