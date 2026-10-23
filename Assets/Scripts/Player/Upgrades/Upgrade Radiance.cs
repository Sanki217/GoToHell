using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Priest class upgrade — Holy Nova heals and smites harder over a wider area.
/// requiredClass = "priest" (set on the prefab).
/// </summary>
public class UpgradeRadiance : PlayerUpgrade
{
    [Header("Radiance — Tuning")]
    [Tooltip("Heal + damage multiplier.")]
    public float potencyMultiplier = 1.5f;
    public float radiusMultiplier = 1.25f;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        if (mgr != null && mgr.TryGetComponent(out HolyNovaSkill skill))
        {
            skill.potencyMultiplier *= potencyMultiplier;
            skill.radiusMultiplier *= radiusMultiplier;
        }
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses) =>
        $"Holy Nova heals and smites {PSY((potencyMultiplier - 1f) * 100f, "F0")}% harder over a " +
        $"{SZ((radiusMultiplier - 1f) * 100f, "F0")}% wider area.";
}
