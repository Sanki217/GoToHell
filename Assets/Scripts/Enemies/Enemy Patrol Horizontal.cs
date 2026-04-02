using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class EnemyPatrolHorizontal : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    [Tooltip("Speed at which enemy slides back to path Y after knockback")]
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

    [Header("Freeze (set by FreezeEffect)")]
    [Range(0f, 1f)]
    public float speedMultiplier = 1f;

    // ================================================================
    //  STATE
    // ================================================================

    private float pathMinX, pathMaxX, pathY;
    private float dirX = 1f;
    private float turnCooldownTimer = 0f;

    private bool suspended = false;
    private bool returningY = false;
    private float returnVelY = 0f;

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

    private void FixedUpdate()
    {
        if (suspended) return;

        float dt = Time.deltaTime;
        float x = transform.position.x;
        float y = transform.position.y;

        // ── Y: smooth return to path using SmoothDamp ─────────────────
        if (returningY)
        {
            y = Mathf.SmoothDamp(y, pathY, ref returnVelY, 0.2f, returnSpeed, dt);
            if (Mathf.Abs(y - pathY) < 0.015f)
            {
                y = pathY;
                returningY = false;
                returnVelY = 0f;
            }
        }

        // ── X: constant-speed patrol ──────────────────────────────────
        float move = dirX * speed * speedMultiplier * dt;

        // Turn cooldown — prevents double-flip and velocity stutter
        turnCooldownTimer -= dt;
        if (turnCooldownTimer <= 0f && ObstacleAhead())
        {
            dirX *= -1f;
            turnCooldownTimer = turnCooldown;
        }

        float nextX = x + move;

        // Hard clamp at path bounds
        if (nextX <= pathMinX) { nextX = pathMinX; dirX = 1f; turnCooldownTimer = turnCooldown; }
        if (nextX >= pathMaxX) { nextX = pathMaxX; dirX = -1f; turnCooldownTimer = turnCooldown; }

        transform.position = new Vector3(nextX, y, 0f);
    }

    // ================================================================
    //  OBSTACLE DETECTION
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
    //  KNOCKBACK — called by Enemy.cs
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        suspended = false;
        returningY = false;
        returnVelY = 0f;

        Vector3 velocity = impulse / Mathf.Max(duration, 0.01f);
        velocity.z = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f - t * t;
            Vector3 step = velocity * scale * dt;

            step = StepWithWallBounce(step);

            transform.position = new Vector3(
                transform.position.x + step.x,
                transform.position.y + step.y,
                0f);

            yield return null;
        }

        returningY = true;
        returnVelY = 0f;
    }

    private Vector3 StepWithWallBounce(Vector3 step)
    {
        if (step.sqrMagnitude < 0.00001f) return step;

        float dist = step.magnitude;
        Vector3 dir = step / dist;

        bool found = Physics.BoxCast(
            transform.position, knockbackCastHalf, dir,
            out RaycastHit hit, Quaternion.identity, dist,
            solidLayers, QueryTriggerInteraction.Ignore);

        if (!found) return step;

        foreach (var sc in selfColliders)
            if (sc == hit.collider) return step;

        float safe = Mathf.Max(0f, hit.distance - 0.05f);
        Vector3 safeStep = dir * safe;
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
    //  GIZMOS
    // ================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(pathMinX, pathY, 0f), new Vector3(pathMaxX, pathY, 0f));
        Gizmos.DrawWireSphere(new Vector3(pathMinX, pathY, 0f), 0.15f);
        Gizmos.DrawWireSphere(new Vector3(pathMaxX, pathY, 0f), 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position + new Vector3(dirX, 0f, 0f) * turnLookAhead,
            turnBoxHalfExtents * 2f);
    }
}