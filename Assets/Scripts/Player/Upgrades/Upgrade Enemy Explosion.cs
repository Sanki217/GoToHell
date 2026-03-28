using UnityEngine;

/// <summary>
/// Enemy Explosion — enemies explode on death, dealing damage in a radius.
/// Level 1: 15 damage, radius 4
/// Each level: +5 damage, +1 radius
/// </summary>
public class UpgradeEnemyExplosion : PlayerUpgrade
{
    public override string Id => "Enemy_Explosion";

    private float explosionDamage = 15f;
    private float explosionRadius = 4f;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;
        mgr.OnEnemyKilled += OnEnemyKilled;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        explosionDamage += 5f;
        explosionRadius += 1f;
    }

    private void OnEnemyKilled(GameObject enemyGO)
    {
        if (enemyGO == null) return;

        Vector3 pos = enemyGO.transform.position;

        // Find all enemies in radius and damage them
        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy nearby = hit.GetComponent<Enemy>();
            if (nearby == null) continue;

            float dist = Vector3.Distance(pos, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / explosionRadius); // stronger at center
            float dmg = explosionDamage * falloff;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg));

            // Knockback outward from explosion
            Vector3 dir = (hit.transform.position - pos);
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

            nearby.TakeDamage(
                rounded,
                pos,
                dir.normalized,
                playerStats != null ? playerStats.knockbackForce : 0f,
                false,
                FloatingTextManager.HitType.Normal
            );

            playerStats?.RecordDamageDealt(dmg, DamageSource.Explosion);
        }

        FXManager.Play(ActionFX.EnemyDeath, pos);
    }
}