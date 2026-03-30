using UnityEngine;

/// <summary>
/// Soul Bonus — earn X% more souls and increase loot range.
///
/// Configure in PlayerUpgradeData.behaviourSettings:
///   "bonusPercent"    — % of extra souls per soul collected (default 0.2 = 20%)
///   "lootRangeBonus"  — flat loot range added on pickup (default 1)
///   "bonusPerLevel"   — extra % added per level (default 0.2)
///   "lootPerLevel"    — extra loot range per level (default 0.5)
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    public override string Id => "SoulBonus";

    private float bonusPercent;
    private float bonusPerLevel;
    private float lootPerLevel;

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
        lootPerLevel = data?.GetSetting("lootPerLevel", 0.5f) ?? 0.5f;

        float lootBonus = data?.GetSetting("lootRangeBonus", 1f) ?? 1f;
        if (playerStats != null) playerStats.lootRange += lootBonus;

        mgr.OnSoulCollected += OnSoulCollected;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        bonusPercent += bonusPerLevel;
        if (playerStats != null) playerStats.lootRange += lootPerLevel;
    }

    private void OnSoulCollected(int amount)
    {
        if (_isGranting || inventory == null || bonusPercent <= 0f) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * bonusPercent));
        _isGranting = true;
        inventory.currentSouls += bonus;   // add directly, no event fire
        levelSystem?.AddXP(bonus);         // grant XP directly
        _isGranting = false;
    }
}