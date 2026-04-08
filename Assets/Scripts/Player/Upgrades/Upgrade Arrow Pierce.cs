using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Arrow Pierce upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: arrows pass through enemies, dealing bonus damage per pass.
/// Scales with Ability Power (pierce bonus damage).
/// </summary>
public class UpgradeArrowPierce : PlayerUpgrade
{
    [Header("Arrow Pierce — Tuning")]
    [Tooltip("How many enemies the arrow passes through.")]
    public int pierceCount = 1;

    [Tooltip("Flat bonus damage dealt per pierce.")]
    public float pierceDamage = 5f;

    [Tooltip("Fraction of Ability Power added to pierce damage.")]
    public float pierceDmgAPScaling = 0.40f;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        var stats = mgr.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.arrowPierceCount = pierceCount;
            stats.pierceDamageBase = pierceDamage;
            stats.pierceDmgAPScaling = pierceDmgAPScaling;
        }
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float pierceDmg = pierceDamage + pierceDmgAPScaling * s.abilityPower;
        return $"Arrows pierce through {pierceCount} enem{(pierceCount == 1 ? "y" : "ies")}, " +
               $"dealing {AP(pierceDmg)} bonus damage on each pass.\n" +
               $"Scales with <color=#4488FF>Ability Power</color>.";
    }
}