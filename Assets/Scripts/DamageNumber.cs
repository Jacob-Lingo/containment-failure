using UnityEngine;

/// Floating combat-text popup built entirely at runtime via TextMesh, so it
/// needs no font/prefab reference — callers just do DamageNumber.Spawn(pos, amount).
public class DamageNumber : MonoBehaviour
{
    private const float RiseSpeed = 1.2f;
    private const float Lifetime = 0.6f;

    private TextMesh label;
    private Color baseColor;
    private float timer;

    public static void Spawn(Vector3 worldPos, int amount)
    {
        GameObject go = new GameObject("DamageNumber");
        go.transform.position = worldPos + Vector3.up * 0.3f;

        var tm = go.AddComponent<TextMesh>();
        tm.text = amount.ToString();
        tm.characterSize = 0.12f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.yellow;

        var mr = go.GetComponent<MeshRenderer>();
        mr.sortingOrder = 20;

        var popup = go.AddComponent<DamageNumber>();
        popup.label = tm;
        popup.baseColor = tm.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * RiseSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, timer / Lifetime);
        label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        if (timer >= Lifetime) Destroy(gameObject);
    }
}
