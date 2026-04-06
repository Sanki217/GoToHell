using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrades — drag every PlayerUpgradeData asset here")]
    public List<PlayerUpgradeData> upgrades = new List<PlayerUpgradeData>();

    // ================================================================
    //  PER-STAT BONUS RANGES
    //  Each primary stat has its own min/max per rarity.
    //  Luck adds luckBonusPerPoint to BOTH min and max for every stat.
    //  Values are in primary stat units (float, 0.1 precision).
    //
    //  Defaults populate in OnEnable — override freely in the Inspector
    //  on your PlayerUpgradePool ScriptableObject asset.
    // ================================================================

    [Header("Per-Stat Bonus Ranges (each stat configurable independently)")]
    [Tooltip("One entry per primary stat. Luck scales both ends by luckBonusPerPoint.")]
    public StatBonusRange[] statRanges;

    [Header("Luck Scaling")]
    [Tooltip("+X to both min and max per 1 Luck point, applied to every stat")]
    public float luckBonusPerPoint = 0.05f;

    [Header("Stat Weights (relative chance each stat appears on a card)")]
    [Tooltip("Increase a value to make that stat appear more often on upgrade cards")]
    public float weightAgility = 1f;
    public float weightAttackDamage = 1f;
    public float weightAbilityPower = 1f;
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
                commonMin=0.3f, commonMax=1.0f,
                rareMin=0.8f,   rareMax=2.0f,
                epicMin=1.5f,   epicMax=3.0f,
                legendaryMin=2.5f, legendaryMax=5.0f },

            new StatBonusRange { stat = PrimaryStat.AttackDamage,
                commonMin=0.3f, commonMax=1.0f,
                rareMin=0.8f,   rareMax=2.0f,
                epicMin=1.5f,   epicMax=3.0f,
                legendaryMin=2.5f, legendaryMax=5.0f },

            new StatBonusRange { stat = PrimaryStat.AbilityPower,
                commonMin=0.3f, commonMax=1.0f,
                rareMin=0.8f,   rareMax=2.0f,
                epicMin=1.5f,   epicMax=3.0f,
                legendaryMin=2.5f, legendaryMax=5.0f },

            // Luck — smaller increments, each point is high impact
            new StatBonusRange { stat = PrimaryStat.Luck,
                commonMin=0.1f, commonMax=0.5f,
                rareMin=0.3f,   rareMax=1.0f,
                epicMin=0.6f,   epicMax=1.5f,
                legendaryMin=1.0f, legendaryMax=2.5f },

            new StatBonusRange { stat = PrimaryStat.Psyche,
                commonMin=0.2f, commonMax=0.8f,
                rareMin=0.5f,   rareMax=1.5f,
                epicMin=1.0f,   epicMax=2.5f,
                legendaryMin=2.0f, legendaryMax=4.0f },

            // Health — larger numbers since 1 Health = 1 max HP
            new StatBonusRange { stat = PrimaryStat.Health,
                commonMin=1.0f, commonMax=3.0f,
                rareMin=2.0f,   rareMax=5.0f,
                epicMin=4.0f,   epicMax=8.0f,
                legendaryMin=7.0f, legendaryMax=12.0f },

            // Size — small increments, each matters for AoE radius
            new StatBonusRange { stat = PrimaryStat.Size,
                commonMin=0.1f, commonMax=0.4f,
                rareMin=0.2f,   rareMax=0.8f,
                epicMin=0.5f,   epicMax=1.2f,
                legendaryMin=1.0f, legendaryMax=2.0f },

            new StatBonusRange { stat = PrimaryStat.Cooldown,
                commonMin=0.2f, commonMax=0.8f,
                rareMin=0.5f,   rareMax=1.5f,
                epicMin=1.0f,   epicMax=2.5f,
                legendaryMin=2.0f, legendaryMax=4.0f },
        };
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public List<UpgradeOffer> RollLevelUpOffers(int layer, float luck,
                                                 PlayerStats stats = null, int count = 3,
                                                 PlayerUpgradeManager upgradeManager = null)
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
            // Skip upgrades already owned — each upgrade has one level, disappears when picked
            if (upgradeManager != null && upgradeManager.HasUpgrade(d.upgradeId)) continue;

            UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
            offers.Add(BuildOffer(d, rarity, stats));
            usedIds.Add(d.upgradeId);
        }
        return offers;
    }

    public UpgradeOffer RollChestOffer(float luck, PlayerStats stats = null)
    {
        if (upgrades.Count == 0) return null;
        UpgradeRarity rarity = UpgradeRarityRoller.RollWithLuckOnly(luck);
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

        if (data.isCurse) return offer;

        int count = data.GetStatCount(rarity);
        if (count <= 0) return offer;

        float luckVal = stats != null ? stats.luck : 0f;
        RollStatBonuses(count, rarity, luckVal, offer.statBonuses);
        return offer;
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void RollStatBonuses(int count, UpgradeRarity rarity, float luck,
                                  List<UpgradeStatBonus> result)
    {
        float luckBonus = luck * luckBonusPerPoint;
        var statPool = BuildWeightedStatPool();
        var usedStats = new HashSet<PrimaryStat>();
        int picked = 0;
        int attempts = 0;

        while (picked < count && attempts < 50)
        {
            attempts++;
            PrimaryStat stat = WeightedPick(statPool);
            if (usedStats.Contains(stat)) continue;
            usedStats.Add(stat);

            // Each stat gets its own range for this rarity
            (float min, float max) = GetRarityRange(rarity, stat);
            min += luckBonus;
            max += luckBonus;

            float raw = Random.Range(min, max);
            float value = Mathf.Round(raw * 10f) / 10f;  // 0.1 precision
            value = Mathf.Max(0.1f, value);

            result.Add(new UpgradeStatBonus { stat = stat, value = value });
            picked++;
        }
    }

    /// <summary>Returns the min/max bonus range for a specific stat at a specific rarity.</summary>
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
        return (0.3f, 1.0f); // safe fallback
    }

    private List<(PrimaryStat stat, float weight)> BuildWeightedStatPool()
    {
        return new List<(PrimaryStat, float)>
        {
            (PrimaryStat.Agility,      weightAgility),
            (PrimaryStat.AttackDamage, weightAttackDamage),
            (PrimaryStat.AbilityPower, weightAbilityPower),
            (PrimaryStat.Luck,         weightLuck),
            (PrimaryStat.Psyche,       weightPsyche),
            (PrimaryStat.Health,       weightHealth),
            (PrimaryStat.Size,         weightSize),
            (PrimaryStat.Cooldown,     weightCooldown),
        };
    }

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
//  PER-STAT RANGE ENTRY
// ================================================================

[System.Serializable]
public class StatBonusRange
{
    public PrimaryStat stat;

    [Tooltip("Range at Common rarity (before luck scaling)")]
    public float commonMin;
    public float commonMax;

    [Tooltip("Range at Rare rarity")]
    public float rareMin;
    public float rareMax;

    [Tooltip("Range at Epic rarity")]
    public float epicMin;
    public float epicMax;

    [Tooltip("Range at Legendary rarity")]
    public float legendaryMin;
    public float legendaryMax;
}

[System.Serializable]
public class UpgradeOffer
{
    public PlayerUpgradeData data;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
    public int currentLevel;
}