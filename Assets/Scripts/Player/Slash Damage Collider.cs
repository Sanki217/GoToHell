using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to the SlashCollider child alongside a BoxCollider (Is Trigger = true).
/// DoSlash() is called directly by PlayerSlash with explicit world-space geometry.
/// This bypasses the physics-sync timing issue where Physics.OverlapBox
/// called in OnEnable can't see just-moved transforms before the next physics step.
/// </summary>
public class Slash_Damage_Collider : MonoBehaviour
{
    /// <summary>
    /// Called directly by PlayerSlash immediately after computing slash geometry.
    /// Uses explicit world-space parameters — no dependency on physics sync.
    /// </summary>
    public void DoSlash(Vector3 center, Vector3 halfExtents, Quaternion rotation,
                        PlayerSlash slash)
    {
        var hit = new HashSet<int>(); // instance IDs already processed

        Collider[] overlaps = Physics.OverlapBox(
            center, halfExtents, rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        foreach (var col in overlaps)
        {
            if (col == null) continue;

            // Never hit the player or any of its children
            if (col.transform.IsChildOf(slash.transform)) continue;

            GameObject go = col.gameObject;

            // ── Enemy ────────────────────────────────────────────────
            if (go.CompareTag("Enemy"))
            {
                // Go to root Enemy component (collider may be on child)
                Enemy enemy = go.GetComponentInParent<Enemy>() ?? go.GetComponent<Enemy>();
                if (enemy == null) continue;
                int id = enemy.gameObject.GetInstanceID();
                if (hit.Contains(id)) continue;
                hit.Add(id);
                slash.OnSlashHitEnemy(enemy.gameObject);
                continue;
            }

            // ── Vase ─────────────────────────────────────────────────
            Vase vase = go.GetComponentInParent<Vase>() ?? go.GetComponent<Vase>();
            if (vase != null)
            {
                int id = vase.gameObject.GetInstanceID();
                if (hit.Contains(id)) continue;
                hit.Add(id);
                slash.OnSlashHitDestructible(vase, null);
                continue;
            }

            // ── Explosive Barrel ─────────────────────────────────────
            ExplosiveBarrel barrel = go.GetComponentInParent<ExplosiveBarrel>()
                                  ?? go.GetComponent<ExplosiveBarrel>();
            if (barrel != null)
            {
                int id = barrel.gameObject.GetInstanceID();
                if (hit.Contains(id)) continue;
                hit.Add(id);
                slash.OnSlashHitDestructible(null, barrel);
            }
        }
    }
}