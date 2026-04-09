using UnityEngine;
using System.Collections;

/// <summary>
/// Generic upgrade orb component. Attach to every upgrade orb prefab alongside a PlayerUpgrade subclass.
///
/// Responsibilities:
///   - Soul-style attraction toward the player
///   - On arrival: applies rolled stat bonuses (unless already applied by LevelUpUI),
///     then calls PlayerUpgrade.OnAdded() for behaviour wiring
///   - Direct pickup when touching the SoulLooter collider (same tag as Soul pickups)
///
/// statBonusesAlreadyApplied: set to true by LevelUpUI when it applies bonuses immediately
/// on card pick. This skips double-application while still calling OnAdded() for behaviour.
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

    [Header("Pickup Delay")]
    [Tooltip("Orb ignores attract / SoulLooter contact for this many real-time seconds after " +
             "spawning, giving the player a moment to see it before it flies in.")]
    public float attractDelay = 0.7f;

    // Set by spawn site after rolling - not configured in prefab Inspector
    [HideInInspector] public UpgradeStatBonus[] rolledStatBonuses;
    [HideInInspector] public UpgradeRarity rolledRarity;

    /// <summary>
    /// When true, Apply() skips stat bonus application (already done by LevelUpUI)
    /// but still calls OnAdded() for behaviour wiring.
    /// </summary>
    [HideInInspector] public bool statBonusesAlreadyApplied = false;

    private Rigidbody rb;
    private bool isAttracted = false;
    private bool attractQueued = false;
    private bool isCollected = false;
    private Transform attractTarget;
    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private Vector3 originalScale;
    private Coroutine dampRoutine;
    private float attractAvailableTime; // unscaled real time

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    public void Initialize(Vector3 ejectDir, float ejectForce)
    {
        attractAvailableTime = Time.unscaledTime + attractDelay;
        rb.linearVelocity = ejectDir.normalized * ejectForce;
        dampRoutine = StartCoroutine(InitialDampCoroutine());
    }

    public void StartAttract(Transform playerTransform, PlayerUpgradeManager mgr, PlayerStats stats)
    {
        if (isAttracted || isCollected || attractQueued) return;

        float remaining = attractAvailableTime - Time.unscaledTime;
        if (remaining > 0f)
        {
            // Too early to attract. Queue it with a coroutine so the orb still gets
            // pulled in even if OnTriggerEnter only fires once (orb spawned inside range).
            attractQueued = true;
            StartCoroutine(DelayedAttractCoroutine(playerTransform, mgr, stats, remaining));
            return;
        }

        BeginAttract(playerTransform, mgr, stats);
    }

    // ================================================================
    //  SOUL LOOTER direct contact - instant pickup (mirrors Soul.cs)
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("SoulLooter")) return;
        if (Time.unscaledTime < attractAvailableTime) return; // still in grace window

        // Grab refs from the player root if attract hasn't started yet
        if (upgradeManager == null)
            upgradeManager = other.transform.root.GetComponent<PlayerUpgradeManager>();
        if (playerStats == null)
            playerStats = other.transform.root.GetComponent<PlayerStats>();

        Apply();
        Destroy(gameObject);
    }

    // ================================================================
    //  PRIVATE HELPERS
    // ================================================================

    private IEnumerator DelayedAttractCoroutine(
        Transform playerTransform, PlayerUpgradeManager mgr, PlayerStats stats, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        attractQueued = false;
        BeginAttract(playerTransform, mgr, stats);
    }

    private void BeginAttract(Transform playerTransform, PlayerUpgradeManager mgr, PlayerStats stats)
    {
        if (isAttracted || isCollected) return;
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
            if (attractTarget == null || isCollected) break;

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
        if (isCollected) return;
        isCollected = true;

        // Apply stat bonuses only if LevelUpUI hasn't already done it
        if (!statBonusesAlreadyApplied && playerStats != null && rolledStatBonuses != null)
            foreach (var bonus in rolledStatBonuses)
                bonus.Apply(playerStats);

        // Always wire up the upgrade behaviour
        if (upgradeManager != null)
        {
            PlayerUpgrade upgrade = GetComponent<PlayerUpgrade>();
            if (upgrade != null)
            {
                upgrade.transform.SetParent(upgradeManager.transform);
                upgradeManager.ApplyUpgrade(upgrade);
            }
        }
    }
}
