using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mirror Arrow upgrade.
///
/// Every Nth arrow fired (default 4) triggers a free phantom copy of the
/// PREVIOUS arrow — same direction, same charge level — fired from the
/// player's shoot point after a short delay that scales with Cooldown.
///
/// The phantom arrow costs no ammo, does not trigger Mirror Arrow again
/// (isChainCopy = true prevents it from counting toward the N-counter),
/// and fires from the current shoot-point position (not the original position).
/// </summary>
public class UpgradeMirrorArrow : PlayerUpgrade
{
    [Header("Mirror Arrow — Tuning")]
    [Tooltip("Fire a phantom copy every N arrows.")]
    public int mirrorEveryN = 4;

    [Tooltip("Base delay in seconds before the phantom copy fires.")]
    public float baseDelay = 0.4f;

    [Tooltip("Reduction applied to delay per 1 point of Cooldown stat. " +
             "e.g. 0.05 = −5% per point.")]
    public float cooldownDelayReductionPerPoint = 0.05f;

    [Tooltip("Minimum delay regardless of Cooldown.")]
    public float minDelay = 0.05f;

    // ================================================================
    //  STATE
    // ================================================================

    private int arrowsFired = 0;

    // Data for the most recently fired arrow (becomes the mirror template on trigger)
    private struct ArrowSnapshot
    {
        public Vector3 dir;
        public float   chargeNormalized;  // used for damage calculation
        public float   speedMultiplier;   // used for projectile speed
        public ArrowFireType fireType;
    }
    private ArrowSnapshot previous;
    private bool hasPrevious = false;

    // References
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
        => HandleFire(dir, chargeNorm: 0.1f, speedMult, ArrowFireType.Weak);

    private void OnMedium(Vector3 dir, float chargeNorm)
        => HandleFire(dir, chargeNorm, speedMult: chargeNorm, ArrowFireType.Medium);

    private void OnCharged(Vector3 dir, float speedMult)
        => HandleFire(dir, chargeNorm: 1f, speedMult, ArrowFireType.Charged);

    private void HandleFire(Vector3 dir, float chargeNorm, float speedMult, ArrowFireType type)
    {
        arrowsFired++;

        // On every Nth arrow: spawn mirror of the PREVIOUS arrow
        if (arrowsFired % mirrorEveryN == 0 && hasPrevious)
            StartCoroutine(SpawnMirror(previous, GetDelay()));

        // Store THIS arrow as the new "previous" for the next trigger
        previous = new ArrowSnapshot
        {
            dir             = dir,
            chargeNormalized = chargeNorm,
            speedMultiplier = speedMult,
            fireType        = type,
        };
        hasPrevious = true;
    }

    // ================================================================
    //  MIRROR SPAWN
    // ================================================================

    private float GetDelay()
    {
        if (playerStats == null) return baseDelay;
        float reduction = playerStats.cooldown * cooldownDelayReductionPerPoint;
        return Mathf.Max(minDelay, baseDelay * (1f - reduction));
    }

    private IEnumerator SpawnMirror(ArrowSnapshot snap, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (shooting == null) yield break;

        GameObject prefab = snap.fireType switch
        {
            ArrowFireType.Weak    => shooting.lightArrowPrefab  ?? shooting.mainArrowPrefab,
            ArrowFireType.Medium  => shooting.mediumArrowPrefab ?? shooting.mainArrowPrefab,
            ArrowFireType.Charged => shooting.strongArrowPrefab ?? shooting.mainArrowPrefab,
            _                    => shooting.mainArrowPrefab,
        };
        if (prefab == null) yield break;

        // Spawn from current shoot-point (player may have moved — feels dynamic)
        GameObject arrowGO = Instantiate(prefab, shooting.shootPoint.position, Quaternion.identity);
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow == null) { Destroy(arrowGO); yield break; }

        arrow.isChainCopy = true;              // don't count toward the mirror counter
        arrow.Initialize(snap.dir, shooting.stickableLayers);
        arrow.speed        = shooting.baseArrowSpeed * snap.speedMultiplier;
        arrow.fireType     = snap.fireType;
        arrow.chargeAmount = snap.chargeNormalized;
        arrow.chargeDamageMultiplierPerPercent = shooting.chargeDamageMultiplierPerPercent;

        upgradeManager?.FireExtraArrow(snap.dir, snap.speedMultiplier);
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float reduction = s.cooldown * cooldownDelayReductionPerPoint;
        float delay = Mathf.Max(minDelay, baseDelay * (1f - reduction));
        return $"Every {mirrorEveryN}th arrow fired spawns a free copy of the " +
               $"PREVIOUS arrow after {CD(delay, "F2")}s.\n" +
               $"Same direction and charge level. Scales with <color=#AAAAFF>Cooldown</color>.";
    }
}
