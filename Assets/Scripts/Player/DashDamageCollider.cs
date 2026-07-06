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

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        float baseDamage = dashAbility != null
            ? (playerStats != null ? playerStats.dashDamage : 1f)
            : 1f;

        float finalDamage;
        bool isCrit;

        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, other.gameObject);
        else { finalDamage = baseDamage; isCrit = false; }

        // Zero Z before normalizing — prevents 3D direction errors
        Vector3 rawDir = other.transform.position - transform.root.position;
        rawDir.z = 0f;
        Vector3 kbDir = rawDir.magnitude > 0.001f ? rawDir.normalized : Vector3.right;

        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;

        enemy.TakeDamage(
            Mathf.Max(1, Mathf.RoundToInt(finalDamage)),
            other.transform.position,
            kbDir,
            kbForce,
            isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal
        );

        playerStats?.RecordDashHitEnemy();
        playerStats?.RecordDamageDealt(finalDamage, DamageSource.Dash, other.gameObject);
        upgradeManager?.DashHitEnemy(other.gameObject);
    }
}