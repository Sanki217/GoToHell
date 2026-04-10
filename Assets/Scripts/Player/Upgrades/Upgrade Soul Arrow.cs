using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Soul Arrow upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect:
///   When an arrow kills an enemy, chain shots fire from that enemy's position.
///
///   Chain count = 1 + floor(Psyche / 10)
///     - Base: 1 chain shot.
///     - +1 chain shot per 10 Psyche points.
///
///   Each chain shot aims at the Nth closest enemy within chainRange.
///   If there are fewer enemies than chain shots, the target list cycles
///   (e.g. 3 shots and 2 enemies: shot 3 re-targets enemy 1).
///
///   Chain shot damage = 50% of arrow damage + 10% of Ability Power (flat).
///   Chain shots behave like normal arrows — they stick to walls and can crit,
///   but they do NOT trigger further chain reactions (isChainCopy = true).
///
/// Requires: Arrow.cs to have damageMultiplier, flatDamageBonus, isChainCopy fields.
///           PlayerUpgradeManager to have OnArrowKill event.
/// </summary>
public class UpgradeSoulArrow : PlayerUpgrade
{
    [Header("Soul Arrow - Tuning")]
    [Tooltip("Maximum distance from the kill position to search for chain targets.")]
    public float chainRange = 12f;

    [Tooltip("Fraction of base arrow damage the chain shot deals. 0.5 = 50%.")]
    public float chainDamageMultiplier = 0.5f;

    [Tooltip("Fraction of Ability Power added as flat damage to each chain shot.")]
    public float chainAPScaling = 0.10f;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerShooting shooting;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats = mgr.GetComponent<PlayerStats>();
        shooting = mgr.GetComponent<PlayerShooting>();

        mgr.OnArrowKill += OnArrowKill;
    }

    private void OnDestroy()
    {
        if (upgradeManager != null)
            upgradeManager.OnArrowKill -= OnArrowKill;
    }

    // ================================================================
    //  CHAIN REACTION
    // ================================================================

    private void OnArrowKill(GameObject killedEnemy)
    {
        if (killedEnemy == null || shooting == null) return;

        Vector3 killPos = killedEnemy.transform.position;

        // Collect all valid targets sorted by distance
        List<Enemy> targets = GetSortedTargets(killPos, killedEnemy);
        if (targets.Count == 0) return;

        // Chain count scales with Psyche: 1 base + 1 per 10 Psyche
        int chainCount = 1 + Mathf.FloorToInt(
            (playerStats != null ? playerStats.psyche : 0f) / 10f);

        for (int i = 0; i < chainCount; i++)
        {
            Enemy target = targets[i % targets.Count]; // cycle if fewer enemies than chains
            FireChainArrow(killPos, target);
        }
    }

    private void FireChainArrow(Vector3 fromPos, Enemy target)
    {
        if (target == null) return;

        Vector3 dir = target.transform.position - fromPos;
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir = dir.normalized;

        // Use light arrow prefab as the chain projectile (falls back to mainArrowPrefab)
        GameObject prefab = shooting.lightArrowPrefab ?? shooting.mainArrowPrefab;
        if (prefab == null) return;

        GameObject arrowGO = Instantiate(prefab, fromPos, Quaternion.identity);
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow == null) { Destroy(arrowGO); return; }

        arrow.Initialize(dir, shooting.stickableLayers);
        arrow.speed = shooting.baseArrowSpeed;
        arrow.fireType = ArrowFireType.Weak;
        arrow.chargeAmount = 0f;       // no charge bonus on chain copies
        arrow.damageMultiplier = chainDamageMultiplier;
        arrow.flatDamageBonus = playerStats != null
            ? playerStats.abilityPower * chainAPScaling
            : 0f;
        arrow.isChainCopy = true;      // prevents further chaining

        // Notify other upgrades an extra arrow was spawned (stat tracking, etc.)
        upgradeManager?.FireExtraArrow(dir, shooting.baseArrowSpeed);
    }

    // ================================================================
    //  TARGET SEARCH — sorted by distance from kill position
    // ================================================================

    private List<Enemy> GetSortedTargets(Vector3 from, GameObject exclude)
    {
        float rangeSq = chainRange * chainRange;
        var result = new List<(Enemy e, float distSq)>();

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in all)
        {
            if (e == null || e.gameObject == exclude) continue;
            Vector3 delta = e.transform.position - from;
            delta.z = 0f;
            float dsq = delta.sqrMagnitude;
            if (dsq <= rangeSq)
                result.Add((e, dsq));
        }

        result.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        var enemies = new List<Enemy>(result.Count);
        foreach (var pair in result) enemies.Add(pair.e);
        return enemies;
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        int chains = 1 + Mathf.FloorToInt(s.psyche / 10f);
        int dmgPct = Mathf.RoundToInt(chainDamageMultiplier * 100f);
        float apFlat = s.abilityPower * chainAPScaling;

        return $"Arrows that kill fire {AD(chains, "F0")} chain shot{(chains != 1 ? "s" : "")} " +
               $"at the closest enem{(chains != 1 ? "ies" : "y")} within {chainRange:F0}m.\n" +
               $"Deals {AD(dmgPct, "F0")}% arrow dmg + {AP(apFlat, "F1")} AP flat. No further chains.\n" +
               $"+1 chain per 10 <color=#FF66CC>Psyche</color>.";
    }
}
