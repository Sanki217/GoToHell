using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Immunity upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: player becomes invincible for duration seconds every cooldown seconds.
/// Scales with Cooldown stat (reduces the interval between pulses).
/// </summary>
public class UpgradeImmunity : PlayerUpgrade
{
    [Header("Immunity — Tuning")]
    [Tooltip("How long invincibility lasts in seconds.")]
    public float duration = 2f;

    [Tooltip("Base time between invincibility pulses in seconds.")]
    public float cooldown = 15f;

    [Tooltip("Fraction of cooldown reduced per 1 point of Cooldown stat. 0.10 = −10%/point.")]
    public float cooldownReductionPerStat = 0.10f;

    [Tooltip("Minimum cooldown regardless of Cooldown stat.")]
    public float cooldownMin = 5f;

    private PlayerStats playerStats;
    private PlayerHealth playerHealth;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        playerHealth = mgr.GetComponent<PlayerHealth>();
        mgr.StartCoroutine(ImmunityCycle());
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float reduction = s.cooldown * cooldownReductionPerStat;
        float effectiveCooldown = Mathf.Max(cooldownMin, cooldown * (1f - reduction));
        return $"Grants {CD(duration)}s of invincibility every {CD(effectiveCooldown)}s.\n" +
               $"Scales with <color=#44FFEE>Cooldown</color>.";
    }

    private float GetCooldown()
    {
        float reduction = playerStats != null ? playerStats.cooldown * cooldownReductionPerStat : 0f;
        return Mathf.Max(cooldownMin, cooldown * (1f - reduction));
    }

    private IEnumerator ImmunityCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetCooldown());
            playerHealth?.StartDashInvincibility(duration);
        }
    }
}