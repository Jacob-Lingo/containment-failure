using UnityEngine;

/// Small collectible dropped by some guard kills (see GuardHealth's death
/// branch) — a "life orb" the player walks over to heal 1 HP, giving kills
/// something tangible to pick up rather than just vanishing. Built entirely
/// at runtime (no art asset), same cached-sprite pattern as HitFlashFx.
public class ExpOrb : MonoBehaviour
{
    private const float Lifetime = 8f;
    private static Sprite orbSprite;

    public static void Spawn(Vector3 position)
    {
        GameObject go = new GameObject("ExpOrb");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrbSprite();
        sr.color = new Color(0.4f, 1f, 0.5f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<ExpOrb>();
        Destroy(go, Lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerHealth>(out var health))
            health.Heal(1);

        HitFlashFx.Spawn(transform.position, new Color(0.4f, 1f, 0.5f, 0.9f), 0.3f);
        Destroy(gameObject);
    }

    private static Sprite GetOrbSprite()
    {
        if (orbSprite != null) return orbSprite;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, dist <= radius ? 1f : 0f));
            }
        }
        tex.Apply();

        orbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return orbSprite;
    }
}
