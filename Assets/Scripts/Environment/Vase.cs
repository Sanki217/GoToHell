using UnityEngine;

/// <summary>
/// Vase — destructible world object (IDestructible). Breaks on arrow, slash,
/// dash or explosion damage, dropping souls, arrows and health orbs.
///
/// DAMAGE SOURCES (all through IDestructible.TakeDamage):
///   - Arrows, slashes, explosions (ExplosiveBarrel, Enemy Explosion upgrade)
///   - Dash: DashDamageCollider triggers OnTriggerEnter on this vase
///
/// SETUP:
///   1. Create a small GameObject (Sphere works as placeholder)
///   2. Add a Sphere/Box Collider (trigger or solid — arrows hit both)
///      Put it on the Destructibles layer (arrows always sweep that layer)
///   3. Add this script
///   4. Assign vaseRenderer
///   5. Assign soulPrefab (for soul drops)
///   6. Assign arrowPrefab (your wall-arrow prefab — same one that spawns in walls)
///   7. Set minSouls/maxSouls and minArrows/maxArrows
///   8. No special tag required
/// </summary>
public class Vase : MonoBehaviour, IDestructible
{
    [Header("Health")]
    public int maxHealth = 10;

    [Header("Soul Drops")]
    public GameObject soulPrefab;
    public int minSouls = 1;
    public int maxSouls = 4;

    [Header("Arrow Drops")]
    [Tooltip("Assign your wall-arrow prefab — the same one that gets spawned in walls")]
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
    private TintController tint;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        tint = TintController.For(gameObject);
        if (vaseRenderer != null) tint.SetTargets(vaseRenderer);
    }

    // ================================================================
    //  PUBLIC API — IDestructible
    // ================================================================

    public void TakeDamage(int amount)
    {
        if (broken) return;

        currentHealth -= amount;
        if (tint != null) tint.Flash(hitColor, hitFlashDuration);

        if (currentHealth <= 0)
            Break();
    }

    // ================================================================
    //  DASH DETECTION
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (broken) return;
        if (other.TryGetComponent<DashDamageCollider>(out _))
        {
            PlayerStats stats = PlayerRefs.I != null ? PlayerRefs.I.Stats : null;
            float dashDmg = stats != null ? stats.dashDamage : 1f;
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

        // Souls — angles -80..80 around "right" (original fan)
        Soul.Burst(soulPrefab, pos, Random.Range(minSouls, maxSouls + 1), -80f, 80f, 3f, 8f);

        // Arrows as immediately-collectible pickups
        int arrowCount = Random.Range(minArrows, maxArrows + 1);
        for (int i = 0; i < arrowCount && arrowPrefab != null; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0f, 0f);
            GameObject arrowGO = Instantiate(arrowPrefab, pos + offset, Quaternion.identity);

            if (arrowGO.TryGetComponent(out ArrowPickup pickup)) pickup.canPickUp = true;
            if (arrowGO.TryGetComponent(out Arrow arrowScript)) arrowScript.enabled = false;   // don't fly
        }

        // Health orbs — upward fan
        int healthOrbCount = Random.Range(minHealthOrbs, maxHealthOrbs + 1);
        for (int i = 0; i < healthOrbCount && healthOrbPrefab != null; i++)
        {
            GameObject orb = Instantiate(healthOrbPrefab, pos, Quaternion.identity);
            if (orb.TryGetComponent(out HealthOrb healthOrb))
            {
                float angle = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f);
                healthOrb.Initialize(dir, Random.Range(3f, 7f));
            }
        }

        Destroy(gameObject);
    }
}
