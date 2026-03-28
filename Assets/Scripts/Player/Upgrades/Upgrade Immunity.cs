using UnityEngine;
using System.Collections;

/// <summary>
/// Immunity — automatically grants damage immunity for X seconds every Y seconds.
/// Level 1: immune 2s every 12s
/// Each level: immunity duration increases by 0.5s, cooldown decreases by 1s (min 4s)
/// </summary>
public class UpgradeImmunity : PlayerUpgrade
{
    public override string Id => "Immunity";

    private float immunityDuration = 2f;
    private float cooldown = 12f;
    private const float MinCooldown = 4f;

    private PlayerHealth playerHealth;
    private bool running = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerHealth = mgr.GetComponent<PlayerHealth>();
        // Start the immunity cycle using a MonoBehaviour coroutine host
        var host = mgr.GetComponent<MonoBehaviour>();
        if (host != null && !running)
        {
            running = true;
            host.StartCoroutine(ImmunityCycle(host));
        }
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        immunityDuration += 0.5f;
        cooldown = Mathf.Max(MinCooldown, cooldown - 1f);
    }

    private IEnumerator ImmunityCycle(MonoBehaviour host)
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldown);
            // Trigger invincibility window
            playerHealth?.StartDashInvincibility(immunityDuration);
            // Optional: play an FX to signal the immunity window
           // FXManager.Play(ActionFX.PlayerDash, host.transform.position);
        }
    }
}