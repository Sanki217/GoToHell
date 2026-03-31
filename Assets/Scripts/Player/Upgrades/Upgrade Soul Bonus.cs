using UnityEngine;

/// <summary>
/// Soul Bonus — earn X% more souls.
/// Loot range is a SEPARATE upgrade — removed from here.
///
/// Configure in PlayerUpgradeData.behaviourSettings:
///   "bonusPercent"   — fraction of extra souls per soul collected (default 0.2 = 20%)
///   "bonusPerLevel"  — extra fraction added per level (default 0.2)
///
/// XP fix: bonus souls grant exactly 1 XP each, the same rate as normal souls,
/// without being multiplied again by xpMultiplier (which already applied to the original soul).
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    public override string Id => "SoulBonus";

    private float bonusPercent;
    private float bonusPerLevel;

    private PlayerStats playerStats;
    private PlayerInventory inventory;
    private PlayerLevelSystem levelSystem;

    private bool _isGranting = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();
        levelSystem = mgr.GetComponent<PlayerLevelSystem>();

        var data = mgr.GetUpgradeData(Id);
        bonusPercent = data?.GetSetting("bonusPercent", 0.2f) ?? 0.2f;
        bonusPerLevel = data?.GetSetting("bonusPerLevel", 0.2f) ?? 0.2f;

        mgr.OnSoulCollected += OnSoulCollected;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        bonusPercent += bonusPerLevel;
    }

    private void OnSoulCollected(int amount)
    {
        if (_isGranting || inventory == null || bonusPercent <= 0f) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * bonusPercent));

        _isGranting = true;

        // Add souls directly to counter — no event fired, no recursive loop
        inventory.currentSouls += bonus;

        // Grant XP at base rate (1 XP per bonus soul), bypassing xpMultiplier.
        // xpMultiplier already applied when the original soul was collected.
        // If we went through AddXP here it would double-multiply.
        if (levelSystem != null)
        {
            float baseXp = bonus;   // 1 XP per bonus soul, no multiplier
            // Add XP directly without going through the multiplier again
            levelSystem.AddXPDirect(baseXp);
        }

        _isGranting = false;
    }
}