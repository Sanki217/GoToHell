using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anything breakable that isn't an Enemy (vases, barrels, future crates and
/// totems). Weapons and explosions look for this interface instead of
/// hard-coding every destructible type — a new breakable is one component
/// that implements it, with zero changes to arrows, slashes or explosions.
/// </summary>
public interface IDestructible
{
    void TakeDamage(int amount);
}

/// <summary>
/// Shared physics queries for combat. Every AoE in the game used to run its own
/// OverlapSphere and walk the results by hand — and an enemy with two colliders
/// (body + contact trigger) got hit twice. These helpers return each Enemy /
/// IDestructible exactly once, without per-call array allocations.
///
/// Results go into caller-owned lists (use UnityEngine.Pool.ListPool) so the
/// queries are re-entrant: an explosion that kills an enemy can trigger another
/// explosion while the first one is still iterating its results.
/// </summary>
public static class CombatQuery
{
    private static readonly Collider[] buffer = new Collider[256];
    private static readonly HashSet<int> seen = new HashSet<int>();

    // ================================================================
    //  ENEMIES
    // ================================================================

    /// <summary>Unique, alive enemies with any collider inside the sphere.</summary>
    public static void OverlapEnemies(Vector3 center, float radius, List<Enemy> results,
                                      bool combatantsOnly = false)
    {
        results.Clear();
        int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, Physics.AllLayers,
                                                  QueryTriggerInteraction.Collide);
        seen.Clear();
        for (int i = 0; i < count; i++)
        {
            Enemy e = buffer[i].GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            if (combatantsOnly && !e.IsCombatant) continue;
            if (seen.Add(e.GetInstanceID())) results.Add(e);
        }
        ClearBuffer(count);
    }

    /// <summary>
    /// Living combatant enemies within <paramref name="range"/> of a point
    /// (gameplay-plane distance), nearest first. Uses EnemyRegistry — no physics.
    /// </summary>
    public static void EnemiesInRangeSorted(Vector3 from, float range, List<Enemy> results,
                                            Enemy exclude = null)
    {
        results.Clear();
        float rangeSq = range * range;
        var all = EnemyRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            Enemy e = all[i];
            if (e == null || e == exclude || e.IsDead || !e.IsCombatant) continue;
            Vector3 d = e.transform.position - from;
            d.z = 0f;
            if (d.sqrMagnitude <= rangeSq) results.Add(e);
        }

        results.Sort((a, b) =>
        {
            Vector3 da = a.transform.position - from; da.z = 0f;
            Vector3 db = b.transform.position - from; db.z = 0f;
            return da.sqrMagnitude.CompareTo(db.sqrMagnitude);
        });
    }

    // ================================================================
    //  DESTRUCTIBLES
    // ================================================================

    /// <summary>Unique destructibles (vases, barrels…) with any collider inside the sphere.</summary>
    public static void OverlapDestructibles(Vector3 center, float radius, List<IDestructible> results,
                                            Object exclude = null)
    {
        results.Clear();
        int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, Physics.AllLayers,
                                                  QueryTriggerInteraction.Collide);
        seen.Clear();
        for (int i = 0; i < count; i++)
        {
            IDestructible d = buffer[i].GetComponentInParent<IDestructible>();
            if (d == null) continue;
            if (d is Object uo)
            {
                if (uo == null || uo == exclude) continue;
                if (!seen.Add(uo.GetInstanceID())) continue;
            }
            results.Add(d);
        }
        ClearBuffer(count);
    }

    /// <summary>The destructible a collider belongs to, if any.</summary>
    public static IDestructible DestructibleOf(Collider col)
    {
        if (col == null) return null;
        IDestructible d = col.GetComponentInParent<IDestructible>();
        return d is Object uo && uo == null ? null : d;
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>Planar (z = 0) knockback direction from a source to a target, never zero.</summary>
    public static Vector3 PlanarDirection(Vector3 from, Vector3 to, Vector3 fallback)
    {
        Vector3 d = to - from;
        d.z = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : fallback;
    }

    private static void ClearBuffer(int count)
    {
        // Don't keep dead references alive in the static buffer
        for (int i = 0; i < count; i++) buffer[i] = null;
    }
}
