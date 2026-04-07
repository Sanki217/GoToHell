using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to the SlashCollider child alongside a BoxCollider (Is Trigger = true).
/// DoSlash() is called directly by PlayerSlash with explicit world-space geometry.
/// This bypasses the physics-sync timing issue where Physics.OverlapBox
/// called in OnEnable can't see just-moved transforms before the next physics step.
/// 
/// SETUP — Destructibles layer:
///   1. In Inspector, assign the "Destructibles" layer index to destructiblesLayer.
///      (Right-click the field → you can also just leave it: the script also tries
///       LayerMask.GetMask("Destructibles") at runtime as a fallback.)
///   2. No Physics Layer Collision Matrix changes needed — this script queries
///      the Enemy layer + Destructibles layer explicitly, so the matrix is irrelevant.
/// </summary>
public class Slash_Damage_Collider : MonoBehaviour
{
    [Tooltip("Layer mask covering enemies. Default is fine if enemies are on Default layer.")]
    public LayerMask enemyLayer = ~0; // all layers fallback

    [Tooltip("Layer mask for destructibles (barrels, vases). Set to your 'Destructibles' layer.")]
    public LayerMask destructiblesLayer = ~0;

    private void Awake()
    {
        // Auto-resolve named layers at runtime so it works even if Inspector isn't set
        int destructIdx = LayerMask.NameToLayer("Destructibles");
        if (destructIdx >= 0)
            destructiblesLayer = (1 << destructIdx);

        int enemyIdx = LayerMask.NameToLayer("Enemy");
        if (enemyIdx >= 0)
            enemyLayer = (1 << enemyIdx);
    }

    /// <summary>
    /// Called directly by PlayerSlash immediately after computing slash geometry.
    /// Uses explicit world-space parameters — no dependency on physics sync.
    /// </summary>
    public void DoSlash(Vector3 center, Vector3 halfExtents, Quaternion rotation,
                        PlayerSlash slash)
    {
        var hit = new HashSet<int>(); // instance IDs already processed

        // Query enemies and destructibles in separate passes so each uses the
        // correct layer mask — this sidesteps any Physics Layer Collision Matrix
        // configuration that might exclude destructibles from the slash layer.
        LayerMask combinedMask = enemyLayer | destructiblesLayer | Physics.AllLayers;

        Collider[] overlaps = Physics.OverlapBox(
            center, halfExtents, rotation,
            combinedMask,
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