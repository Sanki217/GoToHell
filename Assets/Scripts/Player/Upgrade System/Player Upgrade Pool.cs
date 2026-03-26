using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that holds every upgrade in the game.
/// Create ONE of these: Assets → Create → Upgrades → Upgrade Pool
/// Drag all your PlayerUpgradeData assets into the "upgrades" list.
///
/// Handles building random offers for level-up, chests, and merchant.
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrades in the game — drag every asset here")]
    public List<PlayerUpgradeData> upgrades = new List<PlayerUpgradeData>();

    // All 21 stat types in a flat array for random selection
    private static readonly StatType[] AllStats = (StatType[])System.Enum.GetValues(typeof(StatType));

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Build 3 offers for the level-up screen.
    /// Each offer is a different upgrade (no duplicates) at a rolled rarity.
    /// </summary>
    public List<UpgradeOffer> RollLevelUpOffers(int layer, float luck, int count = 3)
    {
        var offers = new List<UpgradeOffer>();
        var usedIds = new HashSet<string>();
        var pool = new List<PlayerUpgradeData>(upgrades);
        int attempts = 0;

        while (offers.Count < count && pool.Count > 0 && attempts < 100)
        {
            attempts++;
            int idx = Random.Range(0, pool.Count);
            PlayerUpgradeData data = pool[idx];
            pool.RemoveAt(idx);

            if (usedIds.Contains(data.upgradeId)) continue;

            UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
            offers.Add(BuildOffer(data, rarity));
            usedIds.Add(data.upgradeId);
        }

        return offers;
    }

    /// <summary>
    /// Roll a single upgrade for a chest.
    /// Rarity is determined first, then a random upgrade from the pool.
    /// </summary>
    public UpgradeOffer RollChestOffer(int layer, float luck)
    {
        if (upgrades.Count == 0) return null;
        UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
        PlayerUpgradeData data = upgrades[Random.Range(0, upgrades.Count)];
        return BuildOffer(data, rarity);
    }

    /// <summary>
    /// Roll a set of offers for the merchant (typically 3-4).
    /// Merchant tends toward slightly higher rarities (layer + 2 bonus).
    /// </summary>
    public List<UpgradeOffer> RollMerchantOffers(int layer, float luck, int count = 3)
    {
        return RollLevelUpOffers(layer + 2, luck, count);
    }

    /// <summary>
    /// Build a complete UpgradeOffer from data + rarity,
    /// rolling random stat bonuses as needed.
    /// </summary>
    public UpgradeOffer BuildOffer(PlayerUpgradeData data, UpgradeRarity rarity)
    {
        var offer = new UpgradeOffer();
        offer.data = data;
        offer.rarity = rarity;
        offer.statBonuses = new List<UpgradeStatBonus>();

        float multiplier = data.GetStatMultiplier(rarity);

        if (data.isPureStatUpgrade)
        {
            // Pure stat upgrade: apply fixed bonuses scaled by rarity multiplier
            int count = data.GetStatBonusCount(rarity);
            if (data.fixedStatBonuses != null)
            {
                for (int i = 0; i < Mathf.Min(count, data.fixedStatBonuses.Length); i++)
                {
                    offer.statBonuses.Add(new UpgradeStatBonus
                    {
                        statType = data.fixedStatBonuses[i].statType,
                        value = data.fixedStatBonuses[i].value * multiplier
                    });
                }
            }
        }
        else
        {
            // Behaviour upgrade: roll random stat bonuses
            int bonusCount = data.GetStatBonusCount(rarity);
            var usedStats = new HashSet<StatType>();

            for (int i = 0; i < bonusCount; i++)
            {
                StatType rolled = RollUniqueStatType(usedStats);
                usedStats.Add(rolled);
                offer.statBonuses.Add(new UpgradeStatBonus
                {
                    statType = rolled,
                    value = GetBaseStatValue(rolled) * multiplier
                });
            }
        }

        return offer;
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private StatType RollUniqueStatType(HashSet<StatType> used)
    {
        int attempts = 0;
        while (attempts < 50)
        {
            StatType t = AllStats[Random.Range(0, AllStats.Length)];
            if (!used.Contains(t)) return t;
            attempts++;
        }
        return AllStats[0];
    }

    /// <summary>
    /// Base value per stat at Common rarity (1.0× multiplier).
    /// Higher rarities multiply these by their multiplier.
    /// </summary>
    private float GetBaseStatValue(StatType t) => t switch
    {
        StatType.MaxHP => 10f,
        StatType.ArrowDamage => 0.5f,
        StatType.DashDamage => 0.5f,
        StatType.CritChance => 0.05f,   // +5%
        StatType.CritMultiplier => 0.10f,   // +10%
        StatType.KnockbackForce => 1f,
        StatType.BurnStrength => 0.20f,   // +20%
        StatType.FreezeStrength => 0.20f,
        StatType.HolyStrength => 0.20f,
        StatType.ShockStrength => 0.20f,
        StatType.MoveSpeed => 0.5f,
        StatType.DashDistance => 1f,
        StatType.DashCost => -3f,      // decrease
        StatType.DashInvincibility => 0.1f,
        StatType.MaxEnergy => 10f,
        StatType.HoverDrainRate => -1f,      // decrease
        StatType.ChargeDrainRate => -0.5f,    // decrease
        StatType.ChargeDuration => -0.1f,    // decrease = faster charge
        StatType.LifeSteal => 0.05f,   // +5%
        StatType.LootRange => 0.5f,
        StatType.Luck => 1f,
        _ => 1f
    };
}

/// <summary>
/// A fully resolved upgrade offer — data + rarity + rolled stat bonuses.
/// This is what the UI displays on a card and what gets applied on pick.
/// </summary>
[System.Serializable]
public class UpgradeOffer
{
    public PlayerUpgradeData data;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
}