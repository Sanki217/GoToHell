using UnityEngine;

/// <summary>
/// One ScriptableObject per upgrade.
/// Stat bonuses are now rolled from a GLOBAL pool (defined on PlayerUpgradePool),
/// not per-upgrade ranges. This ScriptableObject only needs:
///   - Identity and descriptions
///   - How many stats to attach per rarity
///   - Behaviour settings for upgrade-specific tuning
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Player Upgrade Data")]
public class PlayerUpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string upgradeId;
    public string displayName;
    public Sprite icon;
    public UpgradeCategory category;

    [Header("Descriptions — one per level (5 levels)")]
    [Tooltip("Index 0 = level 1, index 4 = level 5")]
    [TextArea(2, 4)] public string descLevel1;
    [TextArea(2, 4)] public string descLevel2;
    [TextArea(2, 4)] public string descLevel3;
    [TextArea(2, 4)] public string descLevel4;
    [TextArea(2, 4)] public string descLevel5;

    [Header("Stat Bonus Count per Rarity")]
    [Tooltip("How many random primary stat bonuses are attached at each rarity.")]
    public int statCountCommon = 1;
    public int statCountRare = 2;
    public int statCountEpic = 3;
    public int statCountLegendary = 4;

    [Header("Behaviour Settings — upgrade-specific tuning")]
    [Tooltip("Key/value pairs read by the upgrade C# class. " +
             "Examples: 'damage', 'radius', 'duration', 'cooldown', 'bonusPercent'")]
    public BehaviourSetting[] behaviourSettings;

    [Header("Curse")]
    [Tooltip("Curses have no levels — they apply once and cannot be levelled up.")]
    public bool isCurse = false;

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>Overload for callers that have a rarity but no level yet (chests, merchant).</summary>
    public string GetDescription(UpgradeRarity rarity)
    {
        int level = rarity switch
        {
            UpgradeRarity.Common => 1,
            UpgradeRarity.Rare => 2,
            UpgradeRarity.Epic => 3,
            UpgradeRarity.Legendary => 4,
            _ => 1,
        };
        return GetDescription(level);
    }

    public string GetDescription(int level)
    {
        return level switch
        {
            1 => descLevel1,
            2 => descLevel2,
            3 => descLevel3,
            4 => descLevel4,
            5 => descLevel5,
            _ => descLevel1,
        };
    }

    public int GetStatCount(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => statCountCommon,
        UpgradeRarity.Rare => statCountRare,
        UpgradeRarity.Epic => statCountEpic,
        UpgradeRarity.Legendary => statCountLegendary,
        _ => statCountCommon
    };

    public float GetSetting(string key, float defaultValue = 0f)
    {
        if (behaviourSettings == null) return defaultValue;
        foreach (var s in behaviourSettings)
            if (s.key == key) return s.value;
        return defaultValue;
    }
}

[System.Serializable]
public class BehaviourSetting
{
    public string key;
    public float value;
}

public enum UpgradeRarity { Common, Rare, Epic, Legendary }
public enum UpgradeCategory { Arrow, Dash, Passive, Conditional, Curse, Misc }