using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rogue class upgrade — Smoke Bomb lasts longer and comes back sooner.
/// requiredClass = "rogue" (set on the prefab).
/// </summary>
public class UpgradeLingeringSmoke : PlayerUpgrade
{
    [Header("Lingering Smoke — Tuning")]
    public float extraDuration = 1f;
    [Tooltip("Cooldown multiplier (0.75 = 25% shorter).")]
    public float cooldownMultiplier = 0.75f;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        if (mgr != null && mgr.TryGetComponent(out SmokeBombSkill skill))
        {
            skill.durationBonus += extraDuration;
            skill.cooldownMultiplier *= cooldownMultiplier;
        }
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses) =>
        $"Smoke Bomb lasts {CD(extraDuration)} s longer and recharges {CD((1f - cooldownMultiplier) * 100f, "F0")}% faster.";
}
