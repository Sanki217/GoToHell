using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pool of upgrade orb prefabs.
///
/// RARITY CHANGE: all cards in one level-up share the same rarity,
/// rolled once via UpgradeRarityRoller.RollLevelUpRarity(luck).
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrade orb prefabs — one entry per upgrade")]
    public List<GameObject> upgradePrefabs = new List<GameObject>();

    // ================================================================
    //  PER-STAT BONUS RANGES
    // ================================================================

    [Header("Per-Stat Bonus Ranges")]
    public StatBonusRange[] statRanges;

    [Header("Luck Scaling")]
    [Tooltip("+X to both min and max per 1 Luck point, applied to every stat")]
    public float luckBonusPerPoint = 0.05f;

    [Header("Stat Weights (relative chance each stat appears on a card)")]
    public float weightAgility = 1f;
    public float weightAttackDamage = 1f;
    public float weightLuck = 1f;
    public float weightPsyche = 1f;
    public float weightHealth = 1f;
    public float weightSize = 1f;
    public float weightCooldown = 1f;

    // ================================================================
    //  DEFAULT RANGES
    // ================================================================

    private void OnEnable()
    {
        if (statRanges != null && statRanges.Length > 0) return;

        statRanges = new StatBonusRange[]
        {
            new StatBonusRange { stat = PrimaryStat.Agility,
                commonMin=0.3f, commonMax=1.0f, rareMin=0.8f, rareMax=2.0f,
                epicMin=1.5f, epicMax=3.0f, legendaryMin=2.5f, legendaryMax=5.0f },
            new StatBonusRange { stat = PrimaryStat.AttackDamage,
                commonMin=0.3f, commonMax=1.0f, rareMin=0.8f, rareMax=2.0f,
                epicMin=1.5f, epicMax=3.0f, legendaryMin=2.5f, legendaryMax=5.0f },
            new StatBonusRange { stat = PrimaryStat.Luck,
                commonMin=0.1f, commonMax=0.5f, rareMin=0.3f, rareMax=1.0f,
                epicMin=0.6f, epicMax=1.5f, legendaryMin=1.0f, legendaryMax=2.5f },
            new StatBonusRange { stat = PrimaryStat.Psyche,
                commonMin=0.2f, commonMax=0.8f, rareMin=0.5f, rareMax=1.5f,
                epicMin=1.0f, epicMax=2.5f, legendaryMin=2.0f, legendaryMax=4.0f },
            new StatBonusRange { stat = PrimaryStat.Health,
                commonMin=1.0f, commonMax=3.0f, rareMin=2.0f, rareMax=5.0f,
                epicMin=4.0f, epicMax=8.0f, legendaryMin=7.0f, legendaryMax=12.0f },
            new StatBonusRange { stat = PrimaryStat.Size,
                commonMin=0.1f, commonMax=0.4f, rareMin=0.2f, rareMax=0.8f,
                epicMin=0.5f, epicMax=1.2f, legendaryMin=1.0f, legendaryMax=2.0f },
            new StatBonusRange { stat = PrimaryStat.Cooldown,
                commonMin=0.2f, commonMax=0.8f, rareMin=0.5f, rareMax=1.5f,
                epicMin=1.0f, epicMax=2.5f, legendaryMin=2.0f, legendaryMax=4.0f },
        };
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Rolls up to `count` distinct offers for the level-up screen.
    /// ALL cards share the same rarity, rolled once here.
    /// </summary>
    public List<UpgradeOrbOffer> RollLevelUpOffers(int layer, float luck,
                                                    PlayerStats stats = null, int count = 3,
                                                    PlayerUpgradeManager upgradeManager = null,
                                                    System.Collections.Generic.HashSet<string> extraExcludeIds = null)
    {
        // Roll ONE rarity for ALL cards this level-up
        UpgradeRarity sharedRarity = UpgradeRarityRoller.RollLevelUpRarity(luck);

        var offers = new List<UpgradeOrbOffer>();
        var pool = new List<GameObject>(upgradePrefabs);
        int attempts = 0;

        while (offers.Count < count && pool.Count > 0 && attempts < 100)
        {
            attempts++;
            int idx = Random.Range(0, pool.Count);
            GameObject prefab = pool[idx];
            pool.RemoveAt(idx);

            if (prefab == null) continue;

            PlayerUpgrade upgrade = prefab.GetComponent<PlayerUpgrade>();
            if (upgrade == null) continue;

            // Skip upgrades already owned by the manager
            if (upgradeManager != null && upgradeManager.HasUpgrade(upgrade.UpgradeId)) continue;

            // Skip upgrades picked this session but whose orbs haven't landed yet
            if (extraExcludeIds != null && extraExcludeIds.Contains(upgrade.UpgradeId)) continue;

            offers.Add(BuildOffer(prefab, upgrade, sharedRarity, stats));
        }

        return offers;
    }

    /// <summary>Rolls one offer for a chest — independent rarity, no pity.</summary>
    public UpgradeOrbOffer RollChestOffer(float luck, PlayerStats stats = null,
                                           PlayerUpgradeManager upgradeManager = null)
    {
        var pool = new List<GameObject>(upgradePrefabs);
        int attempts = 0;

        while (pool.Count > 0 && attempts < 100)
        {
            attempts++;
            int idx = Random.Range(0, pool.Count);
            GameObject prefab = pool[idx];
            pool.RemoveAt(idx);

            if (prefab == null) continue;
            PlayerUpgrade upgrade = prefab.GetComponent<PlayerUpgrade>();
            if (upgrade == null) continue;
            if (upgradeManager != null && upgradeManager.HasUpgrade(upgrade.UpgradeId)) continue;

            UpgradeRarity rarity = UpgradeRarityRoller.RollWithLuckOnly(luck);
            return BuildOffer(prefab, upgrade, rarity, stats);
        }

        return null;
    }

    public UpgradeOrbOffer BuildOffer(GameObject prefab, PlayerUpgrade upgrade,
                                      UpgradeRarity rarity, PlayerStats stats)
    {
        var offer = new UpgradeOrbOffer
        {
            prefab = prefab,
            upgrade = upgrade,
            rarity = rarity
        };

        float luckVal = stats != null ? stats.luck : 0f;
        int count = upgrade.GetStatCount(rarity);
        offer.statBonuses = new List<UpgradeStatBonus>();
        if (count > 0)
            RollStatBonuses(count, rarity, luckVal, offer.statBonuses);

        return offer;
    }

    /// <summary>All display names for the chest slot-machine animation.</summary>
    public List<string> GetAllDisplayNames()
    {
        var names = new List<string>();
        foreach (var prefab in upgradePrefabs)
        {
            if (prefab == null) continue;
            PlayerUpgrade upgrade = prefab.GetComponent<PlayerUpgrade>();
            if (upgrade != null) names.Add(upgrade.displayName);
        }
        return names;
    }

    // ================================================================
    //  PRIVATE — stat rolling
    // ================================================================

    private void RollStatBonuses(int count, UpgradeRarity rarity, float luck,
                                  List<UpgradeStatBonus> result)
    {
        float luckBonus = luck * luckBonusPerPoint;
        var statPool = BuildWeightedStatPool();
        var usedStats = new HashSet<PrimaryStat>();
        int picked = 0, attempts = 0;

        while (picked < count && attempts < 50)
        {
            attempts++;
            PrimaryStat stat = WeightedPick(statPool);
            if (usedStats.Contains(stat)) continue;
            usedStats.Add(stat);

            (float min, float max) = GetRarityRange(rarity, stat);
            min += luckBonus;
            max += luckBonus;

            float raw = Random.Range(min, max);
            float value = Mathf.Round(raw * 10f) / 10f;
            value = Mathf.Max(0.1f, value);

            result.Add(new UpgradeStatBonus { stat = stat, value = value });
            picked++;
        }
    }

    private (float min, float max) GetRarityRange(UpgradeRarity rarity, PrimaryStat stat)
    {
        if (statRanges != null)
        {
            foreach (var entry in statRanges)
            {
                if (entry.stat != stat) continue;
                return rarity switch
                {
                    UpgradeRarity.Common => (entry.commonMin, entry.commonMax),
                    UpgradeRarity.Rare => (entry.rareMin, entry.rareMax),
                    UpgradeRarity.Epic => (entry.epicMin, entry.epicMax),
                    UpgradeRarity.Legendary => (entry.legendaryMin, entry.legendaryMax),
                    _ => (entry.commonMin, entry.commonMax),
                };
            }
        }
        return (0.3f, 1.0f);
    }

    private List<(PrimaryStat stat, float weight)> BuildWeightedStatPool() =>
        new List<(PrimaryStat, float)>
        {
            (PrimaryStat.Agility,      weightAgility),
            (PrimaryStat.AttackDamage, weightAttackDamage),
            (PrimaryStat.Luck,         weightLuck),
            (PrimaryStat.Psyche,       weightPsyche),
            (PrimaryStat.Health,       weightHealth),
            (PrimaryStat.Size,         weightSize),
            (PrimaryStat.Cooldown,     weightCooldown),
        };

    private PrimaryStat WeightedPick(List<(PrimaryStat stat, float weight)> pool)
    {
        float total = 0f;
        foreach (var e in pool) total += e.weight;
        float roll = Random.Range(0f, total);
        float running = 0f;
        foreach (var e in pool)
        {
            running += e.weight;
            if (roll <= running) return e.stat;
        }
        return pool[pool.Count - 1].stat;
    }
}

// ================================================================
//  OFFER  — carries prefab + rolled data from pool to UI
// ================================================================

public class UpgradeOrbOffer
{
    public GameObject prefab;
    public PlayerUpgrade upgrade;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
}

// ================================================================
//  STAT BONUS RANGE  — per-stat min/max at each rarity
// ================================================================

[System.Serializable]
public class StatBonusRange
{
    public PrimaryStat stat;
    public float commonMin; public float commonMax;
    public float rareMin; public float rareMax;
    public float epicMin; public float epicMax;
    public float legendaryMin; public float legendaryMax;
}