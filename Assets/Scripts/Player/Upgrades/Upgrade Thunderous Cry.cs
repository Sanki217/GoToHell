using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Warrior class upgrade — War Cry reaches further and staggers longer.
/// requiredClass = "warrior" (set on the prefab).
/// </summary>
public class UpgradeThunderousCry : PlayerUpgrade
{
    [Header("Thunderous Cry — Tuning")]
    public float radiusMultiplier = 1.5f;
    public float extraStagger = 1f;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        if (mgr != null && mgr.TryGetComponent(out WarCrySkill skill))
        {
            skill.radiusMultiplier *= radiusMultiplier;
            skill.durationBonus += extraStagger;
        }
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses) =>
        $"War Cry radius {SZ((radiusMultiplier - 1f) * 100f, "F0")}% bigger and staggers {CD(extraStagger)} s longer.";
}
