using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gravity Arrow upgrade.
///
/// When an arrow sticks to a wall or surface, it spawns a pull zone at
/// that position for 3 seconds. Enemies within pullRadius units are slowly
/// dragged toward the arrow. Pull speed scales with Psyche.
///
/// COMBO: place arrows around an enemy to trap it — the competing pulls
/// cancel each other out and pin the enemy in place.
///
/// Requires GravityArrowZone.cs (auto-created at runtime, no prefab needed).
/// </summary>
public class UpgradeGravityArrow : PlayerUpgrade
{
    [Header("Gravity Arrow — Tuning")]
    [Tooltip("Radius of the pull field around each stuck arrow.")]
    public float pullRadius = 4f;

    [Tooltip("Base pull speed in units per second at the zone centre (falloff to edges).")]
    public float basePullSpeed = 1.5f;

    [Tooltip("Additional pull speed per 1 point of Psyche.")]
    public float pullSpeedPerPsyche = 0.12f;

    [Tooltip("How many seconds the pull zone lasts after the arrow sticks.")]
    public float zoneDuration = 3f;

    // ================================================================

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;

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
        float ps = playerStats != null ? playerStats.psyche : 0f;
        float speed = basePullSpeed + pullSpeedPerPsyche * ps;

        // Spawn zone as a new GameObject at the arrow's stuck position
        GameObject zoneGO = new GameObject("[GravityArrowZone]");
        zoneGO.transform.position = new Vector3(position.x, position.y, 0f);

        GravityArrowZone zone = zoneGO.AddComponent<GravityArrowZone>();
        zone.pullRadius = pullRadius;
        zone.pullSpeed  = speed;
        zone.duration   = zoneDuration;
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float speed = basePullSpeed + pullSpeedPerPsyche * s.psyche;
        return $"Arrows stuck in surfaces create a pull zone ({pullRadius:F0}m, {zoneDuration:F0}s).\n" +
               $"Enemies are dragged toward the arrow at {PSY(speed, "F1")} u/s.\n" +
               $"Surround an enemy with arrows to trap it. Scales with <color=#FF66CC>Psyche</color>.";
    }
}
