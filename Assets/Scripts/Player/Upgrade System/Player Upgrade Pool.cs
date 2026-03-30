using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Upgrades/Upgrade Pool")]
public class PlayerUpgradePool : ScriptableObject
{
    [Header("All upgrades — drag every PlayerUpgradeData asset here")]
    public List<PlayerUpgradeData> upgrades = new List<PlayerUpgradeData>();

    // ================================================================
    //  PUBLIC API
    // ================================================================

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
            PlayerUpgradeData d = pool[idx];
            pool.RemoveAt(idx);
            if (usedIds.Contains(d.upgradeId)) continue;

            UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
            offers.Add(BuildOffer(d, rarity));
            usedIds.Add(d.upgradeId);
        }
        return offers;
    }

    public UpgradeOffer RollChestOffer(int layer, float luck)
    {
        if (upgrades.Count == 0) return null;
        UpgradeRarity rarity = UpgradeRarityRoller.Roll(layer, luck);
        PlayerUpgradeData data = upgrades[Random.Range(0, upgrades.Count)];
        return BuildOffer(data, rarity);
    }

    public List<UpgradeOffer> RollMerchantOffers(int layer, float luck, int count = 3)
        => RollLevelUpOffers(layer + 2, luck, count);

    public UpgradeOffer BuildOffer(PlayerUpgradeData data, UpgradeRarity rarity)
    {
        var offer = new UpgradeOffer();
        offer.data = data;
        offer.rarity = rarity;
        offer.statBonuses = new List<UpgradeStatBonus>();

        int count = data.GetStatCount(rarity);
        if (count <= 0) return offer;

        if (data.isPureStatUpgrade)
        {
            // Pure stat upgrade: roll all available stat ranges, up to count
            RollFromRanges(data.statRanges, rarity, count, offer.statBonuses);
        }
        else
        {
            // Behaviour upgrade: randomly pick 'count' stats from statRanges
            RollFromRanges(data.statRanges, rarity, count, offer.statBonuses);
        }

        return offer;
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void RollFromRanges(StatRangeEntry[] ranges, UpgradeRarity rarity,
                                 int count, List<UpgradeStatBonus> result)
    {
        if (ranges == null || ranges.Length == 0) return;

        // Shuffle indices so we pick random subset if count < ranges.Length
        var indices = new List<int>();
        for (int i = 0; i < ranges.Length; i++) indices.Add(i);
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
public class UpgradeOffer
{
    public PlayerUpgradeData data;
    public UpgradeRarity rarity;
    public List<UpgradeStatBonus> statBonuses;
}