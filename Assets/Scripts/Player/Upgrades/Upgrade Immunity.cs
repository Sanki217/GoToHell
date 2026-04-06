using UnityEngine;
using System.Collections;

/// <summary>
/// Immunity — become invulnerable for X seconds every Y seconds.
///
/// PlayerUpgradeData behaviourSettings needed:
///   "duration"      — invincibility duration in seconds (default 2)
///   "cooldown"      — base cooldown in seconds (default 15)
///   "cooldownStat"  — fraction reduction per Cooldown point (default 0.10)
///   "cooldownMin"   — minimum cooldown regardless of stat (default 5)
///
/// Example description template:
///   "Become invincible for [duration] every [cooldown] seconds."
/// </summary>
public class UpgradeImmunity : PlayerUpgrade
{
    public override string Id => "Immunity";

    private float duration, cdBase, cdStat, cdMin;
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private bool running = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        playerHealth = mgr.GetComponent<PlayerHealth>();

        var data = mgr.GetUpgradeData(Id);
        duration = data?.GetSetting("duration", 2f) ?? 2f;
        cdBase = data?.GetSetting("cooldown", 15f) ?? 15f;
        cdStat = data?.GetSetting("cooldownStat", 0.10f) ?? 0.10f;
        cdMin = data?.GetSetting("cooldownMin", 5f) ?? 5f;

        if (!running)
        {
            running = true;
            mgr.StartCoroutine(ImmunityCycle());
        }
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel) { }

    private float GetCooldown()
    {
        float reduction = playerStats != null ? playerStats.cooldown * cdStat : 0f;
        return Mathf.Max(cdMin, cdBase * (1f - reduction));
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