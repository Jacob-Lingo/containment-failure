using UnityEngine;

/// Tiny Resources-based SFX helper, mirroring the existing
/// Resources.Load&lt;Sprite&gt;("Combat/...") convention but for one-shot audio.
/// Clips live at Assets/Resources/Audio/{baseName}_{0..variantCount-1}.ogg.
public static class Sfx
{
    public static void PlayRandom(string baseName, int variantCount, Vector3 position, float volume = 1f)
    {
        int index = Random.Range(0, variantCount);
        AudioClip clip = Resources.Load<AudioClip>($"Audio/{baseName}_{index}");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
