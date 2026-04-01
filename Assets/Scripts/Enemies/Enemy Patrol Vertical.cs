using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class EnemyPatrolVertical : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float acceleration = 20f;
    public float returnSpeed = 5f;

    [Header("Path — measured at spawn")]
    public float pathScanDistance = 60f;
    public LayerMask solidLayers;

    [Header("Turn Detection")]
    public float turnLookAhead = 0.55f;
    public Vector3 turnBoxHalfExtents = new Vector3(0.45f, 0.35f, 0.1f);

    [Header("Knockback Wall Bounce")]
    public Vector3 knockbackCastHalf = new Vector3(0.4f, 0.3f, 0.1f);
    [Range(0f, 1f)]
    public float bounceDamping = 0.6f;

    [Header("Freeze (set by FreezeEffect)")]
    [Range(0f, 1f)]
    public float speedMultiplier = 1f;

    // ================================================================
    //  STATE
    // ================================================================

    private float pathMinY, pathMaxY, pathX;
    private float dirY = 1f;
    private float currentVelY = 0f;

    private bool suspended = false;
    private bool returningX = false;

    private Collider[] selfColliders;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        pathX = transform.position.x;
        MeasurePath();
    }

    private void MeasurePath()
    {
        Vector3 o = transform.position;

        if (Physics.Raycast(o, Vector3.down, out RaycastHit hD,
                            pathScanDistance, solidLayers, QueryTriggerInteraction.Ignore))
            pathMinY = hD.point.y + 0.15f;
        else
            pathMinY = o.y - pathScanDistance;

        if (Physics.Raycast(o, Vector3.up, out RaycastHit hU,
                            pathScanDistance, solidLayers, QueryTriggerInteraction.Ignore))
            pathMaxY = hU.point.y - 0.15f;
        else
            pathMaxY = o.y + pathScanDistance;

        if (pathMaxY - pathMinY < 0.5f)
        {
            pathMinY = o.y - 4f;
            pathMaxY = o.y + 4f;
        }
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (suspended) return;

        float dt = Time.deltaTime;
        float x = transform.position.x;
        float y = transform.position.y;

        // ── X: return to path (framerate-independent) ─────────────────
        if (returningX)
        {
            x = Mathf.MoveTowards(x, pathX, returnSpeed * dt);
            if (Mathf.Abs(x - pathX) < 0.01f)
            {
                x = pathX;
                returningX = false;
            }
        }

        // ── Y: patrol movement ────────────────────────────────────────
        float targetSpeed = dirY * speed * speedMultiplier;
        currentVelY = Mathf.MoveTowards(currentVelY, targetSpeed, acceleration * dt);

        if (ObstacleAhead())
        {
            dirY *= -1f;
            currentVelY = 0f;
        }

        float nextY = y + currentVelY * dt;

        if (nextY <= pathMinY) { nextY = pathMinY; dirY = 1f; currentVelY = 0f; }
        if (nextY >= pathMaxY) { nextY = pathMaxY; dirY = -1f; currentVelY = 0f; }

        transform.position = new Vector3(x, nextY, 0f);
    }

    // ================================================================
    //  OBSTACLE DETECTION
    // ================================================================

    private bool ObstacleAhead()
    {
        Vector3 dir = new Vector3(0f, dirY, 0f);
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
    //  KNOCKBACK
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        suspended = false;
        returningX = false;
        currentVelY = 0f;

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

        returningX = true;
        currentVelY = 0f;
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
        Gizmos.DrawLine(new Vector3(pathX, pathMinY, 0f), new Vector3(pathX, pathMaxY, 0f));
        Gizmos.DrawWireSphere(new Vector3(pathX, pathMinY, 0f), 0.15f);
        Gizmos.DrawWireSphere(new Vector3(pathX, pathMaxY, 0f), 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position + new Vector3(0f, dirY, 0f) * turnLookAhead,
            turnBoxHalfExtents * 2f);
    }
}