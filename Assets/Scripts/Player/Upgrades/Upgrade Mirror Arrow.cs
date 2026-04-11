using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mirror Arrow upgrade.
///
/// Every arrow you fire simultaneously spawns a free mirror copy in the
/// X-reflected direction. If you shoot bottom-left, the mirror flies bottom-right.
/// Only the X component is flipped — Y is preserved, so angled shots stay angled.
///
/// Mirror deals 75% of arrow damage (base), scaling upward with Ability Power.
/// It is a chain copy (no ammo cost, does not trigger further mirror effects,
/// does not consume nextArrowDamageMultiplier — e.g. First Strike only applies once).
/// </summary>
public class UpgradeMirrorArrow : PlayerUpgrade
{
    [Header("Mirror Arrow — Tuning")]
    [Tooltip("Base damage fraction the mirror arrow deals. 0.75 = 75% of the original.")]
    public float baseDamage = 0.75f;

    [Tooltip("Additional damage fraction per 1 point of Ability Power. " +
             "e.g. 0.02 = +2% per AP, reaching 1.0× at AP 12.5.")]
    public float damagePerAP = 0.02f;

    [Header("Mirror Arrow — Prefab")]
    [Tooltip("Optional separate prefab for mirror arrows (different visuals, same mechanics). " +
             "If left empty, the same prefab as the fired arrow is used.")]
    public GameObject mirrorArrowPrefab;

    // ================================================================

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerShooting shooting;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats    = mgr.GetComponent<PlayerStats>();
        shooting       = mgr.GetComponent<PlayerShooting>();

        mgr.OnWeakArrowFired    += OnWeak;
        mgr.OnMediumArrowFired  += OnMedium;
        mgr.OnChargedArrowFired += OnCharged;
    }

    private void OnDestroy()
    {
        if (upgradeManager == null) return;
        upgradeManager.OnWeakArrowFired    -= OnWeak;
        upgradeManager.OnMediumArrowFired  -= OnMedium;
        upgradeManager.OnChargedArrowFired -= OnCharged;
    }

    // ================================================================
    //  FIRE EVENT HANDLERS
    // ================================================================

    private void OnWeak(Vector3 dir, float speedMult)
        => SpawnMirror(dir, speedMult, chargeNorm: 0f, ArrowFireType.Weak,
                       shooting?.lightArrowPrefab ?? shooting?.mainArrowPrefab);

    private void OnMedium(Vector3 dir, float chargeNorm)
    {
        float speedMult = shooting != null
            ? Mathf.Lerp(1f, shooting.maxChargeMultiplier, chargeNorm)
            : chargeNorm;
        SpawnMirror(dir, speedMult, chargeNorm, ArrowFireType.Medium,
                    shooting?.mediumArrowPrefab ?? shooting?.mainArrowPrefab);
    }

    private void OnCharged(Vector3 dir, float speedMult)
        => SpawnMirror(dir, speedMult, chargeNorm: 1f, ArrowFireType.Charged,
                       shooting?.strongArrowPrefab ?? shooting?.mainArrowPrefab);

    // ================================================================
    //  MIRROR SPAWN
    // ================================================================

    private void SpawnMirror(Vector3 dir, float speedMult, float chargeNorm,
                              ArrowFireType fireType, GameObject prefab)
    {
        if (shooting == null || prefab == null) return;

        // Reflect X, preserve Y — mirror is purely horizontal
        Vector3 mirrorDir = new Vector3(-dir.x, dir.y, 0f);
        if (mirrorDir.sqrMagnitude < 0.001f) mirrorDir = Vector3.right;
        mirrorDir = mirrorDir.normalized;

        // Use the dedicated mirror prefab if assigned, otherwise fall back to the original.
        GameObject spawnPrefab = mirrorArrowPrefab != null ? mirrorArrowPrefab : prefab;
        GameObject arrowGO = Instantiate(spawnPrefab, shooting.shootPoint.position, Quaternion.identity);
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow == null) { Destroy(arrowGO); return; }

        // Mark as chain copy so it doesn't consume nextArrowDamageMultiplier
        // (First Strike bonus stays on the original arrow only)
        arrow.isChainCopy = true;
        arrow.Initialize(mirrorDir, shooting.stickableLayers);
        arrow.speed        = shooting.baseArrowSpeed * speedMult;
        arrow.fireType     = fireType;
        arrow.chargeAmount = chargeNorm;
        arrow.chargeDamageMultiplierPerPercent = shooting.chargeDamageMultiplierPerPercent;

        // Apply mirror damage fraction
        float ap = playerStats != null ? playerStats.abilityPower : 0f;
        arrow.damageMultiplier = baseDamage + damagePerAP * ap;

        // Mirror arrows self-destruct after 3 seconds — they are never pickable
        // (isChainCopy = true means ArrowPickup.OnArrowLanded is never called, so
        //  canPickUp stays false. The Destroy ensures they don't linger indefinitely.)
        Destroy(arrowGO, 3f);

        upgradeManager?.FireExtraArrow(mirrorDir, speedMult);
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float dmgPct = (baseDamage + damagePerAP * s.abilityPower) * 100f;
        return $"Every arrow fires a free mirror copy in the X-reflected direction simultaneously.\n" +
               $"Mirror deals {AP(dmgPct, "F0")}% of the original arrow's damage.\n" +
               $"Scales with <color=#4488FF>Ability Power</color>.";
    }
}
