using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Burning Arrow upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: arrows apply Burn on hit. Also boosts base burn strength.
/// Scales with Ability Power (burn tick damage).
/// </summary>
public class UpgradeBurningArrow : PlayerUpgrade
{
    [Header("Burning Arrow � Tuning")]
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

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        float burn = stats.baseBurnStrength + burnBonus + stats.burnPerPsyche * stats.psyche;
        return $"Arrows set enemies on fire dealing {PSY(burn, "F2")} burn strength.\n" +
               $"Scales with <color=#FF66CC>Psyche</color>.";
    }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit)
    {
        var e = enemy?.GetComponent<Enemy>();
        if (e != null) e.ApplyStatus(StatusType.Burn, playerStats, upgradeManager);
    }
}