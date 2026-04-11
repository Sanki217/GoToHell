using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dead Man's Hand upgrade.
///
/// The LAST arrow in your quiver always crits (guaranteed crit roll, uses
/// normal crit multiplier from AttackDamage). If that last arrow kills its
/// target, it flies back to your hand — you can fire again immediately
/// without picking up a new arrow.
///
/// RHYTHM: fire all but one → last arrow gets forced crit → kill → arrow
///         flies back → repeat without reloading.
///
/// Implementation:
///   When CurrentArrows == 1 after any shot (meaning the NEXT fire will be
///   the last arrow), playerStats.nextArrowForceCrit is set to true.
///   Arrow.Initialize() consumes it into arrow.isForcedCrit and clears the flag.
///   On the hit, isForcedCrit temporarily boosts critChance to 2f so
///   RollDamage() guarantees a crit.
///
///   On kill (OnArrowKill), if the last-arrow flag was set, the arrow
///   (retrieved via playerStats.lastFiredArrow) is sucked back with
///   ArrowPickup.StartSuck(), which restores the quiver on arrival.
/// </summary>
public class UpgradeDeadMansHand : PlayerUpgrade
{
    [Header("Dead Man's Hand — Tuning")]
    [Tooltip("How many arrows must be remaining after a shot before the next shot gets the crit prime. " +
             "Default 1: fire when quiver has 2 → 1 remains → next (last) shot crits.")]
    public int critPrimeAtArrows = 1;

    // ================================================================
    //  STATE
    // ================================================================

    private bool waitingForKill = false;  // last arrow was fired with forced crit, awaiting kill

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
        if (playerStats != null && waitingForKill)
            playerStats.nextArrowForceCrit = false;
    }

    // ================================================================
    //  EVENT HANDLERS
    // ================================================================

    // Called AFTER Arrow.Initialize has consumed nextArrowForceCrit;
    // CurrentArrows is already decremented at this point.
    private void OnAnyArrowFired(Vector3 dir, float _)
    {
        if (playerShooting == null) return;

        int arrowsLeft = playerShooting.CurrentArrows;

        if (arrowsLeft == critPrimeAtArrows)
        {
            // The NEXT shot will be the last arrow — prime the forced crit
            if (playerStats != null)
                playerStats.nextArrowForceCrit = true;
            waitingForKill = true;
        }
        else if (arrowsLeft > critPrimeAtArrows)
        {
            // Player fired early — cancel any pending prime (e.g. quiver was refilled mid-prime)
            waitingForKill = false;
            if (playerStats != null)
                playerStats.nextArrowForceCrit = false;
        }
        // arrowsLeft == 0: last arrow was just fired; priming already happened, waitingForKill is true
    }

    // Called after the player picks up an arrow (quiver incremented).
    private void OnArrowPickedUp()
    {
        // If the player picked up a new arrow before the last-arrow kill landed, cancel the wait.
        // (The fly-back isn't needed if the quiver was already refilled externally.)
        if (waitingForKill)
        {
            waitingForKill = false;
            if (playerStats != null)
                playerStats.nextArrowForceCrit = false;
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

    //public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    //{
   //     var s = Simulate(stats, simulatedBonuses);
      //  float critMult = s.critMultiplier * 100f;
     //   return $"The last arrow in your quiver always crits ({AD(critMult, "F0")}% damage).\n" +
     //          $"If it kills its target, the arrow flies back to your hand — " +
       //        $"fire again immediately without reloading.\n" +
        //       $"Crit multiplier scales with <color=#FF8800>Attack Damage</color>.";
   // }
}
