using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Soul : MonoBehaviour
{
    [Header("Soul Value")]
    public int value = 1;

    [Header("Initial Eject Physics")]
    public float initialDampDuration = 0.6f;

    [Header("Attract Movement")]
    public float minAttractSpeed = 10f;   // raised from 6 — less likely to stall near centre
    public float maxAttractSpeed = 22f;   // raised from 18
    public float attractAccelerationTime = 0.4f;

    [Header("Arrival")]
    [Tooltip("Soul is collected when closer than this. Raised to prevent sub-step stalling.")]
    public float arrivalDistance = 0.4f;

    [Tooltip("If the soul has been attracting for this many seconds and is still within " +
             "snapDistance of the player, it snaps and collects immediately.")]
    public float timeoutSeconds = 3f;
    public float snapDistance = 1.5f;

    [Header("Shrink on Arrival")]
    public float shrinkStartDistance = 1.5f;

    private Rigidbody rb;
    private bool isAttracted = false;
    private Transform attractTarget;
    private PlayerInventory targetInventory;
    private Vector3 originalScale;
    private Coroutine dampRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        rb.linearVelocity = ejectDir.normalized * ejectForce;
        dampRoutine = StartCoroutine(InitialDampCoroutine());
    }

    IEnumerator InitialDampCoroutine()
    {
        float t = 0f;
        Vector3 startVelocity = rb.linearVelocity;

        while (t < initialDampDuration)
        {
            if (rb.isKinematic) yield break;
            t += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / initialDampDuration), 2f);
            rb.linearVelocity = Vector3.Lerp(startVelocity, Vector3.zero, ease);
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
    }

    public void StartAttract(Transform playerTransform, PlayerInventory inventory)
    {
        if (isAttracted) return;
        isAttracted = true;

        if (dampRoutine != null) { StopCoroutine(dampRoutine); dampRoutine = null; }

        attractTarget = playerTransform;
        targetInventory = inventory;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        StartCoroutine(AttractCoroutine());
    }

    // ================================================================
    //  WHY WaitForFixedUpdate:
    //  rb.MovePosition on a kinematic Rigidbody must be called once per
    //  physics step. WaitForFixedUpdate fires exactly once per physics
    //  step (50/sec) regardless of render fps — identical at 60 and 1975fps.
    // ================================================================

    IEnumerator AttractCoroutine()
    {
        float elapsed = 0f;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (attractTarget == null) break;

            float dt = Time.fixedDeltaTime;
            elapsed += dt;

            Vector3 toPlayer = attractTarget.position - transform.position;
            toPlayer.z = 0f;
            float dist = toPlayer.magnitude;

            // ── Primary arrival check ──────────────────────────────
            if (dist <= arrivalDistance)
            {
                Collect();
                yield break;
            }

            // ── Timeout snap: been attracting too long and still close ─
            // Catches the case where the soul oscillates just outside
            // arrivalDistance and never actually closes the gap.
            if (elapsed >= timeoutSeconds && dist <= snapDistance)
            {
                Collect();
                yield break;
            }

            // ── Speed ramp ────────────────────────────────────────
            float t = Mathf.Clamp01(elapsed / attractAccelerationTime);
            float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, t * t);

            // ── Shrink ────────────────────────────────────────────
            float scaleT = Mathf.Clamp01(dist / shrinkStartDistance);
            transform.localScale = originalScale * scaleT;

            // ── Move — clamp step so we never overshoot arrival ───
            // The clamp is against (dist - arrivalDistance) so on the
            // last step we land exactly at arrivalDistance rather than
            // bouncing past and coming back.
            float step = Mathf.Min(speed * dt, Mathf.Max(0f, dist - arrivalDistance));
            rb.MovePosition(transform.position + toPlayer.normalized * step);
        }
    }

    private void Collect()
    {
        targetInventory?.AddSouls(value);
        Destroy(gameObject);
    }
}