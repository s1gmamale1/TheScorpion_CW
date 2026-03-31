using UnityEngine;
using TMPro;

/// <summary>
/// DamagePopup — floating damage numbers that rise and fade.
/// Create a prefab: Canvas (World Space) → TextMeshProUGUI child.
/// Call DamagePopup.Create(position, damage) from anywhere.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float riseSpeed = 2f;
    public float lifetime = 0.8f;
    public float fadeSpeed = 2f;

    private float timer;
    private Color startColor;

    // Prefab reference — assign in a manager or use Resources.Load
    private static GameObject prefab;

    public static void Create(Vector3 position, float damage, bool isCritical = false)
    {
        if (prefab == null)
            prefab = Resources.Load<GameObject>("DamagePopup");

        if (prefab == null)
        {
            Debug.LogWarning("DamagePopup prefab not found in Resources folder!");
            return;
        }

        var go = Instantiate(prefab, position + Vector3.up * 1.5f, Quaternion.identity);
        var popup = go.GetComponent<DamagePopup>();

        if (popup != null && popup.textMesh != null)
        {
            popup.textMesh.text = Mathf.RoundToInt(damage).ToString();
            popup.textMesh.color = isCritical ? Color.yellow : Color.white;
            popup.textMesh.fontSize = isCritical ? 8f : 5f;
        }
    }

    void Start()
    {
        timer = lifetime;
        if (textMesh != null)
            startColor = textMesh.color;
    }

    void Update()
    {
        // Rise
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Billboard — face camera
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        // Fade
        timer -= Time.deltaTime;
        if (textMesh != null)
        {
            float alpha = Mathf.Lerp(0f, startColor.a, timer / lifetime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
