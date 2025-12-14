using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Orb : MonoBehaviour
{
    [Header("Orb")]
    public int value = 1;
    public float initialDampDuration = 0.6f;
    public float attractDelay = 0.15f;
    public float minAttractSpeed = 6f;
    public float maxAttractSpeed = 18f;
    public float attractAccelerationTime = 0.6f;

    Rigidbody rb;
    bool isAttracted = false;
    Transform attractTarget;
    PlayerInventory targetInventory;
    float attractTimer = 0f;

    // NEW: handle for damping coroutine
    Coroutine dampRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        rb.linearVelocity = ejectDir.normalized * ejectForce;

        // NEW: store coroutine
        dampRoutine = StartCoroutine(InitialDampCoroutine());
    }

    IEnumerator InitialDampCoroutine()
    {
        float t = 0f;
        Vector3 startVelocity = rb.linearVelocity;

        while (t < initialDampDuration)
        {
            // NEW: stop immediately if orb switched to kinematic
            if (rb.isKinematic)
                yield break;

            t += Time.deltaTime;
            float alpha = t / initialDampDuration;

            float ease = 1f - Mathf.Pow(1f - alpha, 2f);
            rb.linearVelocity = Vector3.Lerp(startVelocity, Vector3.zero, ease);

            yield return null;
        }

        rb.linearVelocity = Vector3.zero;
    }

    public void StartAttract(Transform playerTransform, PlayerInventory inventory)
    {
        if (isAttracted) return;
        isAttracted = true;

        // stop damping coroutine if active
        if (dampRoutine != null)
        {
            StopCoroutine(dampRoutine);
            dampRoutine = null;
        }

        attractTarget = playerTransform;
        targetInventory = inventory;
        attractTimer = 0f;

        // stop physics BEFORE switching to kinematic
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // now it is safe to disable physics
        rb.isKinematic = true;

        StartCoroutine(AttractCoroutine());
    }


    IEnumerator AttractCoroutine()
    {
        Vector3 startPos = transform.position;
        float t = 0f;

        while (true)
        {
            if (attractTarget == null)
                break;

            t += Time.deltaTime;
            float accelT = Mathf.Clamp01(t / attractAccelerationTime);
            float accelEase = accelT * accelT;

            float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, accelEase);

            Vector3 dir = (attractTarget.position - transform.position);
            dir.z = 0f;
            float step = speed * Time.deltaTime;

            if (dir.magnitude <= step + 0.05f)
            {
                targetInventory?.AddOrbs(value);
                Destroy(gameObject);
                yield break;
            }

            rb.MovePosition(transform.position + dir.normalized * step);

            yield return null;
        }
    }
}
