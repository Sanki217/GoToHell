using UnityEngine;

/// <summary>
/// One ScriptableObject asset per upgrade.
/// Create via: Assets → Create → Upgrades → Player Upgrade Data
///
/// The rarity table has 4 rows (Common/Rare/Epic/Legendary).
/// Each row defines:
///   - The behaviour description at that rarity
///   - How many random stat bonuses are attached
///   - The fixed stat bonus values if this is a pure stat upgrade
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Player Upgrade Data")]
public class PlayerUpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string upgradeId;        // unique key, must match PlayerUpgrade.Id
    public string displayName;
    public Sprite icon;
    public UpgradeCategory category;

    [Header("Behaviour Description (shown on card)")]
    [TextArea(2, 4)] public string descriptionCommon;
    [TextArea(2, 4)] public string descriptionRare;
    [TextArea(2, 4)] public string descriptionEpic;
    [TextArea(2, 4)] public string descriptionLegendary;

    [Header("Mythic")]
    public bool hasMythic = false;
    [TextArea(2, 4)] public string mythicDescription;

    [Header("Rarity — Random Stat Bonus Count")]
    [Tooltip("How many random stat bonuses are rolled at each rarity. " +
             "Behaviour-only = 0 bonus stats at Common, 1 at Rare, 2 at Epic, 3 at Legendary. " +
             "Stat-only = 2 at Common, 3 at Rare, 4 at Epic, 5 at Legendary.")]
    public int statBonusCountCommon = 0;
    public int statBonusCountRare = 1;
    public int statBonusCountEpic = 2;
    public int statBonusCountLegendary = 3;

    [Header("Rarity — Stat Bonus Multiplier")]
    [Tooltip("Multiplier applied to all random stat bonus values at each rarity.")]
    public float statMultiplierCommon = 1.0f;
    public float statMultiplierRare = 1.2f;
    public float statMultiplierEpic = 1.4f;
    public float statMultiplierLegendary = 1.8f;

    [Header("Is Pure Stat Upgrade?")]
    [Tooltip("If true, this upgrade has no behaviour — it only applies stat bonuses. " +
             "Used for the 18 stat-only pool entries.")]
    public bool isPureStatUpgrade = false;

    [Header("Fixed Stat Bonuses (for pure stat upgrades)")]
    [Tooltip("For pure stat upgrades, define the base bonus at Common rarity. " +
             "Higher rarities multiply this by statMultiplier.")]
    public UpgradeStatBonus[] fixedStatBonuses;

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

    public int GetStatBonusCount(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => statBonusCountCommon,
        UpgradeRarity.Rare => statBonusCountRare,
        UpgradeRarity.Epic => statBonusCountEpic,
        UpgradeRarity.Legendary => statBonusCountLegendary,
        _ => statBonusCountCommon
    };

    public float GetStatMultiplier(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => statMultiplierCommon,
        UpgradeRarity.Rare => statMultiplierRare,
        UpgradeRarity.Epic => statMultiplierEpic,
        UpgradeRarity.Legendary => statMultiplierLegendary,
        _ => statMultiplierCommon
    };
}

public enum UpgradeRarity { Common, Rare, Epic, Legendary }

public enum UpgradeCategory { Bow, Dash, WallSlide, Hover, Status, Passive, Misc }