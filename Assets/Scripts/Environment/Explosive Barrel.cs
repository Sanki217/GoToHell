using UnityEngine;
using System.Collections;

/// <summary>
/// Explosive Barrel � destructible world object.
/// Any damage starts the fuse. After fuseTime seconds it explodes in a radius.
///
/// DAMAGE SOURCES:
///   - Arrows:           Arrow.cs calls TakeDamage() directly via raycast hit
///   - Dash:             DashDamageCollider triggers OnTriggerEnter on this barrel
///   - Explosion chains: Other barrels / UpgradeEnemyExplosion call TakeDamage()
///
/// SETUP:
///   1. Create a GameObject (Capsule works as placeholder)
///   2. Add a Capsule/Box Collider � Is Trigger = FALSE (solid, arrows stick to it)
///      This collider must be on a layer included in Arrow's stickableLayers
///   3. Add this script
///   4. Assign barrelRenderer
///   5. Set explosionDamage, explosionRadius, fuseTime in Inspector
///   6. Optionally assign soulPrefab for soul drops
/// </summary>
public class ExplosiveBarrel : MonoBehaviour
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

    private PlayerStats playerStats;

    private void Start()
    {
        currentHealth = maxHealth;
        if (barrelRenderer == null)
            barrelRenderer = GetComponentInChildren<Renderer>();

        var player = GameObject.FindWithTag("Player");
        if (player != null) playerStats = player.GetComponent<PlayerStats>();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void TakeDamage(int amount)
    {
        if (exploded) return;

        currentHealth -= amount;
        StartCoroutine(HitFlash());

        if (!fuseActive)
            StartFuse();
    }

    // ================================================================
    //  DASH DETECTION
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;
        if (other.GetComponent<DashDamageCollider>() != null)
        {
            float dashDmg = playerStats != null ? playerStats.dashDamage : 1f;
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
        Color originalColor = barrelRenderer != null
            ? barrelRenderer.material.color : Color.white;

        while (elapsed < fuseTime)
        {
            elapsed += Time.deltaTime;
            float flashRate = Mathf.Lerp(2f, 12f, elapsed / fuseTime);
            float t = Mathf.PingPong(elapsed * flashRate, 1f);
            if (barrelRenderer != null)
                barrelRenderer.material.color = Color.Lerp(originalColor, Color.red, t);
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

        SpawnDebugSphere(pos, explosionRadius);

        Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    float dist = Vector3.Distance(pos, hit.transform.position);
                    float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(explosionDamage * falloff));
                    Vector3 dir = hit.transform.position - pos;
                    dir.z = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

                    float kb = playerStats != null ? playerStats.knockbackForce : 0f;
                    enemy.TakeDamage(dmg, pos, dir.normalized, kb, false,
                        FloatingTextManager.HitType.Normal);
                }
            }

            var otherBarrel = hit.GetComponent<ExplosiveBarrel>();
            if (otherBarrel != null && otherBarrel != this && !otherBarrel.exploded)
                otherBarrel.TakeDamage(Mathf.RoundToInt(explosionDamage));

            hit.GetComponent<Vase>()?.TakeDamage(Mathf.RoundToInt(explosionDamage));
        }

        int soulCount = Random.Range(minimumSouls, maximumSouls + 1);
        for (int i = 0; i < soulCount; i++)
        {
            if (!soulPrefab) break;
            GameObject s = Instantiate(soulPrefab, pos, Quaternion.identity);
            Soul soul = s.GetComponent<Soul>();
            if (soul != null)
            {
                float angle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                soul.Initialize(dir, Random.Range(4f, 9f));
            }
        }

        Destroy(gameObject);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private IEnumerator HitFlash()
    {
        if (barrelRenderer == null) yield break;
        Color orig = barrelRenderer.material.color;
        barrelRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        if (!exploded) barrelRenderer.material.color = orig;
    }

    public void SpawnDebugSphere(Vector3 pos, float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * radius * 2f;

        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null)
            r.material = UpgradeEnemyExplosion.MakeTransparentMaterial(
                new Color(1f, 0.4f, 0f, 0.35f));

        Destroy(sphere, debugSphereTime);
    }
}