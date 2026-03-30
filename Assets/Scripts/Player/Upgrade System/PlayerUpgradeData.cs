using UnityEngine;

/// <summary>
/// One ScriptableObject per upgrade.
/// Create via: Assets → Create → Upgrades → Player Upgrade Data
///
/// KEY CHANGES from previous version:
///   - statRanges[] lets you choose WHICH stats this upgrade can roll,
///     and set min/max values per rarity directly in the Inspector.
///   - behaviourSettings[] lets you configure upgrade-specific numbers
///     (e.g. explosion radius, immunity duration) without touching code.
///   - The pool reads statRanges to build stat bonus offers.
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Player Upgrade Data")]
public class PlayerUpgradeData : ScriptableObject
{
    // ================================================================
    //  IDENTITY
    // ================================================================

    [Header("Identity")]
    public string upgradeId;        // must match PlayerUpgrade.Id exactly
    public string displayName;
    public Sprite icon;
    public UpgradeCategory category;
    public bool isPureStatUpgrade = false;

    // ================================================================
    //  DESCRIPTIONS  (one per rarity)
    // ================================================================

    [Header("Card Descriptions — one per rarity")]
    [TextArea(2, 4)] public string descriptionCommon;
    [TextArea(2, 4)] public string descriptionRare;
    [TextArea(2, 4)] public string descriptionEpic;
    [TextArea(2, 4)] public string descriptionLegendary;

    [Header("Mythic")]
    public bool hasMythic = false;
    [TextArea(2, 4)] public string mythicDescription;

    // ================================================================
    //  STAT RANGES  (which stats this upgrade can roll + min/max per rarity)
    // ================================================================

    [Header("Stat Bonus Ranges — choose which stats this upgrade can attach")]
    [Tooltip("Each entry = one possible stat bonus. " +
             "The pool will roll a random value between min and max for the rolled rarity. " +
             "Leave empty for a pure behaviour upgrade with no stat bonuses.")]
    public StatRangeEntry[] statRanges;

    [Header("Stat Bonus Count per Rarity")]
    [Tooltip("How many stats are randomly selected from statRanges at each rarity.")]
    public int statCountCommon = 0;
    public int statCountRare = 1;
    public int statCountEpic = 2;
    public int statCountLegendary = 3;

    // ================================================================
    //  BEHAVIOUR SETTINGS  (upgrade-specific tuning, read by upgrade code)
    // ================================================================

    [Header("Behaviour Settings — upgrade-specific values")]
    [Tooltip("Key-value pairs for behaviour tuning. " +
             "Each upgrade class reads the keys it needs via GetSetting(key, default). " +
             "Common keys: 'damage', 'radius', 'duration', 'cooldown', 'bonusPercent', " +
             "'damagePerLevel', 'radiusPerLevel', 'durationPerLevel', 'cooldownReduction'")]
    public BehaviourSetting[] behaviourSettings;

    // ================================================================
    //  HELPERS
    // ================================================================

    public string GetDescription(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => descriptionCommon,
        UpgradeRarity.Rare => descriptionRare,
        UpgradeRarity.Epic => descriptionEpic,
        UpgradeRarity.Legendary => descriptionLegendary,
        _ => descriptionCommon
    };

    public int GetStatCount(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => statCountCommon,
        UpgradeRarity.Rare => statCountRare,
        UpgradeRarity.Epic => statCountEpic,
        UpgradeRarity.Legendary => statCountLegendary,
        _ => statCountCommon
    };

    /// <summary>
    /// Read a behaviour setting by key. Returns defaultValue if not found.
    /// </summary>
    public float GetSetting(string key, float defaultValue = 0f)
    {
        if (behaviourSettings == null) return defaultValue;
        foreach (var s in behaviourSettings)
            if (s.key == key) return s.value;
        return defaultValue;
    }
}

// ================================================================
//  SUPPORTING TYPES
// ================================================================

/// <summary>
/// One possible stat bonus for an upgrade, with min/max ranges per rarity.
/// The pool will roll a random value within [minCommon, maxCommon] at Common,
/// [minRare, maxRare] at Rare, etc.
/// </summary>
[System.Serializable]
public class StatRangeEntry
{
    public StatType statType;

    [Header("Value Range per Rarity")]
    public float minCommon; public float maxCommon;
    public float minRare; public float maxRare;
    public float minEpic; public float maxEpic;
    public float minLegendary; public float maxLegendary;

    public (float min, float max) GetRange(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => (minCommon, maxCommon),
        UpgradeRarity.Rare => (minRare, maxRare),
        UpgradeRarity.Epic => (minEpic, maxEpic),
        UpgradeRarity.Legendary => (minLegendary, maxLegendary),
        _ => (minCommon, maxCommon)
    };

    public float Roll(UpgradeRarity rarity)
    {
        var (min, max) = GetRange(rarity);
        return Random.Range(min, max);
    }
}

/// <summary>A named float value for upgrade-specific behaviour tuning.</summary>
[System.Serializable]
public class BehaviourSetting
{
    public string key;
    public float value;
}

public enum UpgradeRarity { Common, Rare, Epic, Legendary }
public enum UpgradeCategory { Bow, Dash, WallSlide, Hover, Status, Passive, Misc }