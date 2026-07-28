using UnityEngine;

/// The yellow coin — distinct from ExpOrb (the green heal orb dropped by
/// kills). SpawnDirector maintains a pool of these scattered throughout the
/// map, topping it back up on a timer, and GuardHealth drops them on death.
/// Picking one up banks MetaProgression.CoinValue coins immediately, spendable
/// in the shop between runs.
///
/// Was ExpPickup (it fed the level-up counter) until 2026-07-28 — coins are
/// now purely currency, so level-ups come from kills alone.
public class Coin : MonoBehaviour
{
    private const float PickupRadius = 0.5f;

    private static Sprite coinSprite;

    public static GameObject Spawn(Vector3 position)
    {
        GameObject go = new GameObject("Coin");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCoinSprite();
        sr.color = new Color(1f, 0.85f, 0.2f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = PickupRadius;

        go.AddComponent<Coin>();
        go.AddComponent<OrbMagnet>();
        return go;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        MetaProgression.AddCoins(MetaProgression.CoinValue);
        RunStats.RegisterCoin(MetaProgression.CoinValue);

        HitFlashFx.Spawn(transform.position, new Color(1f, 0.85f, 0.2f, 0.9f), 0.3f);
        Destroy(gameObject);
    }

    private static Sprite GetCoinSprite()
    {
        if (coinSprite != null) return coinSprite;

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

        coinSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return coinSprite;
    }
}
