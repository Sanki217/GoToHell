using UnityEngine;
using System.Collections;

/// <summary>
/// Vase � destructible world object. Breaks on arrow or dash hit, drops souls and arrows.
///
/// DAMAGE SOURCES:
///   - Arrows:     Arrow.cs calls TakeDamage() directly via raycast hit
///   - Dash:       DashDamageCollider triggers OnTriggerEnter on this vase
///   - Explosions: ExplosiveBarrel or UpgradeEnemyExplosion call TakeDamage()
///
/// SETUP:
///   1. Create a small GameObject (Sphere works as placeholder)
///   2. Add a Sphere/Box Collider � Is Trigger = FALSE (arrows must stick to it)
///      Make sure it's on a layer included in Arrow's stickableLayers
///   3. Add this script
///   4. Assign vaseRenderer
///   5. Assign soulPrefab (for soul drops)
///   6. Assign arrowPrefab (your wall-arrow prefab � same one that spawns in walls)
///   7. Set minSouls/maxSouls and minArrows/maxArrows
///   8. No special tag required
/// </summary>
public class Vase : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 10;

    [Header("Soul Drops")]
    public GameObject soulPrefab;
    public int minSouls = 1;
    public int maxSouls = 4;

    [Header("Arrow Drops")]
    [Tooltip("Assign your wall-arrow prefab � the same one that gets spawned in walls")]
    public GameObject arrowPrefab;
    public int minArrows = 0;
    public int maxArrows = 2;

    [Header("Health Orb Drops")]
    public GameObject healthOrbPrefab;
    public int minHealthOrbs = 1;
    public int maxHealthOrbs = 3;

    [Header("Hit Flash")]
    public Renderer vaseRenderer;
    public Color hitColor = Color.white;
    public float hitFlashDuration = 0.06f;

    // ================================================================
    //  STATE
    // ================================================================

    private int currentHealth;
    private bool broken = false;

    private PlayerStats playerStats;

    private void Start()
    {
        currentHealth = maxHealth;
        if (vaseRenderer == null)
            vaseRenderer = GetComponentInChildren<Renderer>();

        playerStats = PlayerRefs.I?.Stats;
    }

    // ================================================================
    //  PUBLIC API � called by Arrow.cs, explosions, etc.
    // ================================================================

    public void TakeDamage(int amount)
    {
        if (broken) return;

        currentHealth -= amount;
        StartCoroutine(HitFlash());

        if (currentHealth <= 0)
            Break();
    }

    // ================================================================
    //  DASH DETECTION
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (broken) return;
        if (other.GetComponent<DashDamageCollider>() != null)
        {
            float dashDmg = playerStats != null ? playerStats.dashDamage : 1f;
            TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dashDmg)));
        }
    }

    // ================================================================
    //  BREAK
    // ================================================================

    private void Break()
    {
        if (broken) return;
        broken = true;

        Vector3 pos = transform.position;

        // Drop souls
        int soulCount = Random.Range(minSouls, maxSouls + 1);
        for (int i = 0; i < soulCount; i++)
        {
            if (!soulPrefab) break;
            GameObject s = Pool.Spawn(soulPrefab, pos, Quaternion.identity);
            Soul soul = s.GetComponent<Soul>();
            if (soul != null)
            {
                float angle = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                soul.Initialize(dir, Random.Range(3f, 8f));
            }
        }

        // Drop arrows as immediately-collectible pickups
        int arrowCount = Random.Range(minArrows, maxArrows + 1);
        for (int i = 0; i < arrowCount; i++)
        {
            if (!arrowPrefab) break;

            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0f, 0f);
            GameObject arrowGO = Instantiate(arrowPrefab, pos + offset, Quaternion.identity);

            // Immediately collectible
            ArrowPickup pickup = arrowGO.GetComponent<ArrowPickup>();
            if (pickup != null) pickup.canPickUp = true;

            // Disable movement so it doesn't fly
            Arrow arrowScript = arrowGO.GetComponent<Arrow>();
            if (arrowScript != null) arrowScript.enabled = false;
        }

        // Drop health orbs
        int healthOrbCount = Random.Range(minHealthOrbs, maxHealthOrbs + 1);
        for (int i = 0; i < healthOrbCount; i++)
        {
            if (!healthOrbPrefab) break;
            GameObject orb = Instantiate(healthOrbPrefab, pos, Quaternion.identity);
            HealthOrb healthOrb = orb.GetComponent<HealthOrb>();
            if (healthOrb != null)
            {
                float angle = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f).normalized;
                healthOrb.Initialize(dir, Random.Range(3f, 7f));
            }
        }

        Destroy(gameObject);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private IEnumerator HitFlash()
    {
        if (vaseRenderer == null) yield break;
        Color orig = vaseRenderer.material.color;
        vaseRenderer.material.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!broken) vaseRenderer.material.color = orig;
    }
}