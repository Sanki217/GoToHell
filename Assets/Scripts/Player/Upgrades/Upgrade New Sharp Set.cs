using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// New Sharp Set upgrade.
///
/// The FIRST arrow you fire after collecting any arrow into an empty quiver
/// deals bonus damage (base 3×, scales with Ability Power).
///
/// RHYTHM: fire your last arrow → quiver hits 0 → collect an arrow
///         → first shot = 3× damage → subsequent shots = normal.
///
/// Implementation detail:
///   When firstStrikeReady is set, playerStats.nextArrowDamageMultiplier is
///   primed. Arrow.Initialize() consumes it into arrow.damageMultiplier and
///   resets it to 1f, so only the very first spawned arrow gets the bonus.
///   Mirror Arrow copies are chain copies and skip this multiplier.
/// </summary>
public class UpgradeNewSharpSet : PlayerUpgrade
{
    [Header("New Sharp Set — Tuning")]
    [Tooltip("Base damage multiplier on the first arrow after a quiver refill.")]
    public float baseMultiplier = 3f;

    [Tooltip("Additional multiplier per 1 point of Ability Power.")]
    public float multiplierPerAP = 0.05f;

    // ================================================================
    //  STATE
    // ================================================================

    private bool quiverDepleted   = false;
    private bool firstStrikeReady = false;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerShooting playerShooting;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats    = mgr.GetComponent<PlayerStats>();
        playerShooting = mgr.GetComponent<PlayerShooting>();

        mgr.OnWeakArrowFired    += OnAnyArrowFired;
        mgr.OnMediumArrowFired  += OnAnyArrowFired;
        mgr.OnChargedArrowFired += OnAnyArrowFired;
        mgr.OnArrowPickedUp     += OnArrowPickedUp;
    }

    private void OnDestroy()
    {
        if (upgradeManager == null) return;
        upgradeManager.OnWeakArrowFired    -= OnAnyArrowFired;
        upgradeManager.OnMediumArrowFired  -= OnAnyArrowFired;
        upgradeManager.OnChargedArrowFired -= OnAnyArrowFired;
        upgradeManager.OnArrowPickedUp     -= OnArrowPickedUp;

        // Clean up if we primed the multiplier but the upgrade was removed before firing
        if (playerStats != null && firstStrikeReady)
            playerStats.nextArrowDamageMultiplier = 1f;
    }

    // ================================================================
    //  EVENT HANDLERS
    // ================================================================

    // Called (by all three fire events) AFTER Arrow.Initialize has already consumed
    // nextArrowDamageMultiplier — so we just clear the local ready flag here.
    private void OnAnyArrowFired(Vector3 dir, float _)
    {
        if (firstStrikeReady)
            firstStrikeReady = false;

        // Track quiver depletion — CurrentArrows is already decremented at this point
        if (playerShooting != null && playerShooting.CurrentArrows == 0)
            quiverDepleted = true;
    }

    // Called each time an arrow pickup is collected (after RestoreArrow increments count).
    private void OnArrowPickedUp()
    {
        if (!quiverDepleted) return;
        if (playerShooting == null || playerShooting.CurrentArrows <= 0) return;

        // Quiver just went from 0 → 1+: prime the bonus
        quiverDepleted   = false;
        firstStrikeReady = true;

        // Write into PlayerStats immediately so Arrow.Initialize picks it up
        // the moment PlayerShooting calls SpawnArrow (before fire events fire).
        if (playerStats != null)
            playerStats.nextArrowDamageMultiplier = GetMultiplier();
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private float GetMultiplier()
    {
        float ps = playerStats != null ? playerStats.psyche : 0f;
        return baseMultiplier + multiplierPerAP * ps;
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        float mult = baseMultiplier + multiplierPerAP * stats.psyche;
        return $"The first arrow after refilling an empty quiver deals {PSY(mult, "F1")}× damage.\n" +
               $"Subsequent arrows deal normal damage.\n" +
               $"Rhythm: fire all → collect → one big shot. Scales with <color=#FF66CC>Psyche</color>.";
    }
}
