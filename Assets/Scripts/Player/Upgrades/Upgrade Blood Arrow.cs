using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Arrow upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect:
///   - Arrows cost HP instead of ammo. Each shot costs hpCostPerShot (default 3).
///   - The player effectively has unlimited arrows (ammo is restored immediately after each shot).
///   - HP cost is clamped to min 1 HP — the player cannot die from shooting.
///   - Below lowHPThreshold (default 30%) of max HP, arrow damage is doubled.
///
/// bloodArrowMultiplier on PlayerStats is read by Arrow.cs to scale damage.
/// It is updated every frame so the threshold responds to real-time HP changes.
/// </summary>
public class UpgradeBloodArrow : PlayerUpgrade
{
    [Header("Blood Arrow - Tuning")]
    [Tooltip("HP deducted per arrow fired.")]
    public int hpCostPerShot = 3;

    [Tooltip("HP fraction below which arrow damage is doubled. 0.3 = 30%.")]
    public float lowHPThreshold = 0.30f;

    [Tooltip("Damage multiplier applied when below the HP threshold.")]
    public float lowHPDamageMultiplier = 2f;

    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private PlayerShooting playerShooting;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats = mgr.GetComponent<PlayerStats>();
        playerHealth = mgr.GetComponent<PlayerHealth>();
        playerShooting = mgr.GetComponent<PlayerShooting>();

        // Subscribe to all arrow fire events to pay HP and restore ammo
        mgr.OnWeakArrowFired += OnAnyArrowFired;
        mgr.OnMediumArrowFired += OnAnyArrowFired;
        mgr.OnChargedArrowFired += OnAnyArrowFired;
    }

    private void Update()
    {
        if (playerStats == null || playerHealth == null) return;

        // Keep the multiplier live so it responds immediately to HP changes
        bool isLowHP = playerHealth.CurrentHP < playerHealth.maxHP * lowHPThreshold;
        playerStats.bloodArrowMultiplier = isLowHP ? lowHPDamageMultiplier : 1f;
    }

    private void OnAnyArrowFired(Vector3 dir, float _)
    {
        // Restore ammo so the player never runs dry
        if (playerShooting != null)
            playerShooting.SetCurrentArrows(playerShooting.maxArrows);

        // Deduct HP (floor at 1 — player can't die from their own shots)
        if (playerHealth != null)
        {
            int newHP = Mathf.Max(1, playerHealth.CurrentHP - hpCostPerShot);
            playerHealth.SetHP(newHP);
        }
    }

    // OnExtraArrowFired uses a different signature — wire it up separately if needed.
    // For now, the extra arrow (from upgrades) does not cost HP, which keeps things fair.

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        int thresholdPct = Mathf.RoundToInt(lowHPThreshold * 100f);
        return $"Arrows cost {HP(hpCostPerShot, "F0")} HP instead of ammo. Unlimited arrows.\n" +
               $"Below {thresholdPct}% HP, arrow damage is {AD(lowHPDamageMultiplier, "F0")}x.";
    }

    private void OnDestroy()
    {
        // Reset multiplier if upgrade is somehow removed
        if (playerStats != null)
            playerStats.bloodArrowMultiplier = 1f;

        if (upgradeManager != null)
        {
            upgradeManager.OnWeakArrowFired -= OnAnyArrowFired;
            upgradeManager.OnMediumArrowFired -= OnAnyArrowFired;
            upgradeManager.OnChargedArrowFired -= OnAnyArrowFired;
        }
    }
}
