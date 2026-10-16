using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary>
/// Enemy Explosion upgrade.
/// Effect: enemies explode on death, dealing AoE damage with falloff.
/// Final damage  = damage + damageAP × Psyche
/// Final radius  = radius + radiusSize × Size
///
/// No chain reactions: an enemy killed BY an explosion does not explode
/// itself (victims are remembered for the current frame).
/// </summary>
public class UpgradeEnemyExplosion : PlayerUpgrade
{
    [Header("Enemy Explosion — Tuning")]
    public float damage = 15f;
    public float damageAP = 1.0f;
    public float radius = 3f;
    public float radiusSize = 0.10f;
    public float debugVisualDuration = 0.25f;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    // Enemies hit by an explosion this frame — their deaths don't explode
    private readonly HashSet<int> victims = new HashSet<int>();
    private int victimsFrame = -1;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats = mgr.GetComponent<PlayerStats>();
        mgr.OnEnemyKilled += OnEnemyKilled;
    }

    private void OnDestroy()
    {
        if (upgradeManager != null) upgradeManager.OnEnemyKilled -= OnEnemyKilled;
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        var s = Simulate(stats, simulatedBonuses);
        float dmg = damage + damageAP * s.psyche;
        float rad = radius + radiusSize * s.size;
        return $"Enemies explode on death dealing {PSY(dmg)} damage in {SZ(rad)} radius.\n" +
               $"Scales with <color=#FF66CC>Psyche</color> and <color=#AAAAAA>Size</color>.";
    }

    private float GetDamage() =>
        damage + damageAP * (playerStats != null ? playerStats.psyche : 0f);

    private float GetRadius() =>
        radius + radiusSize * (playerStats != null ? playerStats.size : 0f);

    private void OnEnemyKilled(GameObject enemyGO)
    {
        if (enemyGO == null) return;

        if (victimsFrame != Time.frameCount)
        {
            victims.Clear();
            victimsFrame = Time.frameCount;
        }
        if (victims.Contains(enemyGO.GetInstanceID())) return;   // killed by an explosion — no chain

        float dmg = GetDamage();
        float rad = GetRadius();
        Vector3 pos = enemyGO.transform.position;

        DebugShapes.Sphere(pos, rad, new Color(1f, 0.1f, 0.1f, 0.35f), debugVisualDuration);

        float kb = playerStats != null ? playerStats.knockbackForce : 0f;

        List<Enemy> hits = ListPool<Enemy>.Get();
        CombatQuery.OverlapEnemies(pos, rad, hits);
        foreach (Enemy nearby in hits)
        {
            if (nearby == null || nearby.IsDead || nearby.gameObject == enemyGO) continue;

            victims.Add(nearby.gameObject.GetInstanceID());

            float dist = Vector3.Distance(pos, nearby.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / rad);
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg * falloff));
            Vector3 dir = CombatQuery.PlanarDirection(pos, nearby.transform.position, Vector3.right);

            GameObject nearbyGO = nearby.gameObject;
            nearby.TakeDamage(rounded, pos, dir, kb, false, FloatingTextManager.HitType.Normal);
            if (playerStats != null)
                playerStats.RecordDamageDealt(dmg * falloff, DamageSource.Explosion, nearbyGO);
        }
        ListPool<Enemy>.Release(hits);

        List<IDestructible> breakables = ListPool<IDestructible>.Get();
        CombatQuery.OverlapDestructibles(pos, rad, breakables);
        foreach (IDestructible d in breakables)
            d.TakeDamage(Mathf.RoundToInt(dmg));
        ListPool<IDestructible>.Release(breakables);

        FXManager.Play(ActionFX.EnemyDeath, pos);
    }

    /// <summary>Kept for existing callers — use DebugShapes.GetMaterial for new code.</summary>
    public static Material MakeTransparentMaterial(Color color) => DebugShapes.GetMaterial(color);
}
