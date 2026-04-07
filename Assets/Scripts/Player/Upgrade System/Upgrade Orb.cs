using UnityEngine;
using System.Collections;

/// <summary>
/// Generic upgrade orb component. Attach this to every upgrade orb prefab alongside
/// a PlayerUpgrade subclass (e.g. UpgradeBurningArrow).
///
/// Responsibilities:
///   - Soul-style attraction toward the player
///   - On arrival: passes rolled stat bonuses to PlayerStats,
///     then calls PlayerUpgrade.OnAdded() for behaviour wiring
///
/// The PlayerUpgrade subclass on the same prefab provides all tuning and display data.
/// This script never needs to know which upgrade it is.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class UpgradeOrb : MonoBehaviour
{
    [Header("Attract Movement")]
    public float initialDampDuration = 0.5f;
    public float minAttractSpeed = 5f;
    public float maxAttractSpeed = 16f;
    public float attractAccelerationTime = 0.6f;
    public float shrinkStartDistance = 1.5f;

    // Set by the pool after rolling — not configured in prefab Inspector
    [HideInInspector] public UpgradeStatBonus[] rolledStatBonuses;
    [HideInInspector] public UpgradeRarity rolledRarity;

    private Rigidbody rb;
    private bool isAttracted = false;
    private Transform attractTarget;
    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private Vector3 originalScale;
    private Coroutine dampRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>Called by spawn site to give the orb an initial pop.</summary>
    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        rb.linearVelocity = ejectDir.normalized * ejectForce;
        dampRoutine = StartCoroutine(InitialDampCoroutine());
    }

    /// <summary>Called by Looter when the orb enters loot range.</summary>
    public void StartAttract(Transform playerTransform, PlayerUpgradeManager mgr, PlayerStats stats)
    {
        if (isAttracted) return;
        isAttracted = true;

        if (dampRoutine != null) { StopCoroutine(dampRoutine); dampRoutine = null; }

        attractTarget = playerTransform;
        upgradeManager = mgr;
        playerStats = stats;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        StartCoroutine(AttractCoroutine());
    }

    IEnumerator InitialDampCoroutine()
    {
        float t = 0f;
        Vector3 startVel = rb.linearVelocity;
        while (t < initialDampDuration)
        {
            if (rb.isKinematic) yield break;
            t += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / initialDampDuration), 2f);
            rb.linearVelocity = Vector3.Lerp(startVel, Vector3.zero, ease);
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
    }

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

            if (dist <= 0.15f)
            {
                Apply();
                Destroy(gameObject);
                yield break;
            }

            float scaleT = Mathf.Clamp01(dist / shrinkStartDistance);
            transform.localScale = originalScale * scaleT;

            float step = Mathf.Min(speed * dt, dist - 0.15f);
            rb.MovePosition(transform.position + toPlayer.normalized * step);
        }
    }

    private void Apply()
    {
        // Apply rolled stat bonuses
        if (playerStats != null && rolledStatBonuses != null)
            foreach (var bonus in rolledStatBonuses)
                bonus.Apply(playerStats);

        // Apply the upgrade behaviour
        if (upgradeManager != null)
        {
            PlayerUpgrade upgrade = GetComponent<PlayerUpgrade>();
            if (upgrade != null)
            {
                // Detach from the orb so it survives destruction
                upgrade.transform.SetParent(upgradeManager.transform);
                upgradeManager.ApplyUpgrade(upgrade);
            }
        }
    }
}