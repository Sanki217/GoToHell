using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Blood Arrow — a CURSE upgrade with a two-phase lifecycle.
///
/// PHASE 1 (curse active, 3-5 player levels):
///   Penalty:  each arrow costs 5 HP (HP floored at 1; player cannot die from shots).
///             Arrows are immediately restored so ammo never runs dry.
///   Bonus:    arrow damage is multiplied by (1 + 0.20 + 0.05 * attackDamage).
///
/// PHASE 2 (curse lifted, permanent):
///   Penalty ends — no more HP cost per shot.
///   Bonus stays — damage multiplier continues for the rest of the run.
///
/// bloodArrowMultiplier on PlayerStats is read by Arrow.cs every hit.
/// It is updated every frame so AttackDamage gains during the run are reflected immediately.
/// </summary>
public class UpgradeBloodArrow : PlayerUpgradeCurse
{
    [Header("Blood Arrow - Penalty")]
    [Tooltip("HP deducted per arrow fired while the curse is active.")]
    public int hpCostPerShot = 5;

    [Header("Blood Arrow - Bonus (permanent)")]
    [Tooltip("Flat damage multiplier bonus added on top of the 1.0 base. 0.20 = +20%.")]
    public float baseDamageBonus = 0.20f;

    [Tooltip("Additional damage multiplier bonus per 1 point of Attack Damage. 0.05 = +5% per AD.")]
    public float damagePerAttackDamage = 0.05f;

    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private PlayerShooting playerShooting;

    // ================================================================
    //  CURSE LIFECYCLE
    // ================================================================

    protected override void OnCurseActivated(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        playerHealth = mgr.GetComponent<PlayerHealth>();
        playerShooting = mgr.GetComponent<PlayerShooting>();

        // Subscribe to all three fire events to impose the HP cost
        mgr.OnWeakArrowFired += OnArrowFired;
        mgr.OnMediumArrowFired += OnArrowFired;
        mgr.OnChargedArrowFired += OnArrowFired;
    }

    protected override void OnCurseLifted()
    {
        // Unsubscribe from the penalty (HP cost) — bonus stays via Update()
        curseMgr.OnWeakArrowFired -= OnArrowFired;
        curseMgr.OnMediumArrowFired -= OnArrowFired;
        curseMgr.OnChargedArrowFired -= OnArrowFired;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // Always clean up if removed mid-run
        if (curseMgr != null)
        {
            curseMgr.OnWeakArrowFired -= OnArrowFired;
            curseMgr.OnMediumArrowFired -= OnArrowFired;
            curseMgr.OnChargedArrowFired -= OnArrowFired;
        }
        if (playerStats != null)
            playerStats.bloodArrowMultiplier = 1f;
    }

    // ================================================================
    //  UPDATE — keep damage multiplier live (responds to AD changes)
    // ================================================================

    private void Update()
    {
        if (playerStats == null) return;
        float bonus = baseDamageBonus + damagePerAttackDamage * playerStats.attackDamage;
        playerStats.bloodArrowMultiplier = 1f + bonus;
    }

    // ================================================================
    //  PENALTY — HP cost per shot (only active while curse is on)
    // ================================================================

    private void OnArrowFired(Vector3 dir, float _)
    {
        // Restore ammo so the player has unlimited arrows
        if (playerShooting != null)
            playerShooting.SetCurrentArrows(playerShooting.maxArrows);

        // Deduct HP — floor at 1 so player cannot die from their own shots
        if (playerHealth != null)
            playerHealth.SetHP(Mathf.Max(1, playerHealth.CurrentHP - hpCostPerShot));
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float bonusPct = (baseDamageBonus + damagePerAttackDamage * s.attackDamage) * 100f;
        return $"<color=#FF4466>[CURSE: {minCurseLevels}-{maxCurseLevels} levels]</color>\n" +
               $"Arrows cost {HP(hpCostPerShot, "F0")} HP. Unlimited ammo.\n" +
               $"Arrow damage {AD(bonusPct, "F0")}% bonus. Curse ends — bonus stays.";
    }
}
