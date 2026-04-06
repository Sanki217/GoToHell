using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrades — drag every PlayerUpgradeData asset here")]
    public List<PlayerUpgradeData> upgrades = new List<PlayerUpgradeData>();

    // ================================================================
    //  GLOBAL STAT BONUS RANGES
    //  These apply to ALL upgrades equally.
    //  Luck scales both min and max: +0.05 per 1 luck point.
    //  Values are in primary stat units (float, 0.1 precision).
    // ================================================================

    [Header("Global Stat Bonus Ranges per Rarity")]
    [Tooltip("Min/max primary stat bonus at Common rarity (before luck scaling)")]
    public float commonMin = 0.3f;
    public float commonMax = 0.8f;

    [Tooltip("Min/max at Rare rarity")]
    public float rareMin = 0.6f;
    public float rareMax = 1.4f;

    [Tooltip("Min/max at Epic rarity")]
    public float epicMin = 1.0f;
    public float epicMax = 2.0f;

    [Tooltip("Min/max at Legendary rarity")]
    public float legendaryMin = 1.5f;
    public float legendaryMax = 3.0f;

    [Header("Luck Scaling")]
    [Tooltip("+X to both min and max per 1 Luck point")]
    public float luckBonusPerPoint = 0.05f;

    [Header("Stat Weights (leave all equal for uniform distribution)")]
    [Tooltip("Relative chance each primary stat appears on an upgrade card. " +
             "All equal = perfectly uniform. Increase a value to make that stat more common.")]
    public float weightAgility = 1f;
    public float weightAttackDamage = 1f;
    public float weightAbilityPower = 1f;
    public float weightLuck = 1f;
    public float weightPsyche = 1f;
    public float weightHealth = 1f;
    public float weightSize = 1f;
    public float weightCooldown = 1f;

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

        // Curses have no stat bonuses
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
        // Build weighted stat pool
        var pool = BuildWeightedStatPool();
        var usedStats = new HashSet<PrimaryStat>();

        // Luck scaling
        float luckBonus = luck * luckBonusPerPoint;
        (float min, float max) = GetRarityRange(rarity);
        min += luckBonus;
        max += luckBonus;

        int picked = 0;
        int attempts = 0;

        while (picked < count && attempts < 50)
        {
            attempts++;
            PrimaryStat stat = WeightedPick(pool);
            if (usedStats.Contains(stat)) continue;

            usedStats.Add(stat);

            // Roll value in 0.1 increments
            float raw = Random.Range(min, max);
            float value = Mathf.Round(raw * 10f) / 10f;  // round to 0.1
            value = Mathf.Max(0.1f, value);         // minimum 0.1

            result.Add(new UpgradeStatBonus { stat = stat, value = value });
            picked++;
        }
    }

    private (float min, float max) GetRarityRange(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => (commonMin, commonMax),
        UpgradeRarity.Rare => (rareMin, rareMax),
        UpgradeRarity.Epic => (epicMin, epicMax),
        UpgradeRarity.Legendary => (legendaryMin, legendaryMax),
        _ => (commonMin, commonMax)
    };

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

[System.Serializable]
public class UpgradeOffer
{
    public PlayerUpgradeData data;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
    public int currentLevel; // filled by LevelUpUI from manager
}