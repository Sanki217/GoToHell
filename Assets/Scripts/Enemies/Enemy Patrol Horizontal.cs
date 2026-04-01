using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class EnemyPatrolHorizontal : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    [Tooltip("How quickly the enemy accelerates to target speed (lower = smoother)")]
    public float smoothTime = 0.12f;
    [Tooltip("Speed at which enemy returns to path Y after knockback")]
    public float returnSpeed = 5f;

    [Header("Path — measured at spawn")]
    public float pathScanDistance = 60f;
    public LayerMask solidLayers;

    [Header("Turn Detection")]
    public float turnLookAhead = 0.55f;
    public Vector3 turnBoxHalfExtents = new Vector3(0.35f, 0.45f, 0.1f);

    [Header("Knockback Wall Bounce")]
    [Tooltip("Half-extents of the cast shape used to detect walls during knockback")]
    public Vector3 knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    [Tooltip("How much speed is retained after bouncing off a wall (0=stop, 1=full bounce)")]
    [Range(0f, 1f)]
    public float bounceDamping = 0.6f;

    [Header("Freeze (set by FreezeEffect)")]
    [Range(0f, 1f)]
    public float speedMultiplier = 1f;

    // ================================================================
    //  STATE
    // ================================================================

    private float pathMinX, pathMaxX, pathY;
    private float dirX = 1f;
    private float currentVelX = 0f;   // used by SmoothDamp
    private float currentVelY = 0f;

    private bool suspended = false;
    private bool returningY = false;

    private Collider[] selfColliders;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        pathY = transform.position.y;
        MeasurePath();
    }

    private void MeasurePath()
    {
        Vector3 o = transform.position;

        if (Physics.Raycast(o, Vector3.left, out RaycastHit hL,
                            pathScanDistance, solidLayers, QueryTriggerInteraction.Ignore))
            pathMinX = hL.point.x + 0.15f;
        else
            pathMinX = o.x - pathScanDistance;

        if (Physics.Raycast(o, Vector3.right, out RaycastHit hR,
                            pathScanDistance, solidLayers, QueryTriggerInteraction.Ignore))
            pathMaxX = hR.point.x - 0.15f;
        else
            pathMaxX = o.x + pathScanDistance;

        if (pathMaxX - pathMinX < 0.5f)
        {
            pathMinX = o.x - 4f;
            pathMaxX = o.x + 4f;
        }
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (suspended) return;

        float x = transform.position.x;
        float y = transform.position.y;

        // ── Y: smooth return to path ──────────────────────────────────
        if (returningY)
        {
            y = Mathf.SmoothDamp(y, pathY, ref currentVelY, 0.2f,
                                  returnSpeed, Time.deltaTime);
            if (Mathf.Abs(y - pathY) < 0.02f)
            {
                y = pathY;
                returningY = false;
                currentVelY = 0f;
            }
        }

        // ── X: smooth patrol ─────────────────────────────────────────
        float effective = speed * speedMultiplier;

        if (effective > 0.01f)
        {
            if (ObstacleAhead()) dirX *= -1f;

            float targetX = x + dirX * effective;
            float nextX = Mathf.SmoothDamp(x, targetX, ref currentVelX,
                                              smoothTime, effective, Time.deltaTime);

            // Bounce at path bounds
            if (nextX <= pathMinX) { nextX = pathMinX; dirX = 1f; currentVelX = 0f; }
            if (nextX >= pathMaxX) { nextX = pathMaxX; dirX = -1f; currentVelX = 0f; }

            x = nextX;
        }

        transform.position = new Vector3(x, y, 0f);
    }

    // ================================================================
    //  WALL-AWARE KNOCKBACK
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        suspended = false; // we handle position ourselves below
        returningY = false;
        currentVelX = 0f;
        currentVelY = 0f;

        // Velocity in units/sec
        Vector3 velocity = impulse / Mathf.Max(duration, 0.01f);
        velocity.z = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // Decelerate over time (ease out)
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f - t * t;   // quadratic ease-out
            Vector3 step = velocity * scale * dt;

            // Cast ahead to find walls
            step = StepWithWallBounce(step);

            Vector3 newPos = transform.position + step;
            newPos.z = 0f;
            transform.position = newPos;

            yield return null;
        }

        // Begin smooth return to path Y
        returningY = true;
        currentVelY = 0f;
    }

    /// <summary>
    /// Move by 'step', bouncing off walls found via BoxCast.
    /// Returns the actual displacement applied.
    /// </summary>
    private Vector3 StepWithWallBounce(Vector3 step)
    {
        if (step.sqrMagnitude < 0.00001f) return step;

        float dist = step.magnitude;
        Vector3 dir = step / dist;

        RaycastHit hit;
        bool found = Physics.BoxCast(
            transform.position,
            knockbackCastHalf,
            dir,
            out hit,
            Quaternion.identity,
            dist,
            solidLayers,
            QueryTriggerInteraction.Ignore);

        if (!found) return step;

        // Is the hit collider our own?
        bool isSelf = false;
        foreach (var sc in selfColliders)
            if (sc == hit.collider) { isSelf = true; break; }
        if (isSelf) return step;

        // Move to just before the wall
        float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
        Vector3 safeStep = dir * safeDistance;

        // Reflect remaining velocity off the wall normal
        Vector3 remaining = step - safeStep;
        Vector3 normal = hit.normal; normal.z = 0f;
        if (normal.sqrMagnitude > 0.001f)
        {
            Vector3 reflected = Vector3.Reflect(remaining, normal.normalized) * bounceDamping;
            reflected.z = 0f;
            return safeStep + reflected;
        }

        return safeStep;
    }

    // ================================================================
    //  OBSTACLE DETECTION (patrol turn)
    // ================================================================

    private bool ObstacleAhead()
    {
        Vector3 dir = new Vector3(dirX, 0f, 0f);
        Vector3 center = transform.position + dir * turnLookAhead;

        Collider[] hits = Physics.OverlapBox(center, turnBoxHalfExtents,
                                              Quaternion.identity, solidLayers,
                                              QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (IsSelf(hit)) continue;
            return true;
        }
        return false;
    }

    private bool IsSelf(Collider c)
    {
        foreach (var sc in selfColliders)
            if (sc == c) return true;
        return false;
    }

    // ================================================================
    //  GIZMOS
    // ================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(pathMinX, pathY, 0f), new Vector3(pathMaxX, pathY, 0f));
        Gizmos.DrawWireSphere(new Vector3(pathMinX, pathY, 0f), 0.15f);
        Gizmos.DrawWireSphere(new Vector3(pathMaxX, pathY, 0f), 0.15f);

        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + new Vector3(dirX, 0f, 0f) * turnLookAhead;
        Gizmos.DrawWireCube(center, turnBoxHalfExtents * 2f);
    }
}