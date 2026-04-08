using UnityEngine;

// ================================================================
//  ENUMS
// ================================================================

public enum UpgradeRarity { Common, Rare, Epic, Legendary }
public enum UpgradeCategory { Arrow, Dash, Passive, Conditional, Curse, Misc }

/// <summary>
/// Handles rarity rolling for all upgrade sources.
///
/// LEVEL-UP RARITY:
///   All three cards share the same rarity (rolled once per level-up).
///   Base chances: Common 50%  Rare 30%  Epic 15%  Legendary 5%
///
/// PITY SYSTEM:
///   Every non-legendary roll increases legendary chance by +5% (additive).
///   Resets to 0 when a legendary is rolled.
///   Tracked in a static field so it persists across level-ups within one run.
///
/// LUCK SCALING:
///   Each point of Luck shifts 2% away from Common toward rarer tiers.
///   Distribution of that shift: 50% Rare, 30% Epic, 20% Legendary.
///   Common floor is 10%.
///
/// CHESTS: use RollWithLuckOnly() — no pity, luck only.
/// </summary>
public static class UpgradeRarityRoller
{
    // ================================================================
    //  BASE CHANCES
    // ================================================================

    private const float BaseCommon = 0.50f;
    private const float BaseRare = 0.30f;
    private const float BaseEpic = 0.15f;
    private const float BaseLegendary = 0.05f;

    private const float ShiftPerLuck = 0.02f;
    private const float CommonFloor = 0.10f;

    private const float RareProportion = 0.50f;
    private const float EpicProportion = 0.30f;
    private const float LegendaryProportion = 0.20f;

    // ================================================================
    //  PITY SYSTEM — static, resets on legendary roll
    // ================================================================

    private static float pityBonus = 0f;          // accumulated legendary bonus
    private const float PityPerNonLegendary = 0.05f;

    /// <summary>Reset pity at run start if needed (call from game reset logic).</summary>
    public static void ResetPity() => pityBonus = 0f;

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Rolls ONE rarity to be shared by all cards in a level-up offer.
    /// Applies luck and pity. Use this once per level-up event.
    /// </summary>
    public static UpgradeRarity RollLevelUpRarity(float luck)
    {
        UpgradeRarity result = RollInternal(luck, usePity: true);

        if (result == UpgradeRarity.Legendary)
            pityBonus = 0f;
        else
            pityBonus += PityPerNonLegendary;

        return result;
    }

    /// <summary>Chest roll — luck only, no pity influence.</summary>
    public static UpgradeRarity RollWithLuckOnly(float luck)
        => RollInternal(luck, usePity: false);

    // ================================================================
    //  INTERNAL
    // ================================================================

    private static UpgradeRarity RollInternal(float luck, bool usePity)
    {
        float common = BaseCommon;
        float rare = BaseRare;
        float epic = BaseEpic;
        float legendary = BaseLegendary;

        // Luck shift
        float luckShift = ShiftPerLuck * Mathf.Max(0f, luck);
        float actualShift = Mathf.Min(luckShift, common - CommonFloor);
        common -= actualShift;
        rare += actualShift * RareProportion;
        epic += actualShift * EpicProportion;
        legendary += actualShift * LegendaryProportion;

        // Pity bonus adds directly to legendary, subtracts from common
        if (usePity && pityBonus > 0f)
        {
            float pityApplied = Mathf.Min(pityBonus, common - CommonFloor);
            common -= pityApplied;
            legendary += pityApplied;
        }

        float r = Random.value;
        if (r < legendary) return UpgradeRarity.Legendary;
        if (r < legendary + epic) return UpgradeRarity.Epic;
        if (r < legendary + epic + rare) return UpgradeRarity.Rare;
        return UpgradeRarity.Common;
    }

    // ================================================================
    //  COLOUR / NAME HELPERS
    // ================================================================

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