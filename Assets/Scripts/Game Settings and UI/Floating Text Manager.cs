using UnityEngine;

/// <summary>
/// Singleton manager for floating damage numbers.
/// Place this on an empty GameObject in the scene.
///
/// Usage from any script:
///   FloatingTextManager.Show(damage, position, FloatingTextManager.Type.Normal);
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    // ================================================================
    //  SINGLETON
    // ================================================================

    public static FloatingTextManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ================================================================
    //  HIT TYPE DEFINITIONS
    // ================================================================

    public enum HitType
    {
        Normal,        // white,      normal size
        Critical,      // gold,       large
        BurnTick,      // orange,     small
        HolyDetonate,  // white/gold, large
        ShockConsume,  // yellow,     medium
        FrozenHit,     // light blue, normal
    }

    // ================================================================
    //  INSPECTOR
    // ================================================================

    [Header("Prefab")]
    public GameObject floatingTextPrefab;  // assign in Inspector — see setup instructions

    [Header("Settings")]
    public bool showDamageNumbers = true;  // toggled from Settings menu

    [Header("Colors")]
    public Color colorNormal      = Color.white;
    public Color colorCritical    = new Color(1f, 0.85f, 0f);    // gold
    public Color colorBurn        = new Color(1f, 0.4f, 0f);     // orange
    public Color colorHoly        = new Color(1f, 0.95f, 0.7f);  // pale gold
    public Color colorShock       = new Color(1f, 0.95f, 0f);    // yellow
    public Color colorFrozen      = new Color(0.5f, 0.85f, 1f);  // light blue

    [Header("Font Sizes")]
    public float sizeNormal    = 5f;
    public float sizeCritical  = 8f;
    public float sizeBurn      = 3.5f;
    public float sizeHoly      = 8f;
    public float sizeShock     = 5.5f;
    public float sizeFrozen    = 5f;

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Spawn a floating damage number at a world position.
    /// </summary>
    public static void Show(float damage, Vector3 worldPosition, HitType type = HitType.Normal)
    {
        if (Instance == null || !Instance.showDamageNumbers) return;
        Instance.Spawn(damage, worldPosition, type);
    }

    /// <summary>
    /// Spawn a floating text with a custom string (e.g. "MISS", "IMMUNE").
    /// </summary>
    public static void ShowText(string text, Vector3 worldPosition, HitType type = HitType.Normal)
    {
        if (Instance == null || !Instance.showDamageNumbers) return;
        Instance.SpawnText(text, worldPosition, type);
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void Spawn(float damage, Vector3 position, HitType type)
    {
        string text = Mathf.CeilToInt(damage).ToString();
        SpawnText(text, position, type);
    }

    private void SpawnText(string text, Vector3 position, HitType type)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("[FloatingTextManager] No prefab assigned!");
            return;
        }

        // Slight random horizontal offset so numbers don't stack perfectly
        Vector3 spawnPos = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, 0f);

        GameObject go = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        FloatingText ft = go.GetComponent<FloatingText>();

        if (ft == null)
        {
            Debug.LogWarning("[FloatingTextManager] Prefab has no FloatingText component!");
            return;
        }

        // Prefix crits with "!"
        string displayText = (type == HitType.Critical) ? "! " + text + " !" : text;

        ft.Init(displayText, GetColor(type), GetSize(type));
    }

    private Color GetColor(HitType type) => type switch
    {
        HitType.Critical    => colorCritical,
        HitType.BurnTick    => colorBurn,
        HitType.HolyDetonate=> colorHoly,
        HitType.ShockConsume=> colorShock,
        HitType.FrozenHit   => colorFrozen,
        _                   => colorNormal
    };

    private float GetSize(HitType type) => type switch
    {
        HitType.Critical    => sizeCritical,
        HitType.BurnTick    => sizeBurn,
        HitType.HolyDetonate=> sizeHoly,
        HitType.ShockConsume=> sizeShock,
        _                   => sizeNormal
    };
}