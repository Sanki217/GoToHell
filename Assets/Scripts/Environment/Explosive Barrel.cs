using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Explosive Barrel — destructible world object (IDestructible).
/// Any damage starts the fuse. After fuseTime seconds it explodes in a radius.
///
/// DAMAGE SOURCES (all through IDestructible.TakeDamage):
///   - Arrows, slashes, explosions (other barrels, Enemy Explosion upgrade)
///   - Dash: DashDamageCollider triggers OnTriggerEnter on this barrel
///
/// The explosion damages each enemy and each other destructible exactly once,
/// with distance falloff for enemies; nearby barrels chain.
///
/// SETUP:
///   1. Create a GameObject (Capsule works as placeholder)
///   2. Add a Capsule/Box Collider (trigger or solid — arrows hit both)
///      Put it on the Destructibles layer (arrows always sweep that layer)
///   3. Add this script
///   4. Assign barrelRenderer
///   5. Set explosionDamage, explosionRadius, fuseTime in Inspector
///   6. Optionally assign soulPrefab for soul drops
/// </summary>
public class ExplosiveBarrel : MonoBehaviour, IDestructible
{
    [Header("Health")]
    public int maxHealth = 30;

    [Header("Explosion")]
    public float explosionDamage = 40f;
    public float explosionRadius = 5f;
    public float fuseTime = 1f;

    [Header("Visual")]
    public Renderer barrelRenderer;

    [Header("Soul Drops (optional)")]
    public GameObject soulPrefab;
    public int minimumSouls = 0;
    public int maximumSouls = 2;

    [Header("Debug Explosion Visual")]
    public float debugSphereTime = 0.25f;

    // ================================================================
    //  STATE
    // ================================================================

    private int currentHealth;
    private bool fuseActive = false;
    private bool exploded = false;
    private TintController tint;

    private PlayerStats Stats => PlayerRefs.I != null ? PlayerRefs.I.Stats : null;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        tint = TintController.For(gameObject);
        if (barrelRenderer != null) tint.SetTargets(barrelRenderer);
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void TakeDamage(int amount)
    {
        if (exploded) return;

        currentHealth -= amount;
        if (tint != null) tint.Flash(Color.white, 0.06f);

        if (!fuseActive)
            StartFuse();
    }

    // ================================================================
    //  DASH DETECTION
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;
        if (other.TryGetComponent<DashDamageCollider>(out _))
        {
            PlayerStats stats = Stats;
            float dashDmg = stats != null ? stats.dashDamage : 1f;
            TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dashDmg)));
        }
    }

    // ================================================================
    //  FUSE
    // ================================================================

    private void StartFuse()
    {
        fuseActive = true;
        StartCoroutine(FuseCoroutine());
    }

    private IEnumerator FuseCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < fuseTime)
        {
            elapsed += Time.deltaTime;
            float flashRate = Mathf.Lerp(2f, 12f, elapsed / fuseTime);
            float t = Mathf.PingPong(elapsed * flashRate, 1f);
            if (tint != null) tint.SetTint(this, Color.Lerp(tint.BaseColor, Color.red, t));
            yield return null;
        }

        Explode();
    }

    // ================================================================
    //  EXPLOSION
    // ================================================================

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector3 pos = transform.position;
        DebugShapes.Sphere(pos, explosionRadius, new Color(1f, 0.4f, 0f, 0.35f), debugSphereTime);

        PlayerStats stats = Stats;
        float kb = stats != null ? stats.knockbackForce : 0f;

        // Enemies — each once, with distance falloff
        List<Enemy> enemies = ListPool<Enemy>.Get();
        CombatQuery.OverlapEnemies(pos, explosionRadius, enemies);
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            float dist = Vector3.Distance(pos, enemy.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
            int dmg = Mathf.Max(1, Mathf.RoundToInt(explosionDamage * falloff));
            Vector3 dir = CombatQuery.PlanarDirection(pos, enemy.transform.position, Vector3.right);
            enemy.TakeDamage(dmg, pos, dir, kb, false, FloatingTextManager.HitType.Normal);
        }
        ListPool<Enemy>.Release(enemies);

        // Other destructibles — barrels chain, vases break
        List<IDestructible> others = ListPool<IDestructible>.Get();
        CombatQuery.OverlapDestructibles(pos, explosionRadius, others, this);
        foreach (IDestructible d in others)
            d.TakeDamage(Mathf.RoundToInt(explosionDamage));
        ListPool<IDestructible>.Release(others);

        Soul.Burst(soulPrefab, pos, Random.Range(minimumSouls, maximumSouls + 1), -60f, 60f, 4f, 9f);

        Destroy(gameObject);
    }
}
