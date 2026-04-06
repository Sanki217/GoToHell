using UnityEngine;

/// <summary>
/// Soul Bonus — earn more souls from every collection.
///
/// PlayerUpgradeData behaviourSettings needed:
///   "bonusPercent" — base fraction of extra souls (default 1.0 = +100%)
///   "bonusLuck"    — fraction of Luck added to bonus (default 0.10)
///
/// Example description template:
///   "Earn [bonus] more souls from all sources."
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    public override string Id => "SoulBonus";

    private float bonusPct, bonusLuck;
    private PlayerStats playerStats;
    private PlayerInventory inventory;
    private PlayerLevelSystem levelSystem;
    private bool granting = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();
        levelSystem = mgr.GetComponent<PlayerLevelSystem>();

        var data = mgr.GetUpgradeData(Id);
        bonusPct = data?.GetSetting("bonusPercent", 1.0f) ?? 1.0f;
        bonusLuck = data?.GetSetting("bonusLuck", 0.10f) ?? 0.10f;

        mgr.OnSoulCollected += OnSoulCollected;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel) { }

    private float GetBonus() =>
        bonusPct + bonusLuck * (playerStats != null ? playerStats.luck : 0f);

    private void OnSoulCollected(int amount)
    {
        if (granting || inventory == null) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * GetBonus()));
        granting = true;
        inventory.currentSouls += bonus;
        levelSystem?.AddXPDirect(bonus);
        granting = false;
    }
}