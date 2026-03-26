using UnityEngine;

/// <summary>
/// Burning Arrow upgrade.
/// On any arrow hitting an enemy, applies the Burn status effect.
///
/// This is both a stat upgrade (increases BurnStrength) and a behavior upgrade
/// (subscribes to OnArrowHitEnemy and applies Burn).
///
/// Rarity table example:
///   Common:    applies Burn
///   Rare:      applies Burn + BurnStrength +20%
///   Epic:      applies Burn + BurnStrength +40%
///   Legendary: applies Burn + BurnStrength +60%
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

        // Subscribe to all arrow hit events
        mgr.OnArrowHitEnemy += OnArrowHit;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        // Each level adds BurnStrength — upgrade stat at each stack
        if (playerStats != null)
            playerStats.burnStrength += 0.2f;
    }

    private void OnArrowHit(GameObject enemyGO, float chargeLevel, bool wasCrit)
    {
        Enemy enemy = enemyGO.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.ApplyStatus(StatusType.Burn, playerStats, upgradeManager);
    }
}