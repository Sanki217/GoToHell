using UnityEngine;
using System.Collections;

/// <summary>
/// Shared logic for single-axis patrolling enemies (EnemyPatrolHorizontal /
/// EnemyPatrolVertical). The two used to be ~95% copy-paste; a bug fixed in
/// one never reached the other. A subclass only says which axis it patrols.
///
/// BEHAVIOUR:
///   • Measures its path at spawn by raycasting both ways along the patrol axis.
///   • Turns at obstacles (overlap box ahead) and at path ends, with a short
///     cooldown against double flips, and SmoothDamp-eased velocity.
///   • Knockback suspends the patrol entirely (it used to keep patrolling
///     underneath the knockback motion), then eases back onto its path line.
///   • Respects Enemy.SpeedMultiplier (Freeze and any future slows).
/// </summary>
[RequireComponent(typeof(Enemy))]
public abstract class EnemyPatrolBase : MonoBehaviour, IKnockbackReceiver
{
    [Header("Movement")]
    public float speed = 3f;
    [Tooltip("Speed at which the enemy slides back onto its path line after knockback")]
    public float returnSpeed = 5f;

    [Header("Path — measured at spawn")]
    public float pathScanDistance = 60f;
    public LayerMask solidLayers;

    [Header("Turn Detection")]
    public float turnLookAhead = 0.55f;
    public Vector3 turnBoxHalfExtents = new Vector3(0.35f, 0.45f, 0.1f);
    [Tooltip("Seconds after a turn before the next wall check. Prevents double-reversals.")]
    public float turnCooldown = 0.25f;

    [Header("Knockback Wall Bounce")]
    public Vector3 knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    [Range(0f, 1f)]
    public float bounceDamping = 0.6f;

    [Header("Movement Smoothing")]
    [Tooltip("Time in seconds to reach full speed after starting or reversing direction. " +
             "0 = instant. ~0.12 gives natural ease-in/out on turns.")]
    public float accelerationTime = 0.12f;

    // ================================================================
    //  AXIS — the only thing subclasses define
    // ================================================================

    /// <summary>Unit vector of the patrol axis (Vector3.right or Vector3.up).</summary>
    protected abstract Vector3 Axis { get; }

    // ================================================================
    //  STATE
    // ================================================================

    protected float pathMin, pathMax;   // along Axis
    protected float pathLine;           // fixed coordinate on the other axis
    protected float dir = 1f;
    private float turnCooldownTimer;

    private bool suspended;
    private bool returning;
    private float returnVel;

    private float smoothedVel;
    private float smoothDampVel;

    private Enemy enemy;
    private Collider[] selfColliders;

    // Reusable hit buffer — shared across all patrol instances, avoids per-frame allocation
    private static readonly Collider[] hitBuffer = new Collider[8];

    private bool IsHorizontal => Axis.x != 0f;

    // ================================================================
    //  INIT
    // ================================================================

    protected virtual void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    protected virtual void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        pathLine = IsHorizontal ? transform.position.y : transform.position.x;
        MeasurePath();
    }

    private void OnDisable()
    {
        // Sleeping (SceneOptimizer) or destroyed mid-knockback: never wake up suspended
        StopAllCoroutines();
        if (suspended) returning = true;   // ease back onto the path when woken
        suspended = false;
    }

    private float Along(Vector3 p) => IsHorizontal ? p.x : p.y;

    private void MeasurePath()
    {
        Vector3 o = transform.position;
        float origin = Along(o);

        pathMin = Physics.Raycast(o, -Axis, out RaycastHit hMin, pathScanDistance, solidLayers,
                                  QueryTriggerInteraction.Ignore)
            ? Along(hMin.point) + 0.15f
            : origin - pathScanDistance;

        pathMax = Physics.Raycast(o, Axis, out RaycastHit hMax, pathScanDistance, solidLayers,
                                  QueryTriggerInteraction.Ignore)
            ? Along(hMax.point) - 0.15f
            : origin + pathScanDistance;

        if (pathMax - pathMin < 0.5f)
        {
            pathMin = origin - 4f;
            pathMax = origin + 4f;
        }
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void FixedUpdate()
    {
        if (suspended) return;

        float dt = Time.fixedDeltaTime;
        Vector3 p = transform.position;
        float along = Along(p);
        float cross = IsHorizontal ? p.y : p.x;

        // ── Cross axis: smooth return to the path line after knockback ──
        if (returning)
        {
            cross = Mathf.SmoothDamp(cross, pathLine, ref returnVel, 0.2f, returnSpeed, dt);
            if (Mathf.Abs(cross - pathLine) < 0.015f)
            {
                cross = pathLine;
                returning = false;
                returnVel = 0f;
            }
        }

        // ── Patrol axis: smoothly accelerating patrol ──
        turnCooldownTimer -= dt;
        if (turnCooldownTimer <= 0f && ObstacleAhead())
        {
            dir *= -1f;
            turnCooldownTimer = turnCooldown;
        }

        float slow = enemy != null ? enemy.SpeedMultiplier : 1f;
        float targetVel = dir * speed * slow;
        float smoothTime = accelerationTime > 0f ? accelerationTime : 0.001f;
        smoothedVel = Mathf.SmoothDamp(smoothedVel, targetVel, ref smoothDampVel, smoothTime,
                                       float.MaxValue, dt);

        float next = along + smoothedVel * dt;

        // Hard clamp at path bounds
        if (next <= pathMin) { next = pathMin; dir = 1f;  turnCooldownTimer = turnCooldown; }
        if (next >= pathMax) { next = pathMax; dir = -1f; turnCooldownTimer = turnCooldown; }

        transform.position = IsHorizontal
            ? new Vector3(next, cross, 0f)
            : new Vector3(cross, next, 0f);
    }

    // ================================================================
    //  OBSTACLE DETECTION
    // ================================================================

    private bool ObstacleAhead()
    {
        Vector3 center = transform.position + Axis * (dir * turnLookAhead);
        int count = Physics.OverlapBoxNonAlloc(center, turnBoxHalfExtents, hitBuffer,
                                               Quaternion.identity, solidLayers,
                                               QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsSelf(hitBuffer[i])) return true;
        return false;
    }

    private bool IsSelf(Collider c)
    {
        if (selfColliders == null) return false;
        foreach (var sc in selfColliders)
            if (sc == c) return true;
        return false;
    }

    // ================================================================
    //  KNOCKBACK — called by EnemyKnockback
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        suspended = true;            // patrol paused for the whole knockback
        returning = false;
        returnVel = 0f;
        smoothedVel = 0f;
        smoothDampVel = 0f;

        Vector3 velocity = impulse / Mathf.Max(duration, 0.01f);
        velocity.z = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 step = velocity * (1f - t * t) * dt;

            step = KnockbackBouncer.StepWithWallBounce(
                step, transform.position, knockbackCastHalf,
                bounceDamping, solidLayers, selfColliders);

            Vector3 p = transform.position;
            transform.position = new Vector3(p.x + step.x, p.y + step.y, 0f);
            yield return null;
        }

        suspended = false;
        returning = true;
        returnVel = 0f;
    }

    // ================================================================
    //  GIZMOS
    // ================================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 a = IsHorizontal ? new Vector3(pathMin, pathLine, 0f) : new Vector3(pathLine, pathMin, 0f);
        Vector3 b = IsHorizontal ? new Vector3(pathMax, pathLine, 0f) : new Vector3(pathLine, pathMax, 0f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.15f);
        Gizmos.DrawWireSphere(b, 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Axis * (dir * turnLookAhead), turnBoxHalfExtents * 2f);
    }
}
