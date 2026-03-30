using UnityEngine;
using System.Collections;

/// <summary>
/// Immunity — auto-grants damage immunity for X seconds every Y seconds.
/// Flashes the player white/gold while immune.
///
/// Configure in PlayerUpgradeData.behaviourSettings:
///   "duration"          — immunity window in seconds (default 2)
///   "cooldown"          — seconds between immunity windows (default 12)
///   "durationPerLevel"  — duration increase per level (default 0.5)
///   "cooldownReduction" — cooldown decrease per level (default 1, min 4s)
/// </summary>
public class UpgradeImmunity : PlayerUpgrade
{
    public override string Id => "Immunity";

    private float immunityDuration;
    private float cooldown;
    private float durationPerLevel;
    private float cooldownReduction;
    private const float MinCooldown = 4f;

    private PlayerHealth playerHealth;
    private Renderer playerRenderer;
    private bool running = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerHealth = mgr.GetComponent<PlayerHealth>();
        playerRenderer = mgr.GetComponentInChildren<Renderer>();

        var data = mgr.GetUpgradeData(Id);
        immunityDuration = data?.GetSetting("duration", 2f) ?? 2f;
        cooldown = data?.GetSetting("cooldown", 12f) ?? 12f;
        durationPerLevel = data?.GetSetting("durationPerLevel", 0.5f) ?? 0.5f;
        cooldownReduction = data?.GetSetting("cooldownReduction", 1f) ?? 1f;

        if (!running)
        {
            running = true;
            var host = mgr.GetComponent<MonoBehaviour>();
            host?.StartCoroutine(ImmunityCycle(host));
        }
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        immunityDuration += durationPerLevel;
        cooldown = Mathf.Max(MinCooldown, cooldown - cooldownReduction);
    }

    private IEnumerator ImmunityCycle(MonoBehaviour host)
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldown);
            playerHealth?.StartDashInvincibility(immunityDuration);

            // Flash the player color to signal immunity window
            if (playerRenderer != null)
                host.StartCoroutine(ImmunityFlash(immunityDuration));
        }
    }

    private IEnumerator ImmunityFlash(float duration)
    {
        if (playerRenderer == null) yield break;

        Color original = playerRenderer.material.color;
        Color immune = new Color(1f, 0.95f, 0.4f); // gold tint
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Pulse between gold and white
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            playerRenderer.material.color = Color.Lerp(immune, Color.white, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerRenderer.material.color = original;
    }
}