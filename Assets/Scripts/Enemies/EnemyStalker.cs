using UnityEngine;
using System.Collections;

/// <summary>
/// Flying stalker. Spawned by EnemyStalkerSpawner in front of the level
/// (negative Z, toward the camera, outside the view) and flies onto the
/// gameplay plane (z = 0), then follows the player: matches his height and
/// hovers at followDistance to his side. Becomes hittable once it reaches the
/// gameplay plane, like every other enemy.
///
/// VARIANTS — one script, prefab config only:
///   • Melee:   small followDistance, projectilePrefab EMPTY; contact damage
///     comes from a child trigger collider with EnemyDamage.
///   • Shooter: large followDistance, projectilePrefab assigned — fires once
///     it has arrived on the gameplay plane.
///
/// Standard enemy prefab architecture: tag "Enemy", enemy layer, mesh,
/// Rigidbody, BoxCollider, Enemy, EnemyHealthBar, this script.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyStalker : MonoBehaviour, IKnockbackReceiver
{
    [Header("Following")]
    [Tooltip("Horizontal distance kept from the player. Small = melee, large = shooter.")]
    public float followDistance = 1.5f;
    [Tooltip("Top speed of the chase.")]
    public float moveSpeed = 8f;
    [Tooltip("Vertical offset from the player while following.")]
    public float heightOffset = 0.5f;
    [Tooltip("Chase smoothing — higher = floatier.")]
    public float smoothTime = 0.35f;

    [Header("Shooting (leave prefab EMPTY for the melee variant)")]
    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    public float projectileSpeed = 10f;
    public float attackRange = 14f;
    public float muzzleOffset = 0.7f;
    [Tooltip("How close to the gameplay plane (z = 0) it must be before it may shoot.")]
    public float shootZTolerance = 0.5f;

    [Header("Knockback")]
    public Vector3 knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    [Range(0f, 1f)] public float bounceDamping = 0.5f;

    // ── state ───────────────────────────────────────────────────────
    private Transform player;
    private float side;          // -1 = hover left of the player, 1 = right
    private float cooldownTimer;
    private bool knockedBack;
    private Vector3 chaseVelocity;
    private Collider[] selfColliders;

    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        if (PlayerRefs.I != null) player = PlayerRefs.I.T;

        side = player != null && transform.position.x < player.position.x ? -1f : 1f;
        cooldownTimer = Random.Range(0f, attackCooldown);
    }

    private void Update()
    {
        if (knockedBack || player == null) return;

        // Chase point: the player's side at followDistance, matching his
        // height, always converging on the gameplay plane (z = 0).
        Vector3 target = new Vector3(
            player.position.x + side * followDistance,
            player.position.y + heightOffset,
            0f);

        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref chaseVelocity, smoothTime, moveSpeed);

        TryShoot();
    }

    // ================================================================
    //  SHOOTING — no-op for the melee variant (no prefab assigned)
    // ================================================================

    private void TryShoot()
    {
        if (projectilePrefab == null) return;
        if (Mathf.Abs(transform.position.z) > shootZTolerance) return;   // still flying in

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.z = 0f;
        if (toPlayer.magnitude > attackRange) return;

        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < attackCooldown) return;
        cooldownTimer = 0f;

        Vector3 dir = toPlayer.normalized;
        Vector3 pos = transform.position + dir * muzzleOffset;
        GameObject proj = Pool.Spawn(projectilePrefab, pos, Quaternion.identity);
        proj.GetComponent<EnemyProjectile>()?.Initialize(dir, projectileSpeed, transform.root);
    }

    // ================================================================
    //  KNOCKBACK — same wall-bounce pattern as EnemyShooter, but it
    //  simply resumes the chase afterwards (no home position).
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        knockedBack = true;
        chaseVelocity = Vector3.zero;

        Vector3 velocity = impulse / Mathf.Max(duration, 0.01f);
        velocity.z = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 step = velocity * (1f - t * t) * dt;

            LayerMask mask = LayerMask.GetMask("Wall", "Ground");
            step = KnockbackBouncer.StepWithWallBounce(
                step, transform.position, knockbackCastHalf,
                bounceDamping, mask, selfColliders);

            transform.position += new Vector3(step.x, step.y, 0f);
            yield return null;
        }

        knockedBack = false;
    }
}
