using UnityEngine;
using System.Collections;

/// <summary>
/// Immunity — become invulnerable for X seconds every Y seconds.
/// Duration: 1/1.5/2/2.5/3 seconds
/// Cooldown: 15 - 10% Cooldown stat (minimum 5s)
///
/// Inspector-configurable per level. Cooldown stat reduces the cooldown.
/// </summary>
public class UpgradeImmunity : PlayerUpgrade
{
    public override string Id => "Immunity";

    [Header("Immunity Duration per Level (seconds)")]
    public float[] durationPerLevel = { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f };

    [Header("Cooldown Base (seconds)")]
    public float baseCooldown = 15f;

    [Header("Cooldown Reduction per Cooldown stat point (fraction)")]
    [Tooltip("0.10 = 10% reduction per point. At Cooldown=10, cooldown is reduced by 100% of base — but floor prevents going below minCooldown")]
    public float cooldownReductionPerPoint = 0.10f;

    [Header("Minimum cooldown (seconds)")]
    public float minCooldown = 5f;

    // ── Private ──────────────────────────────────────────────────────────
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private MonoBehaviour host;
    private bool running = false;
    private int currentLevel = 0;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        playerHealth = mgr.GetComponent<PlayerHealth>();
        host = mgr;
        currentLevel = 1;

        if (!running)
        {
            running = true;
            host.StartCoroutine(ImmunityCycle());
        }
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        currentLevel = newLevel;
        // Cooldown and duration update automatically in the cycle
    }

    private float GetDuration()
    {
        int idx = Mathf.Clamp(currentLevel - 1, 0, durationPerLevel.Length - 1);
        return durationPerLevel[idx];
    }

    private float GetCooldown()
    {
        float reduction = playerStats != null ? playerStats.cooldown * cooldownReductionPerPoint : 0f;
        return Mathf.Max(minCooldown, baseCooldown * (1f - reduction));
    }

    private IEnumerator ImmunityCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetCooldown());
            playerHealth?.StartDashInvincibility(GetDuration());
        }
    }
}