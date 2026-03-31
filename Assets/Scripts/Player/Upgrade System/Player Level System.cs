using UnityEngine;

/// <summary>
/// Manages player level and XP progression.
/// Attach to the Player root alongside PlayerStats and PlayerInventory.
///
/// XP Sources:
///   1. Souls collected  — 1 XP per soul × xpMultiplier
///   2. Falling downward — 1 XP per FallXpUnits units × xpMultiplier
///
/// Per-level XP requirements are editable in the Inspector via xpPerLevel[].
/// If the array runs out, the formula 100 × 1.3^(level-1) is used as fallback.
///
/// xpMultiplier is a live stat (default 1.0) improvable via upgrades.
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    // ================================================================
    //  CONSTANTS
    // ================================================================

    public const int MaxLevel = 20;
    public const float FallXpUnits = 100f;

    // ================================================================
    //  INSPECTOR — editable XP table
    // ================================================================

    [Header("XP Required Per Level (index 0 = level 1→2, index 1 = level 2→3, ...)")]
    [Tooltip("Set each entry to the XP needed to reach the next level. " +
             "If fewer than 19 entries are provided, the formula 100×1.3^(N-1) fills the rest.")]
    public float[] xpPerLevel = new float[]
    {
         100,   // 1 → 2
         130,   // 2 → 3
         169,   // 3 → 4
         220,   // 4 → 5
         286,   // 5 → 6
         371,   // 6 → 7
         483,   // 7 → 8
         627,   // 8 → 9
         815,   // 9 → 10
        1060,   // 10 → 11
        1378,   // 11 → 12
        1791,   // 12 → 13
        2328,   // 13 → 14
        // 14–19 filled by formula if left empty
    };

    [Header("Current Layer (set by level manager when transitioning)")]
    public int currentLayer = 1;

    // ================================================================
    //  LIVE STATS — visible in Inspector, readable by TAB panel
    // ================================================================

    [Header("--- LEVEL STATS (live, read-only) ---")]
    [Tooltip("Current player level.")]
    public int currentLevel = 1;

    [Tooltip("XP accumulated toward the next level.")]
    public float currentXP = 0f;

    [Tooltip("XP required to reach the next level.")]
    public float xpToNextLevel = 100f;

    [Tooltip("XP multiplier — 1.0 = normal, 1.5 = +50% XP from all sources. Improved by upgrades.")]
    public float xpMultiplier = 1f;

    // ================================================================
    //  PUBLIC READ — used by XpBarUI and StatsUI
    // ================================================================

    public int CurrentLevel => currentLevel;
    public float CurrentXP => currentXP;
    public float XPToNextLevel => xpToNextLevel;
    public float XPFraction => xpToNextLevel > 0f ? Mathf.Clamp01(currentXP / xpToNextLevel) : 0f;

    // ================================================================
    //  PRIVATE
    // ================================================================

    private PlayerInventory inventory;
    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;
    private int lastSoulCount = 0;
    private float previousY;
    private float fallXpAccumulator = 0f;

    // ================================================================
    //  INIT
    // ================================================================

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        playerStats = GetComponent<PlayerStats>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();

        xpToNextLevel = GetXpRequired(1);
    }

    private void Start()
    {
        previousY = transform.position.y;

        if (inventory != null)
            inventory.OnSoulsChanged += OnSoulsChanged;
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnSoulsChanged -= OnSoulsChanged;
    }

    // ================================================================
    //  UPDATE — downward distance XP
    // ================================================================

    private void Update()
    {
        float currentY = transform.position.y;
        float dy = currentY - previousY;

        if (dy < 0f)
        {
            fallXpAccumulator += Mathf.Abs(dy);
            while (fallXpAccumulator >= FallXpUnits)
            {
                fallXpAccumulator -= FallXpUnits;
                AddXP(1f);
            }
        }

        previousY = currentY;
    }

    // ================================================================
    //  SOUL XP
    // ================================================================

    private void OnSoulsChanged(int totalSouls)
    {
        int gained = totalSouls - lastSoulCount;
        lastSoulCount = totalSouls;
        if (gained > 0)
            AddXP(gained);
    }

    // ================================================================
    //  XP LOGIC
    // ================================================================

    /// <summary>
    /// Add XP, applying the xpMultiplier.
    /// Triggers level-up automatically if threshold is crossed.
    /// </summary>
    public void AddXP(float baseAmount)
    {
        if (currentLevel >= MaxLevel) return;

        float amount = baseAmount * xpMultiplier;
        currentXP += amount;

        while (currentXP >= xpToNextLevel && currentLevel < MaxLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = GetXpRequired(currentLevel);
            OnLevelUp();
        }

        if (currentLevel >= MaxLevel)
            currentXP = 0f;
    }

    /// <summary>
    /// Add XP directly, bypassing xpMultiplier.
    /// Used by SoulBonus to avoid double-multiplying bonus souls.
    /// </summary>
    public void AddXPDirect(float amount)
    {
        if (currentLevel >= MaxLevel) return;

        currentXP += amount;

        while (currentXP >= xpToNextLevel && currentLevel < MaxLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = GetXpRequired(currentLevel);
            OnLevelUp();
        }

        if (currentLevel >= MaxLevel)
            currentXP = 0f;
    }

    private void OnLevelUp()
    {
        Debug.Log($"[LevelSystem] Level up! Now level {currentLevel}");

        playerStats?.RecordPlayerLevelUp(currentLevel);
        upgradeManager?.PlayerLevelUp(currentLevel);

        if (LevelUpUI.Instance != null)
            LevelUpUI.Instance.Show(currentLayer);
        else
            Debug.LogWarning("[LevelSystem] LevelUpUI.Instance not found.");
    }

    // ================================================================
    //  XP TABLE
    // ================================================================

    /// <summary>
    /// XP required to level up FROM level N.
    /// Reads from xpPerLevel[] if available, falls back to formula.
    /// </summary>
    public float GetXpRequired(int level)
    {
        int idx = level - 1; // level 1 = index 0
        if (xpPerLevel != null && idx >= 0 && idx < xpPerLevel.Length && xpPerLevel[idx] > 0)
            return xpPerLevel[idx];

        // Formula fallback: 100 × 1.3^(level-1)
        return Mathf.Floor(100f * Mathf.Pow(1.3f, level - 1));
    }
}