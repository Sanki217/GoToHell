using UnityEngine;

/// <summary>
/// Handles rarity rolling for all upgrade sources.
///
/// Base weights:  Common 55%  Rare 25%  Epic 15%  Legendary 5%
/// Layer bonus:   Each layer past 1 shifts 0.5% from Common to upper tiers
/// Luck bonus:    Each luck point shifts 2% from Common to upper tiers
///                distributed: 50% Rare, 30% Epic, 20% Legendary
/// Common floor:  Never drops below 20%
///
/// Chests use RollWithLuckOnly() — no layer influence, only luck.
/// </summary>
public static class UpgradeRarityRoller
{
    private const float BaseCommon = 0.55f;
    private const float BaseRare = 0.25f;
    private const float BaseEpic = 0.15f;
    private const float BaseLegendary = 0.05f;

    private const float ShiftPerLayer = 0.005f;
    private const float ShiftPerLuck = 0.02f;
    private const float CommonFloor = 0.20f;

    private const float RareProportion = 0.50f;
    private const float EpicProportion = 0.30f;
    private const float LegendaryProportion = 0.20f;

    /// <summary>
    /// Full roll — uses both layer depth and luck.
    /// Used for level-up upgrade picks.
    /// </summary>
    public static UpgradeRarity Roll(int layer, float luck)
    {
        return RollInternal(layer, luck);
    }

    /// <summary>
    /// Chest roll — luck only, no layer influence.
    /// All chests are the same; rarity comes from luck stat alone.
    /// </summary>
    public static UpgradeRarity RollWithLuckOnly(float luck)
    {
        return RollInternal(1, luck); // layer=1 means zero layer shift
    }

    private static UpgradeRarity RollInternal(int layer, float luck)
    {
        float common = BaseCommon;
        float rare = BaseRare;
        float epic = BaseEpic;
        float legendary = BaseLegendary;

        float totalShift = ShiftPerLayer * Mathf.Max(0, layer - 1)
                          + ShiftPerLuck * Mathf.Max(0f, luck);
        float actualShift = Mathf.Min(totalShift, common - CommonFloor);

        common -= actualShift;
        rare += actualShift * RareProportion;
        epic += actualShift * EpicProportion;
        legendary += actualShift * LegendaryProportion;

        float r = Random.value;
        if (r < legendary) return UpgradeRarity.Legendary;
        if (r < legendary + epic) return UpgradeRarity.Epic;
        if (r < legendary + epic + rare) return UpgradeRarity.Rare;
        return UpgradeRarity.Common;
    }

    public static Color GetRarityColor(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => new Color(0.80f, 0.80f, 0.80f),
        UpgradeRarity.Rare => new Color(0.20f, 0.50f, 1.00f),
        UpgradeRarity.Epic => new Color(0.60f, 0.15f, 0.90f),
        UpgradeRarity.Legendary => new Color(1.00f, 0.70f, 0.10f),
        _ => Color.white
    };

    public static string GetRarityName(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => "Common",
        UpgradeRarity.Rare => "Rare",
        UpgradeRarity.Epic => "Epic",
        UpgradeRarity.Legendary => "Legendary",
        _ => "Common"
    };
}