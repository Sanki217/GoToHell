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
        Vector3 startVelocity = rb.linearVelocity;

        while (t < initialDampDuration)
        {
            t += Time.deltaTime;
            float alpha = t / initialDampDuration;
            // ease out (1 - (1-alpha)^2)
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
        attractTarget = playerTransform;
        targetInventory = inventory;
        attractTimer = 0f;

        // stop physics so we directly control position (optional)
       // rb.isKinematic = true;

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

            
            Vector3 dir = (attractTarget.position - transform.position);
            dir.z = 0f;
            float step = speed * Time.deltaTime;

            if (dir.magnitude <= step + 0.05f)
            {
                targetInventory?.AddOrbs(value);
                Destroy(gameObject);
                yield break;
            }

            transform.position += dir.normalized * step;
            yield return null;
        }
    }
}
