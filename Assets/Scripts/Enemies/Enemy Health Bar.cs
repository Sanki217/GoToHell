using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enemy health bar — World Space Canvas with a UI Slider.
/// Builds itself entirely in code. No prefab work needed.
/// Appears only after the enemy first takes damage.
///
/// ADD THIS SCRIPT TO THE ENEMY ROOT. That's the only step.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("How far above the enemy pivot the bar floats (world units).")]
    public float yOffset = 0.8f;
    [Tooltip("Width of the bar in world units.")]
    public float barWidth = 1.2f;
    [Tooltip("Height of the bar in world units.")]
    public float barHeight = 0.12f;

    [Header("Colors")]
    public Color colorBackground = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    public Color colorFull = new Color(0.15f, 0.85f, 0.15f, 1f);
    public Color colorMid = new Color(0.95f, 0.75f, 0.05f, 1f);
    public Color colorLow = new Color(0.90f, 0.15f, 0.10f, 1f);

    [Tooltip("HP fraction below which bar turns yellow.")]
    public float midThreshold = 0.5f;
    [Tooltip("HP fraction below which bar turns red.")]
    public float lowThreshold = 0.25f;

    // ================================================================
    //  PRIVATE
    // ================================================================

    private bool visible = false;
    private int maxHP = 1;
    private int currentHP = 1;

    private GameObject canvasGO;
    private Slider slider;
    private Image fillImage;

    // ================================================================
    //  INIT
    // ================================================================

    private void Awake()
    {
        BuildBar();
        canvasGO.SetActive(false);
    }

    public void Initialize(int max, int current)
    {
        maxHP = Mathf.Max(1, max);
        currentHP = current;
        if (slider != null)
        {
            slider.maxValue = maxHP;
            slider.value = currentHP;
        }
    }

    // ================================================================
    //  PUBLIC API — called by Enemy.cs
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
    //  BUILD
    // ================================================================

    private void BuildBar()
    {
        // ── Canvas ──────────────────────────────────────────────────
        canvasGO = new GameObject("EnemyHPCanvas");
        canvasGO.transform.SetParent(transform, false);   // child of enemy

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Scale: 1 canvas unit = 1 pixel by default, so shrink to world size.
        // We want the canvas to be barWidth × barHeight in world units.
        // Canvas RectTransform defaults to 100×100 px, so scale = barWidth/100.
        float scale = barWidth / 100f;
        canvasGO.transform.localScale = Vector3.one * scale;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, barHeight / scale);

        // ── Background Image ─────────────────────────────────────────
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = colorBackground;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ── Slider ───────────────────────────────────────────────────
        GameObject sliderGO = new GameObject("HPSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = maxHP;
        slider.value = currentHP;
        slider.wholeNumbers = false;
        slider.interactable = false;   // enemies can't click their own bar

        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // ── Fill Area ────────────────────────────────────────────────
        // Unity Slider needs: sliderGO → Fill Area → Fill
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = colorFull;
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Wire fill to slider
        slider.fillRect = fillRect;

        // ── No Handle ────────────────────────────────────────────────
        // Do NOT create a handle. Slider works fine without one.
        // (The default Unity Slider prefab has a handle; we just don't add one here.)
        slider.handleRect = null;
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void Show()
    {
        visible = true;
        canvasGO.SetActive(true);
    }

    private void Refresh()
    {
        if (slider == null) return;
        slider.maxValue = maxHP;
        slider.value = currentHP;

        if (fillImage == null) return;
        float fraction = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        fillImage.color = fraction > midThreshold ? colorFull
                        : fraction > lowThreshold ? colorMid
                        : colorLow;
    }

    private void LateUpdate()
    {
        if (!visible || canvasGO == null) return;

        // Follow enemy in world space, fixed Y offset, same Z as enemy
        // (SpriteRenderer sorting handles draw order, not Z)
        Vector3 pos = transform.position;
        pos.y += yOffset;
        canvasGO.transform.position = pos;

        // Face the camera — required for World Space Canvas
        Camera cam = Camera.main;
        if (cam != null)
            canvasGO.transform.rotation = cam.transform.rotation;
    }
}