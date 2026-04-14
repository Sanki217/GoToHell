using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dead Man's Hand upgrade.
///
/// While you have exactly 1 arrow in your quiver, that arrow always crits
/// (uses normal crit multiplier from AttackDamage). If it kills its target,
/// the arrow flies back to your hand — you can fire again immediately
/// without reloading.
///
/// RHYTHM: fire until 1 arrow remains → it crits → kill → arrow flies back → repeat.
///
/// Implementation:
///   Update() monitors CurrentArrows. When == 1, playerStats.nextArrowForceCrit
///   is set to true (and a local isPrimed flag mirrors this).
///   Arrow.Initialize() consumes nextArrowForceCrit into arrow.isForcedCrit.
///   On hit, isForcedCrit temporarily boosts critChance to 2f so
///   RollDamage() guarantees a crit.
///
///   When the primed arrow is fired (OnAnyArrowFired with isPrimed == true),
///   waitingForKill is set. On kill (OnArrowKill), the arrow is sucked back
///   via ArrowPickup.StartSuck(), which restores the quiver on arrival.
/// </summary>
public class UpgradeDeadMansHand : PlayerUpgrade
{
    // ================================================================
    //  STATE
    // ================================================================

    private bool isPrimed      = false;   // Update() has primed the forced crit
    private bool waitingForKill = false;  // primed arrow was fired, awaiting kill

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerShooting playerShooting;
    private Transform playerTransform;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager   = mgr;
        playerStats      = mgr.GetComponent<PlayerStats>();
        playerShooting   = mgr.GetComponent<PlayerShooting>();
        playerTransform  = mgr.transform;

        mgr.OnWeakArrowFired    += OnAnyArrowFired;
        mgr.OnMediumArrowFired  += OnAnyArrowFired;
        mgr.OnChargedArrowFired += OnAnyArrowFired;
        mgr.OnArrowPickedUp     += OnArrowPickedUp;
        mgr.OnArrowKill         += OnArrowKill;
    }

    private void OnDestroy()
    {
        if (upgradeManager == null) return;
        upgradeManager.OnWeakArrowFired    -= OnAnyArrowFired;
        upgradeManager.OnMediumArrowFired  -= OnAnyArrowFired;
        upgradeManager.OnChargedArrowFired -= OnAnyArrowFired;
        upgradeManager.OnArrowPickedUp     -= OnArrowPickedUp;
        upgradeManager.OnArrowKill         -= OnArrowKill;

        // Clean up any primed flags if upgrade is removed mid-action
        if (playerStats != null && (isPrimed || waitingForKill))
            playerStats.nextArrowForceCrit = false;
    }

    // ================================================================
    //  UPDATE — monitors arrow count
    // ================================================================

    private void Update()
    {
        if (playerShooting == null || playerStats == null) return;
        if (waitingForKill) return; // already fired the primed arrow, don't interfere

        bool shouldPrime = (playerShooting.CurrentArrows == 1);
        isPrimed = shouldPrime;
        playerStats.nextArrowForceCrit = shouldPrime;
    }

    // ================================================================
    //  EVENT HANDLERS
    // ================================================================

    // Called AFTER Arrow.Initialize has consumed nextArrowForceCrit.
    // If isPrimed was true, this was the forced-crit arrow — start waiting for kill.
    private void OnAnyArrowFired(Vector3 dir, float _)
    {
        if (!isPrimed) return;
        waitingForKill = true;
        isPrimed = false;
        // nextArrowForceCrit was already consumed by Arrow.Initialize()
    }

    // Called after the player picks up an arrow (quiver incremented).
    private void OnArrowPickedUp()
    {
        // If the player picked up a new arrow before the last-arrow kill landed, cancel the wait.
        // (The fly-back isn't needed if the quiver was already refilled externally.)
        if (waitingForKill)
        {
            waitingForKill = false;
            // isPrimed will be re-evaluated by Update() next frame
        }
    }

    // Called when a non-chain arrow kills an enemy.
    private void OnArrowKill(GameObject enemy)
    {
        if (!waitingForKill) return;
        waitingForKill = false;

        // Retrieve the arrow that made the kill
        Arrow lastArrow = playerStats != null ? playerStats.lastFiredArrow : null;

        if (lastArrow != null && lastArrow.gameObject != null)
        {
            ArrowPickup pickup = lastArrow.GetComponent<ArrowPickup>();
            if (pickup != null)
            {
                // Fly the arrow back — ArrowPickup.StartSuck handles RestoreArrow on arrival
                pickup.StartSuck(playerTransform);
                return;
            }
        }

        // Fallback: arrow was already destroyed (hit a barrel, etc.) — restore directly
        playerShooting?.RestoreArrow();
        upgradeManager?.ArrowPickedUp();
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        float critMult = stats.critMultiplier * 100f;
        return $"While you have exactly 1 arrow, it always crits ({AD(critMult, "F0")}% damage).\n" +
               $"If it kills its target, the arrow flies back to your hand — " +
               $"fire again immediately without reloading.\n" +
               $"Crit multiplier scales with <color=#FF4444>Attack Damage</color>.";
    }
}
