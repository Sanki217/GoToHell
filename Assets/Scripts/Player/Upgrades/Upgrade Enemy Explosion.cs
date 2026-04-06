using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy Explosion — enemies explode on death dealing AoE damage.
///
/// PlayerUpgradeData behaviourSettings needed:
///   "damage"       — base explosion damage (default 15)
///   "damageAP"     — fraction of Ability Power added to damage (default 1.0)
///   "radius"       — base explosion radius (default 3)
///   "radiusSize"   — fraction of Size stat added to radius (default 0.10)
///   "debugVisual"  — how long the sphere visual stays (default 0.25)
///
/// Example description template:
///   "Enemies explode on death dealing [damage] damage in a [radius] radius."
/// </summary>
public class UpgradeEnemyExplosion : PlayerUpgrade
{
    public override string Id => "Enemy_Explosion";

    private float damage, damageAP, radius, radiusSize, debugVisual;
    private PlayerStats playerStats;
    private MonoBehaviour host;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        host = mgr;

        var data = mgr.GetUpgradeData(Id);
        damage = data?.GetSetting("damage", 15f) ?? 15f;
        damageAP = data?.GetSetting("damageAP", 1.0f) ?? 1.0f;
        radius = data?.GetSetting("radius", 3.0f) ?? 3.0f;
        radiusSize = data?.GetSetting("radiusSize", 0.10f) ?? 0.10f;
        debugVisual = data?.GetSetting("debugVisual", 0.25f) ?? 0.25f;

        mgr.OnEnemyKilled += OnEnemyKilled;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel) { }

    private float GetDamage() =>
        damage + damageAP * (playerStats != null ? playerStats.abilityPower : 0f);

    private float GetRadius() =>
        radius + radiusSize * (playerStats != null ? playerStats.size : 0f);

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
            var nearby = hit.GetComponent<Enemy>();
            if (nearby == null || hit.gameObject == enemyGO) continue;

            if (!hit.gameObject.TryGetComponent<ExplosionVictim>(out _))
            {
                var v = hit.gameObject.AddComponent<ExplosionVictim>();
                host?.StartCoroutine(RemoveVictim(v));
            }

            float dist = Vector3.Distance(pos, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / rad);
            int rounded = Mathf.Max(1, Mathf.RoundToInt(dmg * falloff));
            Vector3 dir = (hit.transform.position - pos).normalized;
            dir.z = 0f;

            nearby.TakeDamage(rounded, pos, dir,
                playerStats != null ? playerStats.knockbackForce : 0f,
                false, FloatingTextManager.HitType.Normal);
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

    private IEnumerator ShowSphere(Vector3 pos, float rad)
    {
        var sphere = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * rad * 2f;
        var r = sphere.GetComponent<Renderer>();
        if (r != null) r.material = MakeTransparentMaterial(new Color(1f, 0.1f, 0.1f, 0.35f));
        yield return new WaitForSeconds(debugVisual);
        Object.Destroy(sphere);
    }

    public static Material MakeTransparentMaterial(Color color)
    {
        var shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}

public class ExplosionVictim : UnityEngine.MonoBehaviour { }