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

    private Rigidbody rb;
    private bool isAttracted = false;
    private bool collected = false;
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

    // ================================================================
    //  START ATTRACT — called by Looter when soul enters loot range
    // ================================================================

    public void StartAttract(Transform playerTransform, PlayerInventory inventory)
    {
        if (isAttracted || collected) return;
        isAttracted = true;
        attractTarget = playerTransform;
        targetInventory = inventory;

        if (dampRoutine != null) { StopCoroutine(dampRoutine); dampRoutine = null; }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        StartCoroutine(AttractCoroutine());
    }

    // ================================================================
    //  DIRECT CONTACT — SoulLooter child trigger collider on player
    //
    //  Tag the SoulLooter child GameObject with the tag "SoulLooter".
    //  When the soul physically touches that collider it collects
    //  immediately — no coroutine stalling possible.
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("SoulLooter")) return;

        // Grab inventory from the player root if not yet set
        if (targetInventory == null)
        {
            targetInventory = other.transform.root.GetComponent<PlayerInventory>();
        }

        Collect();
    }

    // ================================================================
    //  ATTRACT COROUTINE — moves soul toward player root
    // ================================================================

    IEnumerator AttractCoroutine()
    {
        float elapsed = 0f;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (attractTarget == null || collected) yield break;

            float dt = Time.fixedDeltaTime;
            elapsed += dt;

            Vector3 toPlayer = attractTarget.position - transform.position;
            toPlayer.z = 0f;
            float dist = toPlayer.magnitude;

            // Distance arrival
            if (dist <= arrivalDistance) { Collect(); yield break; }

            // Timeout snap
            if (elapsed >= timeoutSeconds && dist <= snapDistance) { Collect(); yield break; }

            float t = Mathf.Clamp01(elapsed / attractAccelerationTime);
            float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, t * t);

            // Escalation: speed grows linearly over time, uncapped.
            // Near the player this barely matters. When falling away, this
            // guarantees the soul always catches up eventually.
            float escalation = elapsed * speedEscalationPerSecond;
            speed += escalation;

            float scaleT = Mathf.Clamp01(dist / shrinkStartDistance);
            transform.localScale = originalScale * scaleT;

            float step = Mathf.Min(speed * dt, Mathf.Max(0f, dist - arrivalDistance));
            rb.MovePosition(transform.position + toPlayer.normalized * step);
        }
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;
        targetInventory?.AddSouls(value);
        Destroy(gameObject);
    }
}
