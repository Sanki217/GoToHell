using UnityEngine;

/// <summary>
/// Gravity Arrow pull zone — attach this to a prefab.
///
/// When an arrow sticks to a surface, UpgradeGravityArrow spawns this prefab
/// at the arrow's position. The zone drags nearby enemies toward its centre
/// for a limited time, then destroys itself.
///
/// All values are Inspector-editable on the prefab. UpgradeGravityArrow
/// may override pullSpeed at runtime to incorporate the player's Psyche stat.
///
/// EXECUTION ORDER 500: runs its FixedUpdate AFTER enemy movement scripts
/// so the pull is applied on top of the enemy's own movement each frame.
///
/// SETUP:
///   1. Create a prefab (empty GameObject or with a visual — particle system,
///      glowing sphere, etc.).
///   2. Add this script.
///   3. Tune the values in the Inspector.
///   4. Assign the prefab to UpgradeGravityArrow → Gravity Zone Prefab.
/// </summary>
[DefaultExecutionOrder(500)]
public class GravityArrowZone : MonoBehaviour
{
    [Header("Pull Zone Settings")]
    [Tooltip("Radius within which enemies are pulled toward this zone.")]
    public float pullRadius = 4f;

    [Tooltip("Base pull speed in units per second at the centre (falls off linearly to zero at the edge). " +
             "UpgradeGravityArrow overrides this at runtime to add Psyche scaling.")]
    public float pullSpeed = 2f;

    [Tooltip("How many seconds the zone stays active before destroying itself.")]
    public float duration = 3f;

    [Header("Damage Over Time")]
    [Tooltip("Damage dealt per second to every enemy inside the zone. " +
             "UpgradeGravityArrow overrides this at runtime to add Psyche scaling.")]
    public float damagePerSecond = 3f;

    [Tooltip("Seconds between damage ticks. Smaller = smoother floating numbers, " +
             "larger = fewer text popups. 0.5 is a good default.")]
    public float tickInterval = 0.5f;

    // ================================================================

    private float timer;
    private float tickTimer;

    // Reusable buffer — shared across all GravityArrowZones so we never allocate.
    private static readonly Collider[] hitBuffer = new Collider[64];

    private void Start()
    {
        timer = duration;
        tickTimer = tickInterval;
    }

    private void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // ── Damage tick ───────────────────────────────────────────────
        tickTimer -= Time.fixedDeltaTime;
        bool doDamage = false;
        if (tickTimer <= 0f)
        {
            tickTimer += tickInterval;
            doDamage = true;
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, pullRadius, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider col = hitBuffer[i];
            // Walk up to root in case the enemy collider is on a child object
            Enemy enemy = col.GetComponent<Enemy>()
                          ?? col.transform.root.GetComponent<Enemy>();
            if (enemy == null) continue;

            Vector3 toZone = transform.position - enemy.transform.position;
            toZone.z = 0f;
            float dist = toZone.magnitude;

            // ── Pull ──────────────────────────────────────────────────
            if (dist >= 0.01f)
            {
                // Linear falloff: full pull at centre, zero pull at edge
                float strength = Mathf.Clamp01(1f - dist / pullRadius);
                Vector3 pull   = toZone.normalized * pullSpeed * strength * Time.fixedDeltaTime;

                enemy.transform.position = new Vector3(
                    enemy.transform.position.x + pull.x,
                    enemy.transform.position.y + pull.y,
                    0f);
            }

            // ── Damage ────────────────────────────────────────────────
            if (doDamage && damagePerSecond > 0f)
            {
                int tickDamage = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * tickInterval));
                enemy.TakeDamage(
                    tickDamage,
                    enemy.transform.position,
                    Vector3.zero, 0f,
                    false,
                    FloatingTextManager.HitType.Normal);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Fade the gizmo as the zone expires
        float alpha = Application.isPlaying ? Mathf.Clamp01(timer / Mathf.Max(duration, 0.001f)) : 1f;
        Gizmos.color = new Color(0.3f, 0.75f, 1f, 0.12f + 0.25f * alpha);
        Gizmos.DrawSphere(transform.position, pullRadius);
        Gizmos.color = new Color(0.3f, 0.75f, 1f, 0.8f * alpha + 0.2f);
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
#endif
}
