using UnityEngine;

/// <summary>
/// Burning Arrow upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
/// All tuning values are exposed in the Inspector on the prefab.
///
/// Effect: arrows apply Burn on hit. Also boosts base burn strength.
/// </summary>
public class UpgradeBurningArrow : PlayerUpgrade
{
    [Header("Burning Arrow — Tuning")]
    [Tooltip("Flat amount added to baseBurnStrength on pickup.")]
    public float burnBonus = 0.25f;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;

        if (playerStats != null)
        {
            playerStats.baseBurnStrength += burnBonus;
            playerStats.RecalculateDerived();
        }

        mgr.OnArrowHitEnemy += OnArrowHit;
    }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit)
    {
        var e = enemy?.GetComponent<Enemy>();
        if (e != null) e.ApplyStatus(StatusType.Burn, playerStats, upgradeManager);
    }
}