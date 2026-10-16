using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pool of upgrade orb prefabs.
///
/// RARITY: each upgrade has a FIXED rarity, set per-prefab on PlayerUpgrade.rarity
/// (e.g. Enemy Explosion is always Legendary).
/// Level-ups roll ONE rarity per level-up (luck + pity) and every card comes
/// from that tier — fewer than 3 cards when the tier runs short of unowned
/// upgrades, nearest tier when it has none at all.
/// Chests roll a luck-only rarity for their single item, same nearest-tier fallback.
///
/// BUILD SYNERGY (UpgradeTag): inside the rolled tier, upgrades are picked by
/// weight instead of uniformly. Each owned upgrade that shares a tag adds
/// synergyWeightPerOwned (capped at maxSynergyMultiplier), pacts / meta
/// upgrades can favour tags (RunModifiers.FavouredTags) and banned tags are
/// never offered. Rarity odds are untouched — only WHICH upgrade of that
/// rarity shows up.
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrade orb prefabs — one entry per upgrade")]
    public List<GameObject> upgradePrefabs = new List<GameObject>();

    // ================================================================
    //  PER-STAT BONUS RANGES
    // ================================================================

    [Header("Build Synergy (UpgradeTag)")]
    [Tooltip("Extra weight per owned upgrade sharing a tag (0.15 = +15% each).")]
    [Min(0f)] public float synergyWeightPerOwned = 0.15f;
    [Tooltip("Cap on the synergy weight multiplier.")]
    [Min(1f)] public float maxSynergyMultiplier = 2f;

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
    /// Rolls up to `count` offers for the level-up screen. ONE rarity is rolled
    /// per level-up (luck + pity) and every card comes from that tier. If the
    /// tier has no available upgrades at all, the nearest tier is used; if it
    /// has fewer than `count`, fewer cards are returned (the UI hides the rest).
    /// </summary>
    public List<UpgradeOrbOffer> RollLevelUpOffers(int layer, float luck,
                                                    PlayerStats stats = null, int count = 3,
                                                    PlayerUpgradeManager upgradeManager = null,
                                                    System.Collections.Generic.HashSet<string> extraExcludeIds = null)
    {
        var offers = new List<UpgradeOrbOffer>();
        var candidates = CollectCandidates(upgradeManager, extraExcludeIds);
        if (candidates.Count == 0) return offers;

        UpgradeRarity rolled = UpgradeRarityRoller.RollLevelUpRarity(luck);

        // The tier all cards share: the rolled one, or the nearest with candidates
        var tierPool = new List<(GameObject prefab, PlayerUpgrade upgrade)>();
        foreach (UpgradeRarity tier in FallbackOrder(rolled))
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].upgrade.rarity == tier) tierPool.Add(candidates[i]);
            if (tierPool.Count > 0) break;
        }

        while (offers.Count < count && tierPool.Count > 0)
        {
            int idx = WeightedIndex(tierPool, upgradeManager);
            (GameObject prefab, PlayerUpgrade upgrade) = tierPool[idx];
            tierPool.RemoveAt(idx);
            offers.Add(BuildOffer(prefab, upgrade, upgrade.rarity, stats));
        }

        return offers;
    }

    /// <summary>Rolls one offer for a chest — luck-only target rarity, no pity.</summary>
    public UpgradeOrbOffer RollChestOffer(float luck, PlayerStats stats = null,
                                           PlayerUpgradeManager upgradeManager = null)
    {
        var candidates = CollectCandidates(upgradeManager, null);
        if (candidates.Count == 0) return null;

        UpgradeRarity target = UpgradeRarityRoller.RollWithLuckOnly(luck);
        int idx = PickCandidateIndex(candidates, target, upgradeManager);
        if (idx < 0) return null;

        (GameObject prefab, PlayerUpgrade upgrade) = candidates[idx];
        return BuildOffer(prefab, upgrade, upgrade.rarity, stats);
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
    //  PRIVATE — candidate selection
    // ================================================================

    /// <summary>Every prefab that may be offered right now (not owned, available, not excluded).</summary>
    private List<(GameObject prefab, PlayerUpgrade upgrade)> CollectCandidates(
        PlayerUpgradeManager upgradeManager, HashSet<string> extraExcludeIds)
    {
        var list = new List<(GameObject, PlayerUpgrade)>();
        foreach (GameObject prefab in upgradePrefabs)
        {
            if (prefab == null) continue;

            PlayerUpgrade upgrade = prefab.GetComponent<PlayerUpgrade>();
            if (upgrade == null) continue;

            // Skip upgrades already owned by the manager
            if (upgradeManager != null && upgradeManager.HasUpgrade(upgrade.UpgradeId)) continue;

            // Skip upgrades tied to a weapon/class the player isn't running
            if (!upgrade.IsAvailableForCurrentRun()) continue;

            // Skip tags banned by a pact / meta upgrade
            if ((upgrade.tags & RunModifiers.BannedTags) != 0) continue;

            // Skip upgrades picked this session but whose orbs haven't landed yet
            if (extraExcludeIds != null && extraExcludeIds.Contains(upgrade.UpgradeId)) continue;

            list.Add((prefab, upgrade));
        }
        return list;
    }

    /// <summary>
    /// Random candidate of the target rarity. If that tier is empty, walks down
    /// through the tiers below, then up through the tiers above.
    /// Returns -1 only when there are no candidates at all.
    /// </summary>
    private int PickCandidateIndex(
        List<(GameObject prefab, PlayerUpgrade upgrade)> candidates, UpgradeRarity target,
        PlayerUpgradeManager upgradeManager)
    {
        var matching = new List<(GameObject prefab, PlayerUpgrade upgrade)>();
        var indices = new List<int>();
        foreach (UpgradeRarity tier in FallbackOrder(target))
        {
            matching.Clear();
            indices.Clear();
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].upgrade.rarity == tier) { matching.Add(candidates[i]); indices.Add(i); }

            if (matching.Count > 0)
                return indices[WeightedIndex(matching, upgradeManager)];
        }
        return -1;
    }

    // ================================================================
    //  PRIVATE — synergy weighting
    // ================================================================

    /// <summary>
    /// Offer weight of one upgrade: 1, raised by owned upgrades sharing its
    /// tags (capped) and by pact / meta favoured tags. Public so the balance
    /// sheet can preview it.
    /// </summary>
    public float OfferWeight(PlayerUpgrade upgrade, PlayerUpgradeManager upgradeManager)
    {
        if (upgrade == null) return 0f;
        float w = 1f;

        if (upgradeManager != null && upgrade.tags != UpgradeTag.None)
        {
            int shared = upgradeManager.CountOwnedWithAny(upgrade.tags);
            w = Mathf.Min(1f + shared * synergyWeightPerOwned, Mathf.Max(1f, maxSynergyMultiplier));
        }

        if ((upgrade.tags & RunModifiers.FavouredTags) != 0)
            w *= RunModifiers.FavouredTagMult;

        return w;
    }

    private int WeightedIndex(List<(GameObject prefab, PlayerUpgrade upgrade)> list,
                              PlayerUpgradeManager upgradeManager)
    {
        float total = 0f;
        for (int i = 0; i < list.Count; i++) total += OfferWeight(list[i].upgrade, upgradeManager);
        if (total <= 0f) return Random.Range(0, list.Count);

        float roll = Random.value * total;
        for (int i = 0; i < list.Count; i++)
        {
            roll -= OfferWeight(list[i].upgrade, upgradeManager);
            if (roll <= 0f) return i;
        }
        return list.Count - 1;
    }

    private static IEnumerable<UpgradeRarity> FallbackOrder(UpgradeRarity target)
    {
        yield return target;
        for (int r = (int)target - 1; r >= (int)UpgradeRarity.Common; r--)
            yield return (UpgradeRarity)r;
        for (int r = (int)target + 1; r <= (int)UpgradeRarity.Legendary; r++)
            yield return (UpgradeRarity)r;
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
