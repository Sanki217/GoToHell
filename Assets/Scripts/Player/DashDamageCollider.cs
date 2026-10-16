using UnityEngine;

/// <summary>
/// Put this script on a child GameObject of the Player that has a Trigger Collider.
/// Assign that child's Collider to DashAbility.dashDamageCollider.
/// This collider is enabled only during a dash, so it only deals damage then.
/// </summary>
public class DashDamageCollider : MonoBehaviour
{
    // These are populated automatically from the parent DashAbility
    private DashAbility dashAbility;
    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    private readonly System.Collections.Generic.HashSet<int> hitThisDash = new System.Collections.Generic.HashSet<int>();
    private int dashSerialSeen = -1;

    private void Start()
    {
        // Walk up to the player root to find components
        Transform root = transform.root;
        dashAbility = root.GetComponent<DashAbility>();
        playerStats = root.GetComponent<PlayerStats>();
        upgradeManager = root.GetComponent<PlayerUpgradeManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only damage enemies, only while dash ability is active
        if (dashAbility == null || !dashAbility.isDashing) return;
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        // One hit per enemy per dash, however many colliders it has
        if (dashSerialSeen != dashAbility.DashSerial)
        {
            dashSerialSeen = dashAbility.DashSerial;
            hitThisDash.Clear();
        }
        if (!hitThisDash.Add(enemy.GetInstanceID())) return;

        float baseDamage = dashAbility != null
            ? (playerStats != null ? playerStats.dashDamage : 1f)
            : 1f;

        float finalDamage;
        bool isCrit;

        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, enemy.gameObject);
        else { finalDamage = baseDamage; isCrit = false; }

        Vector3 kbDir = CombatQuery.PlanarDirection(transform.root.position, enemy.transform.position, Vector3.right);

        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;

        enemy.TakeDamage(
            Mathf.Max(1, Mathf.RoundToInt(finalDamage)),
            other.transform.position,
            kbDir,
            kbForce,
            isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal
        );

        if (playerStats != null) playerStats.RecordDashHitEnemy();
        if (playerStats != null) playerStats.RecordDamageDealt(finalDamage, DamageSource.Dash, enemy.gameObject);
        if (upgradeManager != null) upgradeManager.DashHitEnemy(enemy.gameObject);
    }
}