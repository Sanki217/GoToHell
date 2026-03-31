using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy Explosion — enemies explode on death dealing AoE damage.
/// Shows a red sphere at the explosion position for a brief moment (debug visual).
///
/// Configure in PlayerUpgradeData.behaviourSettings:
///   "damage"          — base explosion damage (default 15)
///   "radius"          — explosion radius (default 4)
///   "damagePerLevel"  — damage added per level (default 5)
///   "radiusPerLevel"  — radius added per level (default 1)
///   "debugVisualTime" — how long the red sphere stays visible (default 0.25s)
/// </summary>
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

    // MonoBehaviour host for coroutines
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
        if (enemyGO.TryGetComponent<ExplosionVictim>(out _)) return;

        Vector3 pos = enemyGO.transform.position;

        // Spawn red sphere debug visual
        coroutineHost?.StartCoroutine(ShowExplosionSphere(pos, explosionRadius));

        // Deal AoE damage
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

        // Also damage explosive barrels and vases in range
        foreach (var hit in hits)
        {
            hit.GetComponent<ExplosiveBarrel>()?.TakeDamage(Mathf.RoundToInt(explosionDamage));
            hit.GetComponent<Vase>()?.TakeDamage(Mathf.RoundToInt(explosionDamage));
        }

        FXManager.Play(ActionFX.EnemyDeath, pos);
    }

    private IEnumerator ShowExplosionSphere(Vector3 pos, float radius)
    {
        // Create a temporary sphere using Unity's built-in sphere primitive
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ExplosionDebugSphere";

        // Remove collider so it doesn't interfere with physics
        Object.Destroy(sphere.GetComponent<Collider>());

        // Position and scale to match explosion radius
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * radius * 2f; // diameter = radius * 2

        // Red semi-transparent material
        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null)
        {
            // Use Standard shader with transparency
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.1f, 0.1f, 0.35f);
            mat.SetFloat("_Mode", 3);   // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            r.material = mat;
        }

        yield return new WaitForSeconds(debugVisualTime);
        Object.Destroy(sphere);
    }
}

public class ExplosionVictim : UnityEngine.MonoBehaviour { }