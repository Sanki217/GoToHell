using UnityEngine;

/// <summary>
/// Code-driven wall bounce for objects with trigger colliders.
///
/// Attach to any Rigidbody object (Soul, HealthOrb, UpgradeOrb, etc.)
/// that uses a trigger collider and therefore doesn't get physics wall
/// collisions. This script tracks actual displacement each FixedUpdate
/// and, if the object crossed into a wall, snaps it back to the surface
/// and reflects its velocity — one clean bounce, like a pool ball.
///
/// IMPORTANT — DAMP COROUTINE INTEGRATION:
///   Soul / HealthOrb / UpgradeOrb each have an InitialDampCoroutine that
///   overwrites rb.linearVelocity every Update frame. After a bounce, we
///   expose bounceOccurred + postBounceVelocity so those coroutines can
///   update their lerp start to the reflected direction. Without this,
///   the coroutine would snap the velocity back to the pre-bounce direction.
///
/// EXECUTION ORDER 500: runs AFTER enemy movement scripts so it sees
/// the final position from the previous physics step.
///
/// SETUP:
///   1. Add this component to the prefab (alongside the Rigidbody).
///   2. Set wallLayers to the "Wall" layer (and "Ground" if floors should bounce too).
///   3. Tune bounceRadius to roughly match the object's visual size.
///   4. That's it — works automatically with any damp coroutine that
///      checks bounceOccurred.
/// </summary>
[DefaultExecutionOrder(500)]
[RequireComponent(typeof(Rigidbody))]
public class WallBounce : MonoBehaviour
{
    [Header("Wall Detection")]
    [Tooltip("Layers considered solid walls. Usually just 'Wall' and 'Ground'.")]
    public LayerMask wallLayers;

    [Tooltip("Radius of the SphereCast used for wall detection. " +
             "Should roughly match the object's visual radius.")]
    public float bounceRadius = 0.15f;

    [Tooltip("How much velocity is preserved after a bounce (0 = dead stop, 1 = perfect bounce).")]
    [Range(0f, 1f)]
    public float bounciness = 0.6f;

    [Tooltip("Minimum velocity magnitude to bother bouncing. Below this, just stop.")]
    public float minBounceSpeed = 0.3f;

    // ── Bounce state — read by damp coroutines in Soul / HealthOrb / UpgradeOrb ──

    /// <summary>
    /// True for one frame after a bounce. The damp coroutine should check this,
    /// update its startVelocity to postBounceVelocity, and clear the flag.
    /// </summary>
    [HideInInspector] public bool bounceOccurred;

    /// <summary>
    /// The reflected velocity after the bounce (already scaled by bounciness).
    /// </summary>
    [HideInInspector] public Vector3 postBounceVelocity;

    private Rigidbody rb;
    private Vector3 lastPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        lastPosition = transform.position;
        lastPosition.z = 0f;
    }

    private void FixedUpdate()
    {
        // Don't bounce while kinematic (being attracted toward player)
        if (rb.isKinematic)
        {
            lastPosition = transform.position;
            lastPosition.z = 0f;
            return;
        }

        Vector3 currentPos = transform.position;
        currentPos.z = 0f;

        Vector3 displacement = currentPos - lastPosition;
        if (displacement.sqrMagnitude < 0.0001f)
        {
            lastPosition = currentPos;
            return;
        }

        float dist = displacement.magnitude;
        Vector3 dir = displacement / dist;

        // SphereCast from where we WERE toward where we ARE now
        if (Physics.SphereCast(lastPosition, bounceRadius, dir, out RaycastHit hit,
                               dist + 0.02f, wallLayers, QueryTriggerInteraction.Ignore))
        {
            // ── Snap to safe position just before the wall ──
            float safeDistance = Mathf.Max(0f, hit.distance - 0.01f);
            Vector3 safePos = lastPosition + dir * safeDistance;
            transform.position = new Vector3(safePos.x, safePos.y, 0f);

            // ── Reflect velocity off wall normal (2D only) ──
            Vector3 normal = hit.normal;
            normal.z = 0f;
            if (normal.sqrMagnitude < 0.001f)
            {
                // Degenerate normal — just kill velocity
                rb.linearVelocity = Vector3.zero;
                lastPosition = transform.position;
                lastPosition.z = 0f;
                return;
            }
            normal.Normalize();

            Vector3 vel = rb.linearVelocity;
            vel.z = 0f;

            if (vel.sqrMagnitude >= minBounceSpeed * minBounceSpeed)
            {
                Vector3 reflected = Vector3.Reflect(vel, normal) * bounciness;
                reflected.z = 0f;
                rb.linearVelocity = new Vector3(reflected.x, reflected.y, 0f);

                // Expose for damp coroutine integration
                bounceOccurred = true;
                postBounceVelocity = rb.linearVelocity;
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }

            lastPosition = transform.position;
            lastPosition.z = 0f;
        }
        else
        {
            lastPosition = currentPos;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, bounceRadius);
    }
#endif
}
