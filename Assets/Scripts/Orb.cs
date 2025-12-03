using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Orb : MonoBehaviour
{
    [Header("Orb")]
    public int value = 1;                     // how many orbs this gives
    public float initialDampDuration = 0.6f;  // how long the initial eject eases out
    public float attractDelay = 0.15f;        // short delay before it can be attracted
    public float minAttractSpeed = 6f;
    public float maxAttractSpeed = 18f;
    public float attractAccelerationTime = 0.6f; // how fast the attraction speeds up (ease-in)

    Rigidbody rb;
    bool isAttracted = false;
    Transform attractTarget;
    PlayerInventory targetInventory;
    float attractTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // safety: ensure Z=0 for 2.5D behavior if you want
        Vector3 p = transform.position;
        p.z = 0f;
        transform.position = p;
    }

    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        // apply immediate impulse (uses linearVelocity)
        rb.linearVelocity = ejectDir.normalized * ejectForce;

        // start coroutine to damp velocity over time (ease out)
        StartCoroutine(InitialDampCoroutine());
    }

    IEnumerator InitialDampCoroutine()
    {
        float t = 0f;
        Vector3 startVel = rb.linearVelocity;

        while (t < initialDampDuration)
        {
            t += Time.deltaTime;
            float alpha = t / initialDampDuration;
            // ease out (1 - (1-alpha)^2)
            float ease = 1f - Mathf.Pow(1f - alpha, 2f);
            rb.linearVelocity = Vector3.Lerp(startVel, Vector3.zero, ease);
            yield return null;
        }

        rb.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// Called by Looter when orb enters looter trigger
    /// </summary>
    public void StartAttract(Transform playerTransform, PlayerInventory inventory)
    {
        if (isAttracted) return;
        isAttracted = true;
        attractTarget = playerTransform;
        targetInventory = inventory;
        attractTimer = 0f;

        // stop physics so we directly control position (optional)
        rb.isKinematic = true;

        // start attraction coroutine
        StartCoroutine(AttractCoroutine());
    }

    IEnumerator AttractCoroutine()
    {
        Vector3 startPos = transform.position;
        float t = 0f;

        // We'll accelerate speed from min to max over attractAccelerationTime
        while (true)
        {
            if (attractTarget == null) break;

            // increase timer
            t += Time.deltaTime;
            float accelT = Mathf.Clamp01(t / attractAccelerationTime);
            // smooth step for acceleration (ease in)
            float accelEase = accelT * accelT;

            float speed = Mathf.Lerp(minAttractSpeed, maxAttractSpeed, accelEase);

            // move toward moving target position
            Vector3 dir = (attractTarget.position - transform.position);
            dir.z = 0f;
            float step = speed * Time.deltaTime;

            if (dir.magnitude <= step + 0.05f)
            {
                // reached player
                targetInventory?.AddOrbs(value);
                Destroy(gameObject);
                yield break;
            }

            transform.position += dir.normalized * step;
            yield return null;
        }
    }

    // optional: if player touches orb physically (not recommended because we use Looter trigger)
    private void OnTriggerEnter(Collider other)
    {
        if (isAttracted) return;

        if (other.TryGetComponent<PlayerInventory>(out PlayerInventory inv))
        {
            inv.AddOrbs(value);
            Destroy(gameObject);
        }
    }
}
