using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phantom Step upgrade.
///
/// Replaces the physics-based dash with an instant teleport to your cursor
/// (max dash range still applies). You pass through enemies and thin walls.
/// On landing, an AoE explosion damages all enemies within radius.
///
/// STATS THAT SCALE:
///   • Explosion damage  — base + Ability Power scaling
///   • Explosion radius  — base + Size scaling
///
/// UNCHANGED from normal dash:
///   • Dash cost (reduced by Psyche)
///   • Invincibility window (boosted by Luck)
///   • Dash range (boosted by Agility)
///
/// SETUP:
///   1. Assign this upgrade to its orb prefab as usual.
///   2. Optionally assign an Explosion Effect Prefab (particles, etc.) — spawned at landing.
///   3. Tune baseRadius, baseExplosionDamage, and the per-stat scaling below.
/// </summary>
public class UpgradePhantomStep : PlayerUpgrade
{
    [Header("Phantom Step — Explosion")]
    [Tooltip("Base AoE explosion radius in world units.")]
    public float baseRadius = 3f;

    [Tooltip("Extra radius per 1 point of Size.")]
    public float radiusPerSize = 0.15f;

    [Tooltip("Base explosion damage.")]
    public float baseExplosionDamage = 20f;

    [Tooltip("Extra damage per 1 point of Ability Power.")]
    public float damagePerAP = 1.5f;

    [Tooltip("Optional VFX prefab spawned at the landing position on teleport.")]
    public GameObject explosionEffectPrefab;

    [Header("Phantom Step — Layer Mask")]
    [Tooltip("Layers that block the AoE damage sphere. Normally just the Enemy layer.")]
    public LayerMask damageableLayers;

    // ================================================================
    //  REFERENCES
    // ================================================================

    private DashAbility dashAbility;
    private PlayerStats playerStats;

    // ================================================================
    //  SETUP
    // ================================================================

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats  = mgr.GetComponent<PlayerStats>();
        dashAbility  = mgr.GetComponent<DashAbility>();

        if (dashAbility != null)
        {
            dashAbility.isTeleportDash   = true;
            dashAbility.OnTeleportLanded += OnLanded;
        }
        else
        {
            Debug.LogWarning("[UpgradePhantomStep] DashAbility not found on player!", this);
        }
    }

    private void OnDestroy()
    {
        if (dashAbility != null)
        {
            dashAbility.isTeleportDash   = false;
            dashAbility.OnTeleportLanded -= OnLanded;
        }
    }

    // ================================================================
    //  TELEPORT LANDING
    // ================================================================

    private void OnLanded(Vector3 landPosition)
    {
        float radius = GetRadius();
        float damage = GetDamage();

        // AoE damage — hit all enemies within radius at landing position
        Collider[] hits = Physics.OverlapSphere(landPosition, radius);
        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponent<Enemy>()
                          ?? col.transform.root.GetComponent<Enemy>();
            if (enemy == null) continue;

            Vector3 kbDir = (enemy.transform.position - landPosition);
            kbDir.z = 0f;
            if (kbDir.sqrMagnitude < 0.001f) kbDir = Vector3.right;
            kbDir = kbDir.normalized;

            float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;

            enemy.TakeDamage(
                Mathf.Max(1, Mathf.RoundToInt(damage)),
                landPosition,
                kbDir,
                kbForce,
                false,
                FloatingTextManager.HitType.Normal);

            playerStats?.RecordDamageDealt(damage, DamageSource.Explosion, enemy.gameObject);
        }

        // Spawn VFX if assigned
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab,
                        new Vector3(landPosition.x, landPosition.y, 0f),
                        Quaternion.identity);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private float GetRadius()
    {
        float sz = playerStats != null ? playerStats.size : 0f;
        return baseRadius + radiusPerSize * sz;
    }

    private float GetDamage()
    {
        float ps = playerStats != null ? playerStats.psyche : 0f;
        return baseExplosionDamage + damagePerAP * ps;
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        if (stats == null) return description;
        float radius = baseRadius + radiusPerSize * stats.size;
        float damage = baseExplosionDamage + damagePerAP * stats.psyche;

        return $"Dash teleports you instantly to your cursor (max range applies).\n" +
               $"Pass through enemies and thin walls.\n" +
               $"Explode on landing — {PSY(damage, "F0")} damage in a {SZ(radius, "F1")}m radius.\n" +
               $"Scales with <color=#FF66CC>Psyche</color> (damage) " +
               $"and <color=#AAAAAA>Size</color> (radius).";
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Preview the explosion radius in Scene view
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.18f);
        Gizmos.DrawSphere(transform.position, baseRadius);
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, baseRadius);
    }
#endif
}
