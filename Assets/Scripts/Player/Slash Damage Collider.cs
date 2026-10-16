using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to the SlashCollider child alongside a BoxCollider (Is Trigger = true).
/// DoSlash() is called directly by PlayerSlash with explicit world-space geometry.
/// This bypasses the physics-sync timing issue where Physics.OverlapBox
/// called in OnEnable can't see just-moved transforms before the next physics step.
///
/// Hits every Enemy and every IDestructible (vase, barrel, …) overlapping the
/// slash box exactly once, regardless of how many colliders each one has.
/// The query covers all layers so the collision matrix never hides a target.
/// </summary>
public class Slash_Damage_Collider : MonoBehaviour
{
    private static readonly Collider[] buffer = new Collider[64];
    private readonly HashSet<int> hit = new HashSet<int>();

    /// <summary>
    /// Called directly by PlayerSlash immediately after computing slash geometry.
    /// Uses explicit world-space parameters — no dependency on physics sync.
    /// </summary>
    public void DoSlash(Vector3 center, Vector3 halfExtents, Quaternion rotation, PlayerSlash slash)
    {
        hit.Clear();

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, buffer, rotation,
                                               Physics.AllLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider col = buffer[i];
            buffer[i] = null;
            if (col == null) continue;

            // Never hit the player or any of its children
            if (col.transform.IsChildOf(slash.transform)) continue;

            // ── Enemy (collider may be on a child) ──────────────────
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponentInParent<Enemy>();
                if (enemy != null && hit.Add(enemy.GetInstanceID()))
                    slash.OnSlashHitEnemy(enemy);
                continue;
            }

            // ── Destructibles (vase, barrel, …) ─────────────────────
            IDestructible destructible = CombatQuery.DestructibleOf(col);
            if (destructible is Object uo && hit.Add(uo.GetInstanceID()))
                slash.OnSlashHitDestructible(destructible);
        }
    }
}
