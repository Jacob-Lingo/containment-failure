using UnityEngine;

/// Persistent yellow "exp orb" — distinct from ExpOrb (the green heal orb
/// dropped by kills). SpawnDirector maintains a pool of these scattered
/// throughout the map, independent of enemy deaths, topping the pool back
/// up on a timer. Picking one up grants RunStats.BonusExp, which feeds
/// EvolutionSystem's level-up counter alongside raw kills.
public class ExpPickup : MonoBehaviour
{
    private const float PickupRadius = 0.5f;
    private const int ExpPerOrb = 1;

    private static Sprite orbSprite;

    public static GameObject Spawn(Vector3 position)
    {
        GameObject go = new GameObject("ExpPickup");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrbSprite();
        sr.color = new Color(1f, 0.85f, 0.2f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = PickupRadius;

        go.AddComponent<ExpPickup>();
        return go;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        RunStats.RegisterBonusExp(ExpPerOrb);
        HitFlashFx.Spawn(transform.position, new Color(1f, 0.85f, 0.2f, 0.9f), 0.3f);
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
