using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Predator upgrade.
///
/// Killing an enemy while dashing through them instantly refunds the full
/// dash energy cost. This lets aggressive players chain dashes by killing
/// enemies mid-dash without spending extra energy.
///
/// DETECTION:
///   Subscribes to OnDashHitEnemy. One frame later, checks if the hit
///   enemy has been destroyed (i.e. the dash kill it). If so, the dash
///   cost is refunded via PlayerEnergy.RestoreEnergy.
///
/// Note: Phantom Step's teleport dash does NOT enable the damage collider,
/// so OnDashHitEnemy won't fire during a teleport. Predator is compatible
/// with normal dashes only (and combos naturally with other dash upgrades).
/// </summary>
public class UpgradePredator : PlayerUpgrade
{
    // ================================================================
    //  REFERENCES
    // ================================================================

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerEnergy playerEnergy;
    private DashAbility dashAbility;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats    = mgr.GetComponent<PlayerStats>();
        playerEnergy   = mgr.GetComponent<PlayerEnergy>();
        dashAbility    = mgr.GetComponent<DashAbility>();

        mgr.OnDashHitEnemy += OnDashHitEnemy;
    }

    private void OnDestroy()
    {
        if (upgradeManager != null)
            upgradeManager.OnDashHitEnemy -= OnDashHitEnemy;
    }

    // ================================================================
    //  DASH HIT HANDLER
    // ================================================================

    private void OnDashHitEnemy(GameObject enemyGO)
    {
        if (enemyGO == null) return;
        StartCoroutine(CheckKill(enemyGO));
    }

    /// <summary>
    /// Waits one frame for the damage to resolve, then checks if the enemy
    /// is gone (destroyed = killed by the dash). If so, refund the dash cost.
    /// </summary>
    private IEnumerator CheckKill(GameObject enemyGO)
    {
        yield return null; // wait one physics/update frame

        bool isDead = (enemyGO == null || !enemyGO.activeInHierarchy);
        if (!isDead) yield break;

        float refund = playerStats != null ? playerStats.dashCost : (dashAbility != null ? dashAbility.dashCost : 0f);
        if (refund > 0f && playerEnergy != null)
            playerEnergy.RestoreEnergy(refund);
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        float cost = stats.dashCost;
        return $"Killing an enemy while dashing through them refunds {PSY(cost, "F0")} energy " +
               $"(your full dash cost).\n" +
               $"Chain dashes through enemies at no net cost.\n" +
               $"Dash cost scales with <color=#FF66CC>Psyche</color>.";
    }
}
