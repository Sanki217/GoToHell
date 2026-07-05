using UnityEngine;

/// <summary>
/// Soul pickup. POOLED — spawn with Pool.Spawn, collected souls return to the
/// pool. Motion (eject damping + attraction) is ticked by SoulMotionManager
/// in a single FixedUpdate for all souls — no per-soul coroutines.
///
/// Lifecycle: Pool.Spawn → Initialize(ejectDir, force) → [Looter calls
/// StartAttract OR SoulLooter trigger contact] → Collect → Pool.Despawn.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Soul : MonoBehaviour
{
    [Header("Soul Value")]
    public int value = 1;

    [Header("Initial Eject Physics")]
    public float initialDampDuration = 0.6f;

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

    // ── state ───────────────────────────────────────────────────────
    private Rigidbody rb;
    private Vector3 originalScale;

    private bool collected;
    private bool isAttracted;
    private Transform attractTarget;
    private PlayerInventory targetInventory;
    private float attractElapsed;

    private bool damping;
    private float dampTimer;
    private Vector3 dampStartVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    // OnEnable runs on every pool reuse — full state reset here.
    void OnEnable()
    {
        collected = false;
        isAttracted = false;
        attractTarget = null;
        targetInventory = null;
        attractElapsed = 0f;
        damping = false;
        dampTimer = 0f;
        transform.localScale = originalScale;
        rb.isKinematic = false;

        SoulMotionManager.Register(this);
    }

    void OnDisable()
    {
        SoulMotionManager.Unregister(this);
    }

    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        rb.linearVelocity = ejectDir.normalized * ejectForce;
        dampStartVelocity = rb.linearVelocity;
        dampTimer = 0f;
        damping = true;
    }

    // ================================================================
    //  START ATTRACT — called by Looter when soul enters loot range
    // ================================================================

    public void StartAttract(Transform playerTransform, PlayerInventory inventory)
    {
        if (isAttracted || collected) return;
        isAttracted = true;
        attractTarget = playerTransform;
        targetInventory = inventory;
        attractElapsed = 0f;
        damping = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
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

        if (isAttracted) { AttractTick(dt); return; }
        if (damping) DampTick(dt);
    }

    private void DampTick(float dt)
    {
        if (rb.isKinematic) { damping = false; return; }

        dampTimer += dt;
        float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(dampTimer / initialDampDuration), 2f);
        rb.linearVelocity = Vector3.Lerp(dampStartVelocity, Vector3.zero, ease);

        if (dampTimer >= initialDampDuration)
        {
            rb.linearVelocity = Vector3.zero;
            damping = false;
        }
    }

    private void AttractTick(float dt)
    {
        if (attractTarget == null) { isAttracted = false; return; }

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
