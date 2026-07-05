using UnityEngine;

/// <summary>
/// Soul pickup. POOLED — spawn with Pool.Spawn; collected souls return to the
/// pool. Motion is ticked by SoulMotionManager in a single FixedUpdate.
///
/// KINEMATIC MOVEMENT, MANUAL COLLISION: the soul moves itself and checks the
/// Wall layer with a spherecast each step — the physics engine never resolves
/// contacts for it, so there is no residual jitter, and it ignores arrows,
/// other souls, and everything not on wallLayers by construction.
///
/// Bounce rule: exactly ONE reflected bounce off walls/platforms at
/// bounceRetainedSpeed (default half) of the impact velocity — mirror
/// direction, like physics. The next wall contact stops it dead.
///
/// Collection is unchanged (the fun part): Looter range starts the attraction,
/// SoulLooter contact collects instantly.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Soul : MonoBehaviour
{
    [Header("Soul Value")]
    public int value = 1;

    [Header("Eject Movement")]
    public float initialDampDuration = 0.6f;

    [Header("Wall Bounce")]
    [Tooltip("Layers the soul bounces off (once) and then stops on. Auto-set to 'Wall' if left empty.")]
    public LayerMask wallLayers;
    [Tooltip("Fraction of impact speed kept after the single bounce.")]
    [Range(0f, 1f)]
    public float bounceRetainedSpeed = 0.5f;
    [Tooltip("Radius of the movement spherecast — roughly the soul's visual radius.")]
    public float castRadius = 0.15f;

    [Header("Attract Movement")]
    public float minAttractSpeed = 10f;
    public float maxAttractSpeed = 22f;
    public float attractAccelerationTime = 0.4f;

    [Header("Arrival")]
    public float arrivalDistance = 0.4f;
    public float timeoutSeconds = 3f;
    public float snapDistance = 1.5f;

    [Header("Shrink on Arrival")]
    public float shrinkStartDistance = 1.5f;

    public float speedEscalationPerSecond = 8f;

    private const float Skin = 0.02f;   // gap kept from wall surfaces

    // ── state ───────────────────────────────────────────────────────
    private Rigidbody rb;
    private Vector3 originalScale;

    private bool collected;
    private bool isAttracted;
    private Transform attractTarget;
    private PlayerInventory targetInventory;
    private float attractElapsed;

    private Vector3 ejectVelocity;   // undamped base velocity; damp curve scales it
    private float ejectTimer;
    private bool hasBounced;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;    // engine never moves or resolves this body
        rb.useGravity = false;
        originalScale = transform.localScale;

        if (wallLayers == 0)
        {
            int wall = LayerMask.NameToLayer("Wall");
            if (wall >= 0) wallLayers = 1 << wall;
        }
    }

    // OnEnable runs on every pool reuse — full state reset here.
    void OnEnable()
    {
        collected = false;
        isAttracted = false;
        attractTarget = null;
        targetInventory = null;
        attractElapsed = 0f;
        ejectVelocity = Vector3.zero;
        ejectTimer = 0f;
        hasBounced = false;
        transform.localScale = originalScale;

        SoulMotionManager.Register(this);
    }

    void OnDisable()
    {
        SoulMotionManager.Unregister(this);
    }

    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        ejectVelocity = ejectDir.normalized * ejectForce;
        ejectTimer = 0f;
        hasBounced = false;
    }

    // ================================================================
    //  START ATTRACT — called by Looter when soul enters loot range
    // ================================================================

    public void StartAttract(Transform playerTransform, PlayerInventory inventory)
    {
        if (isAttracted || collected || playerTransform == null) return;
        isAttracted = true;
        attractTarget = playerTransform;
        targetInventory = inventory;
        attractElapsed = 0f;
        ejectVelocity = Vector3.zero;
    }

    // ================================================================
    //  DIRECT CONTACT — SoulLooter child trigger collider on player
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("SoulLooter")) return;

        if (targetInventory == null)
            targetInventory = other.transform.root.GetComponent<PlayerInventory>();

        Collect();
    }

    // ================================================================
    //  TICK — called by SoulMotionManager from a single FixedUpdate
    // ================================================================

    public void Tick(float dt)
    {
        if (collected) return;

        if (isAttracted)
        {
            AttractTick(dt);
            return;
        }

        EjectTick(dt);
    }

    private void EjectTick(float dt)
    {
        if (ejectVelocity == Vector3.zero || ejectTimer >= initialDampDuration) return;

        ejectTimer += dt;

        // Ease-out damping: current speed = base * (1 - t)^2.
        // Halving the base at a bounce halves the current speed too, so the
        // "half the power it had when colliding" rule holds exactly.
        float remaining = 1f - Mathf.Clamp01(ejectTimer / initialDampDuration);
        Vector3 velocity = ejectVelocity * (remaining * remaining);

        Vector3 step = velocity * dt;
        float dist = step.magnitude;
        if (dist < 0.0001f) return;

        Vector3 dir = step / dist;

        if (Physics.SphereCast(transform.position, castRadius, dir, out RaycastHit hit,
                               dist + Skin, wallLayers, QueryTriggerInteraction.Ignore))
        {
            // Advance to the contact point (minus skin), never into the wall
            Vector3 contactPos = transform.position + dir * Mathf.Max(0f, hit.distance - Skin);
            rb.MovePosition(contactPos);

            if (!hasBounced)
            {
                hasBounced = true;
                Vector3 reflected = Vector3.Reflect(ejectVelocity, hit.normal) * bounceRetainedSpeed;
                reflected.z = 0f;
                ejectVelocity = reflected;
            }
            else
            {
                ejectVelocity = Vector3.zero;   // second wall contact — stop dead
            }
        }
        else
        {
            rb.MovePosition(transform.position + step);
        }
    }

    private void AttractTick(float dt)
    {
        if (attractTarget == null)
        {
            isAttracted = false;   // player gone (death/scene edge) — go idle
            return;
        }

        attractElapsed += dt;

        Vector3 toPlayer = attractTarget.position - transform.position;
        toPlayer.z = 0f;
        float dist = toPlayer.magnitude;

        // Distance arrival
        if (dist <= arrivalDistance) { Collect(); return; }

        // Timeout snap
        if (attractElapsed >= timeoutSeconds && dist <= snapDistance) { Collect(); return; }

        float t = Mathf.Clamp01(attractElapsed / attractAccelerationTime);
        float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, t * t);

        // Escalation: speed grows linearly over time, uncapped — the soul
        // always catches up eventually, even if the player is falling away.
        speed += attractElapsed * speedEscalationPerSecond;

        float scaleT = Mathf.Clamp01(dist / shrinkStartDistance);
        transform.localScale = originalScale * scaleT;

        float step = Mathf.Min(speed * dt, Mathf.Max(0f, dist - arrivalDistance));
        rb.MovePosition(transform.position + toPlayer.normalized * step);
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;
        targetInventory?.AddSouls(value);
        Pool.Despawn(gameObject);
    }
}
