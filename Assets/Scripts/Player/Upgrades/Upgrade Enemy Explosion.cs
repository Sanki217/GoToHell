using UnityEngine;
using System.Collections;

public class UpgradeEnemyExplosion : PlayerUpgrade
{
    public override string Id => "Enemy_Explosion";

    private float explosionDamage;
    private float explosionRadius;
    private float damagePerLevel;
    private float radiusPerLevel;
    private float debugVisualTime;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;
    private MonoBehaviour coroutineHost;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;
        coroutineHost = mgr;

        var data = mgr.GetUpgradeData(Id);
        explosionDamage = data?.GetSetting("damage", 15f) ?? 15f;
        explosionRadius = data?.GetSetting("radius", 4f) ?? 4f;
        damagePerLevel = data?.GetSetting("damagePerLevel", 5f) ?? 5f;
        radiusPerLevel = data?.GetSetting("radiusPerLevel", 1f) ?? 1f;
        debugVisualTime = data?.GetSetting("debugVisualTime", 0.25f) ?? 0.25f;

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

        // Only skip if this enemy was killed BY an explosion this same frame.
        // ExplosionVictim is removed after 1 frame so enemies that survive the
        // explosion (and die later from arrows etc.) still trigger their own explosion.
        if (enemyGO.TryGetComponent<ExplosionVictim>(out _)) return;

        Vector3 pos = enemyGO.transform.position;

        coroutineHost?.StartCoroutine(ShowExplosionSphere(pos, explosionRadius));

        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            Enemy nearby = hit.GetComponent<Enemy>();
            if (nearby == null || hit.gameObject == enemyGO) continue;

            // Mark as explosion victim for THIS frame only — removed next frame
            // so enemies that survive don't have their later explosions suppressed
            if (!hit.gameObject.TryGetComponent<ExplosionVictim>(out _))
            {
                var victim = hit.gameObject.AddComponent<ExplosionVictim>();
                coroutineHost?.StartCoroutine(RemoveVictimNextFrame(victim));
            }

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

        foreach (var hit in hits)
        {
            hit.GetComponent<ExplosiveBarrel>()?.TakeDamage(Mathf.RoundToInt(explosionDamage));
            hit.GetComponent<Vase>()?.TakeDamage(Mathf.RoundToInt(explosionDamage));
        }

        FXManager.Play(ActionFX.EnemyDeath, pos);
    }

    // Remove ExplosionVictim the frame after it was added.
    // This means the enemy is immune to chaining during THIS explosion event,
    // but is free to trigger its own explosion if it dies later.
    private IEnumerator RemoveVictimNextFrame(ExplosionVictim victim)
    {
        yield return null; // wait one frame
        if (victim != null)
            Object.Destroy(victim);
    }

    private IEnumerator ShowExplosionSphere(Vector3 pos, float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ExplosionDebugSphere";
        Object.Destroy(sphere.GetComponent<Collider>());

        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * radius * 2f;

        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null)
            r.material = MakeTransparentMaterial(new Color(1f, 0.1f, 0.1f, 0.35f));

        yield return new WaitForSeconds(debugVisualTime);
        Object.Destroy(sphere);
    }

    // ================================================================
    //  SHARED TRANSPARENT MATERIAL HELPER
    //  Uses Unlit/Transparent — always included in builds, no shader stripping issues.
    // ================================================================

    public static Material MakeTransparentMaterial(Color color)
    {
        // Unlit/Transparent is always available in builds (unlike "Standard")
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}

public class ExplosionVictim : UnityEngine.MonoBehaviour { }