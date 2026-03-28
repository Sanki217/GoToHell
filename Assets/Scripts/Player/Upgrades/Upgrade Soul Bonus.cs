using UnityEngine;

/// <summary>
/// Soul Bonus — earn X% more souls and increase loot radius by X%.
/// Level 1: +20% souls, +20% loot range
/// Each level: +20% more souls (cumulative), +0.5 loot range
///
/// Implementation: hooks into OnSoulCollected and adds bonus souls directly.
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    public override string Id => "SoulBonus";

    private float bonusPercent = 0.20f;   // +20% extra souls
    private float lootRangeBonus = 1f;       // flat bonus to loot range

    private PlayerStats playerStats;
    private PlayerInventory inventory;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();

        // Apply initial loot range bonus
        if (playerStats != null) playerStats.lootRange += lootRangeBonus;

        mgr.OnSoulCollected += OnSoulCollected;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        bonusPercent += 0.20f;  // +20% per level
        lootRangeBonus = 0.5f;
        if (playerStats != null) playerStats.lootRange += 0.5f;
    }

    private void OnSoulCollected(int amount)
    {
        if (inventory == null || bonusPercent <= 0f) return;
        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * bonusPercent));
        inventory.AddSouls(bonus);
    }
}