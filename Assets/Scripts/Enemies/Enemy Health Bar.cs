using UnityEngine;

/// <summary>
/// Enemy health bar using two SpriteRenderers (no Canvas, no UI).
/// Appears only after the enemy first takes damage.
///
/// ──────────────────────────────────────────────────────────────
///  SETUP — follow exactly:
/// ──────────────────────────────────────────────────────────────
///
///  1. Select your enemy prefab in the Project window and open it.
///
///  2. In the Hierarchy, right-click the enemy root → Create Empty.
///     Name it: HealthBar
///     Local Position: (0, 0.7, 0)   ← adjust Y later if needed
///     Local Scale:    (1, 1, 1)      ← leave at default
///     Leave it ACTIVE (checked).
///
///  3. Right-click HealthBar → Create Empty → name it: Background
///     Local Position: (0, 0, 0)
///     Local Scale:    (1.1, 0.15, 1)
///     Add Component → Sprite Renderer
///       Sprite: the built-in "UISprite" or "Square" — in the Project
///               window search bar type "Square" and look for the one
///               under Packages or built-in resources. Alternatively:
///               create a 4×4 white PNG, import it, set Texture Type
///               to "Sprite (2D and UI)" and use that.
///       Color:  (0.1, 0.1, 0.1, 0.85)   ← dark background
///       Sorting Layer: Default (or whichever layer your enemies use)
///       Order in Layer: 10
///
///  4. Right-click HealthBar → Create Empty → name it: Fill
///     Local Position: (0, 0, 0)
///     Local Scale:    (1.0, 0.11, 1)
///     Add Component → Sprite Renderer
///       Sprite: same sprite as Background
///       Color:  (0.15, 0.85, 0.15, 1.0)  ← green
///       Sorting Layer: same as Background
///       Order in Layer: 11
///
///  5. Select the ENEMY ROOT → Add Component → EnemyHealthBar
///     Drag "HealthBar"   into the Health Bar Root field
///     Drag "Fill"        into the Fill Renderer field
///
///  6. Leave all other fields at their defaults.
///     The script hides the bar at Start and shows it on first damage.
///
/// ──────────────────────────────────────────────────────────────
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References — MUST assign both")]
    [Tooltip("The 'HealthBar' empty GameObject child.")]
    public GameObject healthBarRoot;

    [Tooltip("The SpriteRenderer on the 'Fill' child.")]
    public SpriteRenderer fillRenderer;

    [Header("Position")]
    [Tooltip("How far above the enemy pivot the bar floats.")]
    public float yOffset = 0.7f;

    [Tooltip("Z position of the health bar in world space. " +
             "Must be CLOSER to the camera than your sprites. " +
             "If your camera looks toward +Z, use a MORE NEGATIVE value. " +
             "If camera looks toward -Z (typical Unity setup), use a MORE POSITIVE value. " +
             "Default -3 works for most Unity 2.5D setups where sprites are at Z=0 " +
             "and the camera is at Z=-10.")]
    public float zPosition = -1f;

    [Header("Colors")]
    public Color colorFull = new Color(0.15f, 0.85f, 0.15f, 1f);
    public Color colorMid = new Color(0.95f, 0.75f, 0.05f, 1f);
    public Color colorLow = new Color(0.90f, 0.15f, 0.10f, 1f);

    [Tooltip("Below this fraction the bar turns yellow.")]
    public float midThreshold = 0.5f;
    [Tooltip("Below this fraction the bar turns red.")]
    public float lowThreshold = 0.25f;

    // ================================================================
    //  STATE
    // ================================================================

    private bool visible = false;
    private int maxHP;
    private int currentHP;
    private float fillFullScaleX;

    // ================================================================
    //  INIT
    // ================================================================

    private void Awake()
    {
        if (fillRenderer != null)
            fillFullScaleX = fillRenderer.transform.localScale.x;

        if (healthBarRoot != null)
            healthBarRoot.SetActive(false);
    }

    public void Initialize(int max, int current)
    {
        maxHP = max;
        currentHP = current;
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void NotifyDamage(int newHP)
    {
        currentHP = Mathf.Max(0, newHP);
        if (!visible) Show();
        Refresh();
    }

    public void NotifyHeal(int newHP)
    {
        currentHP = newHP;
        if (visible) Refresh();
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void Show()
    {
        visible = true;
        if (healthBarRoot != null)
            healthBarRoot.SetActive(true);
    }

    private void Refresh()
    {
        if (maxHP <= 0 || fillRenderer == null) return;

        float fraction = Mathf.Clamp01((float)currentHP / maxHP);

        // Scale fill on X
        Vector3 s = fillRenderer.transform.localScale;
        s.x = fillFullScaleX * fraction;
        fillRenderer.transform.localScale = s;

        // Shift fill left so it shrinks from the right edge, not the centre
        Vector3 p = fillRenderer.transform.localPosition;
        p.x = fillFullScaleX * (fraction - 1f) * 0.5f;
        fillRenderer.transform.localPosition = p;

        // Color
        fillRenderer.color = fraction > midThreshold ? colorFull
                           : fraction > lowThreshold ? colorMid
                           : colorLow;
    }

    private void LateUpdate()
    {
        if (!visible || healthBarRoot == null) return;

        // Position: follow enemy in XY, but use a fixed Z so it's
        // always in front of the enemy sprite and not occluded by geometry.
        Vector3 pos = transform.position;
        pos.y += yOffset;
        pos.z = zPosition;
        healthBarRoot.transform.position = pos;

        // No rotation needed — SpriteRenderers always face the camera.
    }
}