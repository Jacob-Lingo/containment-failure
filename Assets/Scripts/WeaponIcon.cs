using UnityEngine;

/// Loads a weapon icon sprite onto this object's SpriteRenderer at runtime,
/// by Resources path rather than a hard-wired asset reference — lets the
/// same prefab structure serve both guard variants by swapping one string.
[RequireComponent(typeof(SpriteRenderer))]
public class WeaponIcon : MonoBehaviour
{
    [SerializeField] private string resourceName = "gun_icon";

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>($"Combat/{resourceName}");
    }
}
