using UnityEngine;

/// <summary>
/// Burning Arrow — arrows apply Burn on hit.
/// Also increases the Burn stat by a flat amount.
///
/// PlayerUpgradeData behaviourSettings needed:
///   "burnBonus" — fraction added to baseBurnStrength (default 0.25 = +25%)
///
/// Example description template:
///   "Arrows apply [Burn] to enemies on hit, increasing Burn strength by [burn]."
/// </summary>
public class UpgradeBurningArrow : PlayerUpgrade
{
    public override string Id => "Arrow_Burn";

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;

        var data = mgr.GetUpgradeData(Id);
        if (playerStats != null && data != null)
        {
            float bonus = data.GetSetting("burnBonus", 0.25f);
            playerStats.baseBurnStrength += bonus;
            playerStats.RecalculateDerived();
        }

        mgr.OnArrowHitEnemy += OnArrowHit;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel) { }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit)
    {
        var e = enemy?.GetComponent<Enemy>();
        if (e != null) e.ApplyStatus(StatusType.Burn, playerStats, upgradeManager);
    }
}