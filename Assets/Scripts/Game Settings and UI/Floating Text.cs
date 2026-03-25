using UnityEngine;
using TMPro;

/// <summary>
/// A single floating damage number. Spawned by FloatingTextManager.
/// Rises upward, fades out, then destroys itself.
/// Attach this to a prefab that has a TMP_Text component.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class FloatingText : MonoBehaviour
{
    [Header("Animation")]
    public float riseSpeed = 1.5f;   // units per second upward
    public float lifetime = 0.8f;   // seconds before gone
    public float fadeStart = 0.4f;   // seconds before fade begins

    private TMP_Text label;
    private float timer;
    private Color startColor;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// Call this immediately after spawning to set up the text.
    /// </summary>
    public void Init(string text, Color color, float fontSize = 5f)
    {
        label.text = text;
        label.color = color;
        label.fontSize = fontSize;
        startColor = color;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Rise upward in world space
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Fade out in the second half of lifetime
        if (timer >= fadeStart)
        {
            float t = (timer - fadeStart) / (lifetime - fadeStart);
            Color c = label.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            label.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}