using UnityEngine;

/// <summary>
/// Soul Bonus — earn X% more souls and increase loot radius.
///
/// FIX: The previous version called inventory.AddSouls() inside OnSoulsChanged,
/// which fired OnSoulsChanged again → infinite recursion → stack overflow.
///
/// Now we add the bonus souls DIRECTLY to inventory.currentSouls without
/// firing the event, and grant XP directly to PlayerLevelSystem without
/// going through the soul pipeline at all.
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    public override string Id => "SoulBonus";

    private float bonusPercent = 0.20f;  // +20% extra souls per soul collected
    private float lootRangeBonus = 1f;

    private PlayerStats playerStats;
    private PlayerInventory inventory;
    private PlayerLevelSystem levelSystem;

    // Re-entrancy guard — prevents the bonus grant from triggering itself
    private bool _isGranting = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();
        levelSystem = mgr.GetComponent<PlayerLevelSystem>();

        if (playerStats != null) playerStats.lootRange += lootRangeBonus;

        mgr.OnSoulCollected += OnSoulCollected;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        bonusPercent += 0.20f;
        if (playerStats != null) playerStats.lootRange += 0.5f;
    }

    private void OnSoulCollected(int amount)
    {
        // Guard against re-entrancy
        if (_isGranting || inventory == null) return;
        if (bonusPercent <= 0f) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * bonusPercent));

        _isGranting = true;

        // Add souls directly to the counter WITHOUT firing OnSoulsChanged
        // This prevents the recursive loop
        inventory.currentSouls += bonus;

        // Grant the XP directly to level system — bypass the soul event entirely
        levelSystem?.AddXP(bonus);

        _isGranting = false;
    }
}