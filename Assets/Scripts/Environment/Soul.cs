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
    public float minAttractSpeed = 6f;
    public float maxAttractSpeed = 18f;
    public float attractAccelerationTime = 0.5f;

    [Header("Shrink on Arrival")]
    [Tooltip("Distance at which the soul starts shrinking to zero")]
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
    //
    //  rb.MovePosition on a kinematic Rigidbody must be called once per
    //  physics step. Using "yield return null" (render frame) calls it
    //  33+ times per physics step at high fps — the body tries to move
    //  to many different targets before any step resolves, causing souls
    //  to orbit and overshoot instead of arriving cleanly.
    //
    //  WaitForFixedUpdate fires exactly once per physics step (50/sec)
    //  regardless of render fps. Souls now behave identically at 60fps
    //  and 1975fps.
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

            float t = Mathf.Clamp01(elapsed / attractAccelerationTime);
            float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, t * t);

            Vector3 toPlayer = attractTarget.position - transform.position;
            toPlayer.z = 0f;
            float dist = toPlayer.magnitude;

            // Fixed world-space arrival — same at any framerate
            if (dist <= 0.15f)
            {
                targetInventory?.AddSouls(value);
                Destroy(gameObject);
                yield break;
            }

            // Shrink as soul closes in
            float scaleT = Mathf.Clamp01(dist / shrinkStartDistance);
            transform.localScale = originalScale * scaleT;

            // Clamp step so we never overshoot the arrival threshold
            float step = Mathf.Min(speed * dt, dist - 0.15f);
            rb.MovePosition(transform.position + toPlayer.normalized * step);
        }
    }
}