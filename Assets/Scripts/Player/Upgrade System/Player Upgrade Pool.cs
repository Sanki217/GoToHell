using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrades — drag every PlayerUpgradeData asset here")]
    public List<PlayerUpgradeData> upgrades = new List<PlayerUpgradeData>();

    // ================================================================
    //  STAT CAPS
    //
    //  Values here are in INTERNAL units — same scale as PlayerStats fields.
    //    Percent stats: 1.0 = 100%, 0.75 = 75%
    //    Flat stats:    same number as PlayerStats field
    //
    //  When a stat reaches its cap, it is filtered out of all future rolls.
    //  Edit these directly on your UpgradePool ScriptableObject in the Inspector.
    // ================================================================

    [Header("Stat Caps — values in internal units (percent stats: 1.0 = 100%)")]
    [Tooltip("Stats at or above their cap are excluded from upgrade rolls. " +
             "Add entries for any stat you want to limit.")]
    public List<StatCapEntry> statCaps = new List<StatCapEntry>
    {
        // These defaults are set in code but you can edit them freely in the Inspector
    };

    private void OnEnable()
    {
        // Populate default caps if the list is empty (first time ScriptableObject is created)
        if (statCaps == null || statCaps.Count == 0)
        {
            statCaps = new List<StatCapEntry>
            {
                new StatCapEntry { statType = StatType.CritChance,     cap = 1.00f  }, // 100%
                new StatCapEntry { statType = StatType.CritMultiplier, cap = 5.00f  }, // 500%
                new StatCapEntry { statType = StatType.LifeSteal,      cap = 0.75f  }, // 75%
                new StatCapEntry { statType = StatType.LootRange,      cap = 15f    },
                new StatCapEntry { statType = StatType.Luck,           cap = 20f    },
                new StatCapEntry { statType = StatType.MoveSpeed,      cap = 30f    },
                new StatCapEntry { statType = StatType.DashDistance,   cap = 30f    },
                new StatCapEntry { statType = StatType.MaxEnergy,      cap = 300f   },
                new StatCapEntry { statType = StatType.BurnStrength,   cap = 3.00f  }, // 300%
                new StatCapEntry { statType = StatType.FreezeStrength, cap = 3.00f  },
                new StatCapEntry { statType = StatType.HolyStrength,   cap = 3.00f  },
                new StatCapEntry { statType = StatType.ShockStrength,  cap = 3.00f  },
            };
        }
    }

    // ================================================================
    //  PUBLIC API
    //  Pass PlayerStats so capped stats can be filtered out of rolls.
    // ================================================================

    public List<UpgradeOffer> RollLevelUpOffers(int layer, float luck,
                                                 PlayerStats stats = null, int count = 3)
    {
        var offers = new List<UpgradeOffer>();
        var usedIds = new HashSet<string>();
        var pool = new List<PlayerUpgradeData>(upgrades);
        int attempts = 0;

        while (offers.Count < count && pool.Count > 0 && attempts < 100)
        {
            attempts++;
            int idx = Random.Range(0, pool.Count);
            PlayerUpgradeData d = pool[idx];
            pool.RemoveAt(idx);
            if (usedIds.Contains(d.upgradeId)) continue;

            UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
            offers.Add(BuildOffer(d, rarity, stats));
            usedIds.Add(d.upgradeId);
        }
        return offers;
    }

    public UpgradeOffer RollChestOffer(int layer, float luck, PlayerStats stats = null)
    {
        if (upgrades.Count == 0) return null;
        UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
        PlayerUpgradeData data = upgrades[Random.Range(0, upgrades.Count)];
        return BuildOffer(data, rarity, stats);
    }

    public List<UpgradeOffer> RollMerchantOffers(int layer, float luck,
                                                   PlayerStats stats = null, int count = 3)
        => RollLevelUpOffers(layer + 2, luck, stats, count);

    public UpgradeOffer BuildOffer(PlayerUpgradeData data, UpgradeRarity rarity,
                                    PlayerStats stats = null)
    {
        var offer = new UpgradeOffer();
        offer.data = data;
        offer.rarity = rarity;
        offer.statBonuses = new List<UpgradeStatBonus>();

        int count = data.GetStatCount(rarity);
        if (count <= 0 || data.statRanges == null || data.statRanges.Length == 0)
            return offer;

        // Filter out stat ranges where the stat is already at its cap
        var available = GetAvailableRanges(data.statRanges, stats);
        if (available.Count == 0) return offer;

        RollFromRanges(available, rarity, count, offer.statBonuses);
        return offer;
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    /// <summary>
    /// Returns only the stat range entries where the stat hasn't hit its cap yet.
    /// If no PlayerStats is provided, all entries are returned.
    /// </summary>
    private List<StatRangeEntry> GetAvailableRanges(StatRangeEntry[] ranges, PlayerStats stats)
    {
        var result = new List<StatRangeEntry>();
        foreach (var entry in ranges)
        {
            if (stats == null || !IsAtCap(entry.statType, stats))
                result.Add(entry);
        }
        return result;
    }

    private bool IsAtCap(StatType t, PlayerStats s)
    {
        float cap = GetCap(t);
        if (cap >= 9999f) return false;
        return GetCurrentValue(t, s) >= cap;
    }

    private float GetCap(StatType t)
    {
        foreach (var entry in statCaps)
            if (entry.statType == t) return entry.cap;
        return 9999f; // no cap by default
    }

    private float GetCurrentValue(StatType t, PlayerStats s) => t switch
    {
        StatType.MaxHP => s.maxHP,
        StatType.ArrowDamage => s.arrowDamage,
        StatType.DashDamage => s.dashDamage,
        StatType.CritChance => s.critChance,
        StatType.CritMultiplier => s.critMultiplier,
        StatType.KnockbackForce => s.knockbackForce,
        StatType.BurnStrength => s.burnStrength,
        StatType.FreezeStrength => s.freezeStrength,
        StatType.HolyStrength => s.holyStrength,
        StatType.ShockStrength => s.shockStrength,
        StatType.MoveSpeed => s.moveSpeed,
        StatType.DashDistance => s.dashDistance,
        StatType.DashCost => s.dashCost,
        StatType.DashInvincibility => s.dashInvincibilityWindow,
        StatType.MaxEnergy => s.maxEnergy,
        StatType.HoverDrainRate => s.hoverDrainRate,
        StatType.ChargeDrainRate => s.arrowChargeDrainRate,
        StatType.ChargeDuration => s.arrowChargeDuration,
        StatType.LifeSteal => s.lifeSteal,
        StatType.LootRange => s.lootRange,
        StatType.Luck => s.luck,
        _ => 0f
    };

    private void RollFromRanges(List<StatRangeEntry> ranges, UpgradeRarity rarity,
                                 int count, List<UpgradeStatBonus> result)
    {
        var indices = new List<int>();
        for (int i = 0; i < ranges.Count; i++) indices.Add(i);
        Shuffle(indices);

        int picked = 0;
        foreach (int i in indices)
        {
            if (picked >= count) break;
            // Roll value in display units — Apply() will convert to internal
            float rolled = ranges[i].Roll(rarity);
            result.Add(new UpgradeStatBonus { statType = ranges[i].statType, value = rolled });
            picked++;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// ================================================================
//  SUPPORTING TYPES
// ================================================================

[System.Serializable]
public class StatCapEntry
{
    public StatType statType;

    [Tooltip("Internal cap value. Percent stats: 1.0 = 100%, 0.75 = 75%. " +
             "Flat stats: same as PlayerStats field. Set to 9999 to disable.")]
    public float cap;
}

[System.Serializable]
public class UpgradeOffer
{
    public PlayerUpgradeData data;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
}