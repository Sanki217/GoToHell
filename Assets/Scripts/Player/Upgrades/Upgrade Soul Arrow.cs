using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Soul Arrow upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: when an arrow kills an enemy, a chain copy is fired from that enemy's
/// position toward the nearest other enemy within chainRange. The copy deals
/// chainDamageMultiplier (default 50%) of normal arrow damage and does NOT chain again.
///
/// Hooks into PlayerUpgradeManager.OnArrowKill, which is fired by Arrow.cs only
/// for non-chain arrows, preventing infinite chaining.
/// </summary>
public class UpgradeSoulArrow : PlayerUpgrade
{
    [Header("Soul Arrow - Tuning")]
    [Tooltip("Maximum distance from the kill position to search for the next target.")]
    public float chainRange = 12f;

    [Tooltip("Fraction of normal arrow damage the chain copy deals. 0.5 = 50%.")]
    public float chainDamageMultiplier = 0.5f;

    private PlayerShooting shooting;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        shooting = mgr.GetComponent<PlayerShooting>();

        mgr.OnArrowKill += OnArrowKill;
    }

    private void OnArrowKill(GameObject killedEnemy)
    {
        if (killedEnemy == null || shooting == null) return;

        Vector3 killPos = killedEnemy.transform.position;

        Enemy nearest = FindNearestEnemy(killPos, killedEnemy);
        if (nearest == null) return;

        Vector3 dir = nearest.transform.position - killPos;
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir = dir.normalized;

        // Prefer the same tier as the last shot; fall back gracefully
        GameObject prefab = shooting.lightArrowPrefab
                         ?? shooting.mainArrowPrefab;
        if (prefab == null) return;

        GameObject arrowGO = Instantiate(prefab, killPos, Quaternion.identity);
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow == null) { Destroy(arrowGO); return; }

        arrow.Initialize(dir, shooting.stickableLayers);
        arrow.speed = shooting.baseArrowSpeed;
        arrow.damageMultiplier = chainDamageMultiplier;
        arrow.isChainCopy = true;

        // Notify other upgrades an extra arrow was spawned (e.g. stat tracking)
        upgradeManager?.FireExtraArrow(dir, shooting.baseArrowSpeed);
    }

    private Enemy FindNearestEnemy(Vector3 from, GameObject exclude)
    {
        Enemy nearest = null;
        float bestDistSq = chainRange * chainRange;

#if UNITY_6000_0_OR_NEWER
        var enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
#else
        var enemies = Object.FindObjectsOfType<Enemy>();
#endif

        foreach (Enemy e in enemies)
        {
            if (e == null || e.gameObject == exclude) continue;
            Vector3 delta = e.transform.position - from;
            delta.z = 0f;
            float distSq = delta.sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                nearest = e;
            }
        }
        return nearest;
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        int pct = Mathf.RoundToInt(chainDamageMultiplier * 100f);
        return $"Arrows that kill an enemy fire a chain shot at the nearest enemy " +
               $"within {chainRange:F0}m, dealing {AD(pct, "F0")}% damage. Does not chain again.";
    }

    private void OnDestroy()
    {
        if (upgradeManager != null)
            upgradeManager.OnArrowKill -= OnArrowKill;
    }
}
