using UnityEngine;

/// <summary>
/// Arrow Pierce upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
/// All tuning values are exposed in the Inspector on the prefab.
///
/// Effect: arrows pass through enemies, dealing bonus damage per pass.
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
}