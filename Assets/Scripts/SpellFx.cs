using System.Collections.Generic;
using UnityEngine;

/// Plays the animated pixel spell effects in Resources/Fx.
///
/// The sheets are plain Textures, not Sprite assets: every sheet is a square
/// grid of 100x100 frames (36-121 of them), and hand-authoring that many sprite
/// rects into a .meta for twenty sheets would be thousands of lines of
/// generated YAML. Instead the frames are cut once at runtime with
/// Sprite.Create and cached, which needs no import settings beyond "leave it
/// as a texture".
///
/// Art: "Free Pixel Effects Pack" / Pixel FX Designer, CC0 (public domain, no
/// attribution required) — see Assets/Art/Fantasy/FxPixelEffects/README.txt.
public static class SpellFx
{
    // Effect names, matching the files in Resources/Fx.
    public const string WeaponHit = "weaponhit";
    public const string MagickaHit = "magickahit";
    public const string Fire = "fire";
    public const string BrightFire = "brightfire";
    public const string FlameLash = "flamelash";
    public const string FireSpin = "firespin";
    public const string Vortex = "vortex";
    public const string Freezing = "freezing";
    public const string BlueFire = "bluefire";
    public const string FelSpell = "felspell";
    public const string Phantom = "phantom";
    public const string Midnight = "midnight";
    public const string Nebula = "nebula";
    public const string MagicSpell = "magicspell";
    public const string Casting = "casting";
    public const string SunBurn = "sunburn";
    public const string ProtectionCircle = "protectioncircle";
    public const string MagicBubbles = "magicbubbles";

    // Loops rather than bursts — they read as "something is ongoing", so they
    // mark state (a channel filling, a ward standing) instead of an impact.
    public const string ChannelRing = "loading";
    public const string Sigil = "magic8";

    private const int FramePixels = 100;
    private const float DefaultFps = 30f;

    private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    /// `scale` is the effect's diameter in world units. `speed` multiplies the
    /// playback rate — 2 makes a long 100-frame sheet snappy enough for a
    /// per-hit effect.
    public static void Play(string effect, Vector3 position, float scale = 2f, float speed = 1f,
                            Color? tint = null, float rotation = 0f)
    {
        Sprite[] frames = Frames(effect);
        if (frames == null || frames.Length == 0) return;

        var go = new GameObject("SpellFx_" + effect);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = Vector3.one * scale;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.color = tint ?? Color.white;
        renderer.sortingOrder = 20; // above creatures, below UI

        go.AddComponent<Player>().Init(frames, DefaultFps * Mathf.Max(0.01f, speed));
    }

    /// A single frame held still, for things that persist rather than play —
    /// a hazard patch on the ground, an icon. Shares the same frame cache as
    /// Play(), so asking for one costs nothing extra.
    public static Sprite StaticFrame(string effect, int index)
    {
        Sprite[] frames = Frames(effect);
        if (frames == null || frames.Length == 0) return null;
        return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    /// Cuts a sheet into frames once, then reuses them. Row-major, top-left
    /// first — Unity's texture origin is bottom-left, so rows are read upward.
    private static Sprite[] Frames(string effect)
    {
        if (cache.TryGetValue(effect, out var cached)) return cached;

        var texture = Resources.Load<Texture2D>("Fx/" + effect);
        if (texture == null)
        {
            Debug.LogWarning($"SpellFx: no texture at Resources/Fx/{effect}.");
            cache[effect] = null;
            return null;
        }

        int columns = texture.width / FramePixels;
        int rows = texture.height / FramePixels;
        var frames = new List<Sprite>(columns * rows);

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int column = 0; column < columns; column++)
            {
                var rect = new Rect(column * FramePixels, row * FramePixels, FramePixels, FramePixels);
                frames.Add(Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), FramePixels));
            }
        }

        cache[effect] = frames.ToArray();
        return cache[effect];
    }

    /// Steps frames and destroys itself at the end. Unscaled time so effects
    /// still resolve during a Juice hitstop instead of freezing mid-burst.
    private class Player : MonoBehaviour
    {
        private Sprite[] frames;
        private SpriteRenderer renderer;
        private float fps;
        private float elapsed;

        public void Init(Sprite[] frames, float fps)
        {
            this.frames = frames;
            this.fps = fps;
            renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;

            int index = Mathf.FloorToInt(elapsed * fps);
            if (index >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }

            renderer.sprite = frames[index];
        }
    }
}
