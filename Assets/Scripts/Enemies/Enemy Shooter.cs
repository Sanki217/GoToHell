using UnityEngine;
using System.Collections;

/// <summary>
/// Stationary turret-style enemy: fires pooled projectiles at the player when
/// in range. Knockback pushes it off its home spot; it eases back afterwards.
/// Fire rate scales with Enemy.SpeedMultiplier (Freeze slows shooting too).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyShooter : MonoBehaviour, IKnockbackReceiver, IDebugDrawable
{
    [Header("Detection")]
    public float attackRange = 15f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    public float projectileSpeed = 10f;
    public float muzzleOffset = 0.7f;

    [Header("Behaviour")]
    public bool facesPlayer = false;

    [Header("Knockback Return")]
    [Tooltip("Speed at which the shooter smoothly returns to its home position")]
    public float returnSpeed = 4f;
    [Tooltip("Half-extents of the BoxCast for wall detection during knockback")]
    public Vector3 knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    [Range(0f, 1f)]
    public float bounceDamping = 0.5f;

    // ================================================================
    //  STATE
    // ================================================================

    private Vector3 homePosition;
    private float cooldownTimer = 0f;

    private bool knockedBack = false;
    private bool returning = false;
    private float returnVelX = 0f;
    private float returnVelY = 0f;

    private Enemy enemy;
    private Collider[] selfColliders;

    // ================================================================
    //  INIT
    // ================================================================

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        homePosition = transform.position;
        selfColliders = GetComponentsInChildren<Collider>(true);
        cooldownTimer = Random.Range(0f, attackCooldown);
    }

    private void OnDisable()
    {
        // Slept by SceneOptimizer mid-knockback: resume by returning home
        StopAllCoroutines();
        if (knockedBack) { knockedBack = false; returning = true; }
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (knockedBack) return;

        float dt = Time.deltaTime;

        // Smooth return to home after knockback
        if (returning)
        {
            float x = Mathf.SmoothDamp(transform.position.x, homePosition.x, ref returnVelX, 0.25f, returnSpeed, dt);
            float y = Mathf.SmoothDamp(transform.position.y, homePosition.y, ref returnVelY, 0.25f, returnSpeed, dt);
            transform.position = new Vector3(x, y, 0f);

            if ((transform.position - homePosition).sqrMagnitude < 0.0025f)
            {
                transform.position = homePosition;
                returning = false;
                returnVelX = 0f;
                returnVelY = 0f;
            }
        }

        // Shooting
        Transform player = PlayerRefs.PlayerTransform;
        if (player == null || projectilePrefab == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.z = 0f;
        if (toPlayer.sqrMagnitude > attackRange * attackRange) return;

        if (facesPlayer && toPlayer.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Slows (Freeze) stretch the cooldown
        cooldownTimer += dt * (enemy != null ? enemy.SpeedMultiplier : 1f);
        if (cooldownTimer < attackCooldown) return;

        cooldownTimer = 0f;
        Shoot(toPlayer.normalized);
    }

    // ================================================================
    //  DEBUG OVERLAY (` key)
    // ================================================================

    public void DrawDebug()
    {
        Vector3 p = transform.position;
        Vector3 label = p + Vector3.up * 1.2f;
        DebugDraw.Range(p, attackRange, DebugColors.Attack, "shoot");
        DebugDraw.Cross(homePosition, 0.3f, DebugColors.Target);
        DebugDraw.Label(label, $"shot {Mathf.Max(0f, attackCooldown - cooldownTimer):0.0}s", DebugColors.State, 4);
        if (knockedBack) DebugDraw.Label(label, "knocked back", DebugColors.State, 5);
        else if (returning) DebugDraw.Label(label, "returning home", DebugColors.State, 5);
    }

    // ================================================================
    //  KNOCKBACK
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        knockedBack = true;
        returning = false;
        returnVelX = 0f;
        returnVelY = 0f;

        Vector3 velocity = impulse / Mathf.Max(duration, 0.01f);
        velocity.z = 0f;
        LayerMask mask = Layers.SolidMask;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 step = velocity * (1f - t * t) * dt;

            step = KnockbackBouncer.StepWithWallBounce(
                step, transform.position, knockbackCastHalf,
                bounceDamping, mask, selfColliders);

            Vector3 p = transform.position;
            transform.position = new Vector3(p.x + step.x, p.y + step.y, 0f);

            yield return null;
        }

        knockedBack = false;
        returning = true;
    }

    // ================================================================
    //  SHOOT
    // ================================================================

    private void Shoot(Vector3 dir)
    {
        Vector3 pos = transform.position + dir * muzzleOffset;
        GameObject proj = Pool.Spawn(projectilePrefab, pos, Quaternion.identity);
        if (proj != null && proj.TryGetComponent(out EnemyProjectile p))
            p.Initialize(dir, projectileSpeed, transform);
    }

    // ================================================================
    //  GIZMOS
    // ================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(homePosition, 0.2f);
        }
    }
}
