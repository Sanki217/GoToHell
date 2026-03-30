using UnityEngine;

/// <summary>
/// Enemy Explosion — enemies explode on death dealing AoE damage.
///
/// Configure in PlayerUpgradeData.behaviourSettings:
///   "damage"          — base explosion damage (default 15)
///   "radius"          — explosion radius (default 4)
///   "damagePerLevel"  — damage added per level (default 5)
///   "radiusPerLevel"  — radius added per level (default 1)
/// </summary>
public class UpgradeEnemyExplosion : PlayerUpgrade
{
    public override string Id => "Enemy_Explosion";

    private float explosionDamage;
    private float explosionRadius;
    private float damagePerLevel;
    private float radiusPerLevel;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;

        // Read configuration from ScriptableObject — set via behaviourSettings in Inspector
        var data = mgr.GetUpgradeData(Id);
        explosionDamage = data?.GetSetting("damage", 15f) ?? 15f;
        explosionRadius = data?.GetSetting("radius", 4f) ?? 4f;
        damagePerLevel = data?.GetSetting("damagePerLevel", 5f) ?? 5f;
        radiusPerLevel = data?.GetSetting("radiusPerLevel", 1f) ?? 1f;

        mgr.OnEnemyKilled += OnEnemyKilled;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        explosionDamage += damagePerLevel;
        explosionRadius += radiusPerLevel;
    }

    private void OnEnemyKilled(GameObject enemyGO)
    {
        if (enemyGO == null) return;
        if (enemyGO.TryGetComponent<ExplosionVictim>(out _)) return;

        Vector3 pos = enemyGO.transform.position;
        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            Enemy nearby = hit.GetComponent<Enemy>();
            if (nearby == null || hit.gameObject == enemyGO) continue;

            if (!hit.gameObject.TryGetComponent<ExplosionVictim>(out _))
                hit.gameObject.AddComponent<ExplosionVictim>();

            float dist = Vector3.Distance(pos, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
            float dmg = explosionDamage * falloff;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg));
            Vector3 dir = hit.transform.position - pos;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

            nearby.TakeDamage(rounded, pos, dir.normalized,
                playerStats != null ? playerStats.knockbackForce : 0f,
                false, FloatingTextManager.HitType.Normal);

            playerStats?.RecordDamageDealt(dmg, DamageSource.Explosion);
        }

        FXManager.Play(ActionFX.EnemyDeath, pos);
    }
}

public class ExplosionVictim : UnityEngine.MonoBehaviour { }