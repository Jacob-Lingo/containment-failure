using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.25f;
    private void Start() => Destroy(gameObject, lifetime);
}