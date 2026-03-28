using UnityEngine;

/// <summary>
/// Enemy Explosion — enemies deal AoE damage in a radius when they die.
/// Level 1: 15 damage, radius 4
/// Each level: +5 damage, +1 radius
///
/// FIX: Previous version caused chain explosions (A dies → explodes → kills B →
/// B dies → explodes → kills C → ...) generating exponential souls and XP.
///
/// Now each enemy can only be the SOURCE of one explosion. Enemies hit by an
/// explosion are marked with a frame-safe flag so they don't re-trigger
/// the explosion upgrade when they die from explosion damage.
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

        // If this enemy was killed BY an explosion, don't chain
        if (enemyGO.TryGetComponent<ExplosionVictim>(out _)) return;

        Vector3 pos = enemyGO.transform.position;

        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy nearby = hit.GetComponent<Enemy>();
            if (nearby == null) continue;
            // Don't hit the already-dead source enemy
            if (hit.gameObject == enemyGO) continue;

            // Mark this enemy as an explosion victim so it doesn't chain if it dies
            if (!hit.gameObject.TryGetComponent<ExplosionVictim>(out _))
                hit.gameObject.AddComponent<ExplosionVictim>();

            float dist = Vector3.Distance(pos, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
            float dmg = explosionDamage * falloff;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg));

            Vector3 dir = hit.transform.position - pos;
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

/// <summary>
/// Marker component added to enemies hit by an explosion.
/// Prevents them from triggering a second explosion if they die from it.
/// Automatically destroyed with the enemy — no cleanup needed.
/// </summary>
public class ExplosionVictim : UnityEngine.MonoBehaviour { }