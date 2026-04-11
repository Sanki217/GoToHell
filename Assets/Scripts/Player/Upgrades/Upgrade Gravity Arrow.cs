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

    // ================================================================

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;

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

    private void OnArrowHitWall(Vector3 position)
    {
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
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);

        float basePull = 0f;
        if (gravityZonePrefab != null)
        {
            var z = gravityZonePrefab.GetComponent<GravityArrowZone>();
            if (z != null) { basePull = z.pullSpeed; }
        }

        float totalPull  = basePull + pullSpeedPerPsyche * s.psyche;
        float radius     = gravityZonePrefab?.GetComponent<GravityArrowZone>()?.pullRadius ?? 4f;
        float dur        = gravityZonePrefab?.GetComponent<GravityArrowZone>()?.duration   ?? 3f;

        return $"Arrows stuck in surfaces create a pull zone ({radius:F0}m, {dur:F0}s).\n" +
               $"Enemies dragged toward the arrow at {PSY(totalPull, "F1")} u/s.\n" +
               $"Surround an enemy with arrows to trap it. Scales with <color=#FF66CC>Psyche</color>.";
    }
}
