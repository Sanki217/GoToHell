using UnityEngine;

/// <summary>
/// Code-driven wall bounce for objects with trigger colliders.
///
/// Attach to any Rigidbody object (Soul, HealthOrb, UpgradeOrb, etc.)
/// that uses a trigger collider and therefore doesn't get physics wall
/// collisions. This script raycasts ahead of the object's velocity
/// and reflects it off walls — just like a real bounce.
///
/// Works in both physics-driven mode (rb.linearVelocity) and kinematic
/// mode (rb.MovePosition) by checking the actual displacement each
/// FixedUpdate frame.
///
/// SETUP:
///   1. Add this component to the prefab (alongside the Rigidbody).
///   2. Set wallLayers to the "Wall" layer (and "Ground" if floors should bounce too).
///   3. Set bounceRadius to roughly match the object's visual size.
///   4. That's it — works automatically.
/// </summary>
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
    public float minBounceSpeed = 0.5f;

    [Header("Safety")]
    [Tooltip("Max bounces per FixedUpdate frame (prevents infinite corner loops).")]
    public int maxBouncesPerFrame = 3;

    private Rigidbody rb;
    private Vector3 lastPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Don't bounce while kinematic (being attracted toward player)
        if (rb.isKinematic) return;

        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;
        if (vel.sqrMagnitude < minBounceSpeed * minBounceSpeed) return;

        float dt = Time.fixedDeltaTime;
        Vector3 step = vel * dt;
        float dist = step.magnitude;
        if (dist < 0.001f) return;

        Vector3 dir = step / dist;

        for (int bounce = 0; bounce < maxBouncesPerFrame; bounce++)
        {
            if (!Physics.SphereCast(transform.position, bounceRadius, dir, out RaycastHit hit,
                                     dist + 0.02f, wallLayers, QueryTriggerInteraction.Ignore))
                break; // Clear path — no bounce needed

            // Snap to safe position just before the wall
            float safeDistance = Mathf.Max(0f, hit.distance - 0.01f);
            transform.position += dir * safeDistance;

            // Reflect velocity off wall normal (2D only)
            Vector3 normal = hit.normal;
            normal.z = 0f;
            if (normal.sqrMagnitude < 0.001f) break;
            normal.Normalize();

            vel = Vector3.Reflect(vel, normal) * bounciness;
            vel.z = 0f;

            // Update for next iteration
            float remaining = dist - safeDistance;
            if (remaining < 0.001f || vel.sqrMagnitude < minBounceSpeed * minBounceSpeed) break;

            dir = vel.normalized;
            dist = remaining * bounciness;
        }

        rb.linearVelocity = new Vector3(vel.x, vel.y, 0f);
        lastPosition = transform.position;
    }
}
