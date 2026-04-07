using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy Explosion upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
/// All tuning values are exposed in the Inspector on the prefab.
///
/// Effect: enemies explode on death, dealing AoE damage with falloff.
/// Final damage  = damage + damageAP × AbilityPower
/// Final radius  = radius + radiusSize × Size
/// </summary>
public class UpgradeEnemyExplosion : PlayerUpgrade
{
    [Header("Enemy Explosion — Tuning")]
    [Tooltip("Base explosion damage.")]
    public float damage = 15f;

    [Tooltip("Fraction of Ability Power added to damage.")]
    public float damageAP = 1.0f;

    [Tooltip("Base explosion radius in world units.")]
    public float radius = 3f;

    [Tooltip("Fraction of Size stat added to radius.")]
    public float radiusSize = 0.10f;

    [Tooltip("How long the debug sphere visual stays visible (seconds).")]
    public float debugVisualDuration = 0.25f;

    private PlayerStats playerStats;
    private MonoBehaviour host;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        host = mgr;
        mgr.OnEnemyKilled += OnEnemyKilled;
    }

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
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * rad * 2f;
        var r = sphere.GetComponent<Renderer>();
        if (r != null) r.material = MakeTransparentMaterial(new Color(1f, 0.1f, 0.1f, 0.35f));
        yield return new WaitForSeconds(debugVisualDuration);
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

public class ExplosionVictim : MonoBehaviour { }