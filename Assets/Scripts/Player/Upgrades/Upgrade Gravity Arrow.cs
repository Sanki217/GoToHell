using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gravity Arrow upgrade.
///
/// When an arrow sticks to a surface, spawns a GravityArrowZone prefab
/// at that position. Enemies within the zone are slowly pulled toward the arrow.
/// Pull speed scales with the player's Psyche stat.
///
/// SETUP:
///   1. Create a GravityArrowZone prefab (any visual — sphere, particles, etc.)
///      and add the GravityArrowZone script to it.
///   2. Assign it to the Gravity Zone Prefab field on this upgrade's orb prefab.
///
/// COMBO: place arrows around an enemy to trap it — competing pulls cancel
/// each other out and pin it in place.
/// </summary>
public class UpgradeGravityArrow : PlayerUpgrade
{
    [Header("Gravity Arrow — Prefab")]
    [Tooltip("Prefab with GravityArrowZone component. Spawned at each arrow's stuck position. " +
             "Add your own visuals (particles, glow, etc.) to this prefab.")]
    public GameObject gravityZonePrefab;

    [Header("Gravity Arrow — Psyche Scaling")]
    [Tooltip("Additional pull speed per 1 point of Psyche, added on top of the prefab's base pullSpeed.")]
    public float pullSpeedPerPsyche = 0.12f;

    [Header("Gravity Arrow — Damage")]
    [Tooltip("Base damage per second dealt to enemies inside the zone.")]
    public float baseDamagePerSecond = 3f;

    [Tooltip("Additional damage per second per 1 point of Psyche.")]
    public float damagePerSecondPerPsyche = 0.15f;

    [Header("Gravity Arrow — Cooldown")]
    [Tooltip("Minimum seconds between zone spawns regardless of how many arrows stick.")]
    public float baseCooldown = 5f;

    [Tooltip("Seconds of cooldown removed per 1 point of Cooldown stat.")]
    public float cooldownReducPerCooldown = 0.3f;

    [Tooltip("Minimum possible cooldown floor in seconds.")]
    public float minCooldown = 1f;

    // ================================================================

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private float lastZoneTime = -999f;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats    = mgr.GetComponent<PlayerStats>();
        mgr.OnArrowHitWall += OnArrowHitWall;
    }

    private void OnDestroy()
    {
        if (upgradeManager != null)
            upgradeManager.OnArrowHitWall -= OnArrowHitWall;
    }

    // ================================================================
    //  WALL-STICK EVENT
    // ================================================================

    private float GetCooldown()
    {
        float cd = playerStats != null ? playerStats.cooldown : 0f;
        return Mathf.Max(minCooldown, baseCooldown - cooldownReducPerCooldown * cd);
    }

    private void OnArrowHitWall(Vector3 position)
    {
        // Cooldown gate — one zone per X seconds
        if (Time.time - lastZoneTime < GetCooldown()) return;
        lastZoneTime = Time.time;

        if (gravityZonePrefab == null)
        {
            Debug.LogWarning("[UpgradeGravityArrow] Gravity Zone Prefab is not assigned!", this);
            return;
        }

        GameObject zoneGO = Instantiate(
            gravityZonePrefab,
            new Vector3(position.x, position.y, 0f),
            Quaternion.identity);

        GravityArrowZone zone = zoneGO.GetComponent<GravityArrowZone>();
        if (zone == null)
        {
            Debug.LogWarning("[UpgradeGravityArrow] Gravity Zone Prefab is missing GravityArrowZone component!", zoneGO);
            return;
        }

        // Add Psyche scaling on top of whatever the prefab has as its base pullSpeed
        float ps = playerStats != null ? playerStats.psyche : 0f;
        zone.pullSpeed += pullSpeedPerPsyche * ps;

        // Override damage-per-second with our computed value (overrides prefab default)
        zone.damagePerSecond = baseDamagePerSecond + damagePerSecondPerPsyche * ps;
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;

        float basePull = 0f;
        var zone = gravityZonePrefab?.GetComponent<GravityArrowZone>();
        if (zone != null) basePull = zone.pullSpeed;

        float totalPull  = basePull + pullSpeedPerPsyche * stats.psyche;
        float totalDPS   = baseDamagePerSecond + damagePerSecondPerPsyche * stats.psyche;
        float radius     = zone?.pullRadius ?? 4f;
        float dur        = zone?.duration   ?? 3f;
        float cd         = Mathf.Max(minCooldown, baseCooldown - cooldownReducPerCooldown * stats.cooldown);

        return $"Arrows stuck in surfaces create a pull zone ({radius:F0}m, {dur:F0}s).\n" +
               $"Enemies dragged toward the arrow at {PSY(totalPull, "F1")} u/s " +
               $"and take {PSY(totalDPS, "F1")} damage per second.\n" +
               $"Cooldown: {CD(cd)}s. Scales with <color=#FF66CC>Psyche</color> and <color=#44FFEE>Cooldown</color>.";
    }
}
