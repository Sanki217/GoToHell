using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Burning Weapon upgrade (common pool — works with every weapon and class).
/// Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: ALL weapon damage applies Burn — arrows, sword slashes, and dash
/// hits. Also boosts base burn strength. Burn tick damage scales with Psyche.
///
/// (This replaces the old bow-only Burning Arrow — same prefab/GUID.)
/// </summary>
public class UpgradeBurningWeapon : PlayerUpgrade
{
    [Header("Burning Weapon — Tuning")]
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
        mgr.OnSlashHitEnemy += OnMeleeHit;
        mgr.OnDashHitEnemy += OnMeleeHit;
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        float burn = stats.baseBurnStrength + burnBonus + stats.burnPerPsyche * stats.psyche;
        return $"Your weapon hits set enemies on fire dealing {PSY(burn, "F2")} burn strength.\n" +
               $"Works with arrows, slashes, and dashes. Scales with <color=#FF66CC>Psyche</color>.";
    }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit) => ApplyBurn(enemy);
    private void OnMeleeHit(GameObject enemy) => ApplyBurn(enemy);

    private void ApplyBurn(GameObject enemyGO)
    {
        Enemy e = enemyGO != null ? enemyGO.GetComponentInParent<Enemy>() : null;
        if (e != null) e.ApplyStatus(StatusType.Burn, playerStats, upgradeManager);
    }
}
