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
    //  For normal stats (higher = better): cap is a ceiling.
    //    e.g. CritChance cap 1.0 = max 100%
    //
    //  For inverted stats (lower = better): cap is a floor.
    //    e.g. DashCost floor 5 = never cheaper than 5 energy
    //    Set isFloor = true for these.
    //
    //  Values in internal units (same as PlayerStats fields).
    // ================================================================

    [Header("Stat Caps — internal units")]
    public List<StatCapEntry> statCaps = new List<StatCapEntry>();

    private void OnEnable()
    {
        if (statCaps == null || statCaps.Count == 0)
        {
            statCaps = new List<StatCapEntry>
            {
                new StatCapEntry { statType = StatType.CritChance,     cap = 1.00f,  isFloor = false },
                new StatCapEntry { statType = StatType.CritMultiplier, cap = 5.00f,  isFloor = false },
                new StatCapEntry { statType = StatType.LifeSteal,      cap = 0.75f,  isFloor = false },
                new StatCapEntry { statType = StatType.LootRange,      cap = 15f,    isFloor = false },
                new StatCapEntry { statType = StatType.Luck,           cap = 20f,    isFloor = false },
                new StatCapEntry { statType = StatType.MoveSpeed,      cap = 30f,    isFloor = false },
                new StatCapEntry { statType = StatType.DashDistance,   cap = 30f,    isFloor = false },
                new StatCapEntry { statType = StatType.MaxEnergy,      cap = 300f,   isFloor = false },
                new StatCapEntry { statType = StatType.BurnStrength,   cap = 3.00f,  isFloor = false },
                new StatCapEntry { statType = StatType.FreezeStrength, cap = 3.00f,  isFloor = false },
                new StatCapEntry { statType = StatType.HolyStrength,   cap = 3.00f,  isFloor = false },
                new StatCapEntry { statType = StatType.ShockStrength,  cap = 3.00f,  isFloor = false },
                // Inverted stats — floor caps (don't let them go below this)
                new StatCapEntry { statType = StatType.DashCost,       cap = 5f,     isFloor = true  },
                new StatCapEntry { statType = StatType.HoverDrainRate, cap = 1f,     isFloor = true  },
                new StatCapEntry { statType = StatType.ChargeDrainRate,cap = 0.5f,   isFloor = true  },
                new StatCapEntry { statType = StatType.ChargeDuration, cap = 0.3f,   isFloor = true  },
            };
        }
    }

    // ================================================================
    //  PUBLIC API
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

        var available = GetAvailableRanges(data.statRanges, stats);
        if (available.Count == 0) return offer;

        RollFromRanges(available, rarity, count, offer.statBonuses);
        return offer;
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

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
        StatCapEntry entry = GetCapEntry(t);
        if (entry == null) return false;

        float current = GetCurrentValue(t, s);

        if (entry.isFloor)
            // Inverted stat: capped when current value is at or below the floor
            return current <= entry.cap;
        else
            // Normal stat: capped when current value is at or above the ceiling
            return current >= entry.cap;
    }

    private StatCapEntry GetCapEntry(StatType t)
    {
        foreach (var entry in statCaps)
            if (entry.statType == t) return entry;
        return null;
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

[System.Serializable]
public class StatCapEntry
{
    public StatType statType;
    [Tooltip("For normal stats: ceiling (stat won't go above this). " +
             "For inverted stats (isFloor=true): floor (stat won't go below this).")]
    public float cap;
    [Tooltip("True for inverted stats like DashCost where lower = better. " +
             "Cap acts as a minimum floor rather than a maximum ceiling.")]
    public bool isFloor;
}

[System.Serializable]
public class UpgradeOffer
{
    public PlayerUpgradeData data;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
}