using UnityEngine;
using System.Collections;

/// <summary>
/// Health Orb — soul-style pickup that restores HP on collection.
///
/// BEHAVIOUR:
///   - Ejected on spawn with a small burst (Initialize).
///   - Looter's sphere trigger calls StartAttract when the orb enters range.
///   - Direct contact with the "SoulLooter" child collider collects immediately.
///
/// SETUP:
///   1. Create a prefab with Rigidbody (freeze Z pos + all rotation).
///   2. Add a Sphere Collider (Is Trigger = true).
///   3. Add this script and set healAmount.
///   4. Assign in Vase Inspector as healthOrbPrefab.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HealthOrb : MonoBehaviour
{
    [Header("Heal Value")]
    [Tooltip("HP restored when the orb is collected.")]
    public int healAmount = 5;

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

    private Rigidbody rb;
    private bool isAttracted = false;
    private bool collected = false;
    private Transform attractTarget;
    private PlayerHealth targetHealth;
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
        WallBounce wallBounce = GetComponent<WallBounce>();

        while (t < initialDampDuration)
        {
            if (rb.isKinematic) yield break;
            t += Time.deltaTime;

            // If WallBounce reflected us, adopt the new direction
            if (wallBounce != null && wallBounce.bounceOccurred)
            {
                startVelocity = wallBounce.postBounceVelocity;
                wallBounce.bounceOccurred = false;
            }

            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / initialDampDuration), 2f);
            rb.linearVelocity = Vector3.Lerp(startVelocity, Vector3.zero, ease);
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
    }

    // ================================================================
    //  START ATTRACT — called by Looter when orb enters loot range
    // ================================================================

    public void StartAttract(Transform playerTransform, PlayerHealth health)
    {
        if (isAttracted || collected) return;
        isAttracted = true;
        attractTarget = playerTransform;
        targetHealth = health;

        if (dampRoutine != null) { StopCoroutine(dampRoutine); dampRoutine = null; }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        StartCoroutine(AttractCoroutine());
    }

    // ================================================================
    //  DIRECT CONTACT — "SoulLooter" child trigger on player
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("SoulLooter")) return;

        if (targetHealth == null)
            targetHealth = other.transform.root.GetComponent<PlayerHealth>();

        Collect();
    }

    // ================================================================
    //  ATTRACT COROUTINE
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

            if (dist <= arrivalDistance) { Collect(); yield break; }
            if (elapsed >= timeoutSeconds && dist <= snapDistance) { Collect(); yield break; }

            float t = Mathf.Clamp01(elapsed / attractAccelerationTime);
            float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, t * t);

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
        targetHealth?.RestoreHP(healAmount);
        Destroy(gameObject);
    }
}
