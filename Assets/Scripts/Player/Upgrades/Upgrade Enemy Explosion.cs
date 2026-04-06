using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy Explosion — enemies explode on death.
/// Damage:  5/10/15/20/25 + 100% Ability Power
/// Radius:  2/2.5/3/3.5/4 + 10% Size
///
/// All values configurable in Inspector. Chain prevention via ExplosionVictim
/// (removed after 1 frame so survivors can still chain later).
/// </summary>
public class UpgradeEnemyExplosion : PlayerUpgrade
{
    public override string Id => "Enemy_Explosion";

    [Header("Base Damage per Level")]
    public float[] baseDamagePerLevel = { 5f, 10f, 15f, 20f, 25f };

    [Header("Ability Power Scaling (fraction, 1.0 = 100%)")]
    public float abilityPowerScaling = 1.0f;

    [Header("Base Radius per Level (world units)")]
    public float[] baseRadiusPerLevel = { 2.0f, 2.5f, 3.0f, 3.5f, 4.0f };

    [Header("Size Stat Scaling on Radius (fraction per Size point)")]
    public float sizeScalingPerPoint = 0.10f;

    [Header("Debug Sphere")]
    public float debugVisualTime = 0.25f;

    // ── Private ──────────────────────────────────────────────────────────
    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;
    private MonoBehaviour host;
    private int currentLevel = 0;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;
        host = mgr;
        currentLevel = 1;
        mgr.OnEnemyKilled += OnEnemyKilled;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        currentLevel = newLevel;
    }

    // ── Per-frame derived values ──────────────────────────────────────────

    private float GetDamage()
    {
        int idx = Mathf.Clamp(currentLevel - 1, 0, baseDamagePerLevel.Length - 1);
        float apBonus = playerStats != null ? playerStats.abilityPower * abilityPowerScaling : 0f;
        return baseDamagePerLevel[idx] + apBonus;
    }

    private float GetRadius()
    {
        int idx = Mathf.Clamp(currentLevel - 1, 0, baseRadiusPerLevel.Length - 1);
        float sizeBonus = playerStats != null ? playerStats.size * sizeScalingPerPoint : 0f;
        return baseRadiusPerLevel[idx] + sizeBonus;
    }

    private void OnEnemyKilled(GameObject enemyGO)
    {
        if (enemyGO == null) return;
        if (enemyGO.TryGetComponent<ExplosionVictim>(out _)) return;

        float dmg = GetDamage();
        float rad = GetRadius();
        Vector3 pos = enemyGO.transform.position;

        host?.StartCoroutine(ShowSphere(pos, rad));

        Collider[] hits = Physics.OverlapSphere(pos, rad);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            Enemy nearby = hit.GetComponent<Enemy>();
            if (nearby == null || hit.gameObject == enemyGO) continue;

            if (!hit.gameObject.TryGetComponent<ExplosionVictim>(out _))
            {
                var victim = hit.gameObject.AddComponent<ExplosionVictim>();
                host?.StartCoroutine(RemoveVictim(victim));
            }

            float dist = Vector3.Distance(pos, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / rad);
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg * falloff));
            Vector3 dir = hit.transform.position - pos; dir.z = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

            float kb = playerStats != null ? playerStats.knockbackForce : 0f;
            nearby.TakeDamage(rounded, pos, dir.normalized, kb, false,
                FloatingTextManager.HitType.Normal);
            playerStats?.RecordDamageDealt(dmg * falloff, DamageSource.Explosion);
        }

        foreach (var hit in hits)
        {
            hit.GetComponent<ExplosiveBarrel>()?.TakeDamage(Mathf.RoundToInt(dmg));
            hit.GetComponent<Vase>()?.TakeDamage(Mathf.RoundToInt(dmg));
        }

        FXManager.Play(ActionFX.EnemyDeath, pos);
    }

    private IEnumerator RemoveVictim(ExplosionVictim v)
    {
        yield return null;
        if (v != null) Object.Destroy(v);
    }

    private IEnumerator ShowSphere(Vector3 pos, float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * radius * 2f;
        var r = sphere.GetComponent<Renderer>();
        if (r != null)
            r.material = UpgradeEnemyExplosion.MakeTransparentMaterial(new Color(1f, 0.1f, 0.1f, 0.35f));
        yield return new WaitForSeconds(debugVisualTime);
        Object.Destroy(sphere);
    }

    public static Material MakeTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}

public class ExplosionVictim : UnityEngine.MonoBehaviour { }