using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lifesteal upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: the player heals for a percentage of all damage dealt (arrows, dash, slash, status).
/// Base: 10%. Scales with Psyche: +0.05% per Psyche point.
///
/// Uses PlayerStats.lifeSteal which is already checked in RecordDamageDealt().
/// Re-evaluated whenever stats change (PlayerStats.OnStatsChanged), so Psyche
/// gains during the run are reflected immediately — no per-frame polling.
/// </summary>
public class UpgradeLifesteal : PlayerUpgrade
{
    [Header("Lifesteal - Tuning")]
    [Tooltip("Base heal fraction of damage dealt. 0.10 = 10%.")]
    public float baseLifesteal = 0.10f;

    [Tooltip("Additional heal fraction per 1 point of Psyche. 0.0005 = 0.05% per Psyche.")]
    public float lifestealPerPsyche = 0.0005f;

    private PlayerStats playerStats;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        if (playerStats != null) playerStats.OnStatsChanged += RefreshLifesteal;
        RefreshLifesteal();
    }

    private void OnDestroy()
    {
        if (playerStats != null) playerStats.OnStatsChanged -= RefreshLifesteal;
    }

    private void RefreshLifesteal()
    {
        if (playerStats == null) return;
        playerStats.lifeSteal = baseLifesteal + lifestealPerPsyche * playerStats.psyche;
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float pct = (baseLifesteal + lifestealPerPsyche * s.psyche) * 100f;
        return $"Heal for {HP(pct, "F1")}% of all damage dealt.\n" +
               $"Scales with <color=#FF66CC>Psyche</color>.";
    }
}
