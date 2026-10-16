using UnityEngine;

/// <summary>
/// Rogue RMB skill: spend energy to dash toward the cursor (physics slide with
/// a damage collider), or — with Phantom Step — teleport there.
///
/// Input is ignored while the player has no control (menus, level-up,
/// resurrection). Teleports never end inside solid geometry: the landing
/// point is pulled back along the dash line until the player fits.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings — defaults, overridden by PlayerStats at runtime")]
    public float dashCost = 20f;
    public float maxDashRange = 10f;
    public float dashDuration = 0.3f;
    public float postDashMomentum = 10f;
    public int dashDamage = 1;
    public LayerMask dashCollisionLayers;

    [Header("Dash Damage Collider")]
    [Tooltip("Assign a child GameObject that has a Trigger Collider on it. " +
             "It will be enabled only during the dash to deal damage to enemies.")]
    public Collider dashDamageCollider;

    [Header("References")]
    public LineRenderer lineRenderer;
    public Transform shootOrigin;

    [Header("Teleport Safety (Phantom Step)")]
    [Tooltip("Radius of the clearance check at the teleport landing point.")]
    public float teleportClearanceRadius = 0.4f;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private Rigidbody rb;
    private PlayerEnergy playerEnergy;
    private PlayerStateController stateCtrl;
    private Collider[] selfColliders;
    private static readonly Collider[] clearanceBuffer = new Collider[8];

    public bool isDashing = false;

    /// <summary>Increments every dash — lets hit detection reset its per-dash state.</summary>
    public int DashSerial { get; private set; }

    // ── Phantom Step teleport mode (activated by UpgradePhantomStep) ──
    /// <summary>When true, dash instantly teleports to cursor instead of physics-sliding.</summary>
    [HideInInspector] public bool isTeleportDash = false;

    /// <summary>
    /// Fired at the landing position when a teleport dash completes.
    /// Phantom Step subscribes here to apply its AoE explosion.
    /// </summary>
    public event System.Action<Vector3> OnTeleportLanded;

    private float dashTimer;
    private Vector3 dashDirection;
    private Vector3 dashVelocity;
    private float dashStartDistance;

    private float CurrentDashCost => playerStats != null ? playerStats.dashCost : dashCost;
    private float CurrentDashRange => playerStats != null ? playerStats.dashDistance : maxDashRange;
    private float CurrentDashDmg => playerStats != null ? playerStats.dashDamage : dashDamage;
    private float CurrentKnockback => playerStats != null ? playerStats.knockbackForce : 0f;

    void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        playerStats = GetComponent<PlayerStats>();
        playerHealth = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody>();
        playerEnergy = GetComponent<PlayerEnergy>();
        stateCtrl = GetComponent<PlayerStateController>();
        selfColliders = GetComponentsInChildren<Collider>(true);
        if (shootOrigin == null) shootOrigin = transform;

        // Damage collider starts disabled — only active during dash
        if (dashDamageCollider != null)
            dashDamageCollider.enabled = false;
    }

    void Update()
    {
        if (stateCtrl == null || stateCtrl.HasControl())
            HandleInput();
        UpdateDashLine();
    }

    private void OnDisable()
    {
        if (isDashing) EndDash();
    }

    void FixedUpdate()
    {
        if (!isDashing) return;

        rb.linearVelocity = dashVelocity;
        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }

    void HandleInput()
    {
        if (GameInput.SkillPressed && !isDashing && playerEnergy != null)
        {
            Vector3 cursorWorld = GetCursorWorldPosition();
            Vector3 origin = shootOrigin.position;
            Vector3 direction = (cursorWorld - origin).normalized;
            float distanceToCursor = Vector3.Distance(origin, cursorWorld);
            float intendedDistance = Mathf.Min(distanceToCursor, CurrentDashRange);

            Ray ray = new Ray(origin, direction);
            Vector3 dashTarget;

            if (Physics.Raycast(ray, out RaycastHit hit, intendedDistance, dashCollisionLayers))
            {
                dashTarget = hit.point;
                if (Vector3.Distance(origin, dashTarget) < 0.5f) return;
            }
            else
            {
                dashTarget = origin + direction * intendedDistance;
            }

            float cost = CurrentDashCost;
            if (!playerEnergy.SpendEnergy(cost))
            {
                playerEnergy.FlashInsufficient();
                return;
            }

            if (playerStats != null) playerStats.RecordEnergySpent(cost, EnergySpentSource.Dash);

            // ── PHANTOM STEP: instant teleport instead of physics dash ──
            if (isTeleportDash)
            {
                Vector3 tpTarget = FindTeleportLanding(origin, direction, intendedDistance);
                float tpDist = Vector3.Distance(origin, tpTarget);

                float invincTP = dashDuration +
                    (playerStats != null ? playerStats.dashInvincibilityWindow : 0f);
                if (playerHealth != null) playerHealth.StartDashInvincibility(invincTP);

                if (upgradeManager != null) upgradeManager.DashStart();

                // Move the body by the same offset the shoot origin travels
                Vector3 landing = transform.position + (tpTarget - origin);
                landing.z = transform.position.z;
                rb.position = landing;
                transform.position = landing;

                OnTeleportLanded?.Invoke(tpTarget);
                if (playerStats != null) playerStats.RecordDash(tpDist);
                if (upgradeManager != null) upgradeManager.DashEnd();
                return;
            }
            // ───────────────────────────────────────────────────────────

            dashDirection = (dashTarget - origin).normalized;
            dashVelocity = dashDirection * (Vector3.Distance(origin, dashTarget) / dashDuration);
            dashTimer = dashDuration;
            dashStartDistance = Vector3.Distance(origin, dashTarget);
            isDashing = true;
            DashSerial++;

            // Enable dash damage collider
            if (dashDamageCollider != null)
                dashDamageCollider.enabled = true;

            // Start invincibility window (covers dash + a bit after)
            float invincDuration = dashDuration +
                (playerStats != null ? playerStats.dashInvincibilityWindow : 0f);
            if (playerHealth != null) playerHealth.StartDashInvincibility(invincDuration);

            if (upgradeManager != null) upgradeManager.DashStart();
        }
    }

    /// <summary>
    /// Teleport target along the dash line. Phantom Step passes through thin
    /// walls, but must never land INSIDE geometry — walk back toward the
    /// origin until the landing point is clear.
    /// </summary>
    private Vector3 FindTeleportLanding(Vector3 origin, Vector3 direction, float distance)
    {
        const int Steps = 12;
        for (int i = 0; i <= Steps; i++)
        {
            float d = distance * (1f - (float)i / Steps);
            Vector3 p = origin + direction * d;
            p.z = origin.z;
            if (IsClear(p)) return p;
        }
        return origin;
    }

    private bool IsClear(Vector3 point)
    {
        if (dashCollisionLayers == 0) return true;
        int count = Physics.OverlapSphereNonAlloc(point, teleportClearanceRadius, clearanceBuffer,
                                                  dashCollisionLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            bool self = false;
            foreach (var c in selfColliders)
                if (c == clearanceBuffer[i]) { self = true; break; }
            if (!self) return false;
        }
        return true;
    }

    void EndDash()
    {
        if (playerStats != null) playerStats.RecordDash(dashStartDistance);
        isDashing = false;

        // Disable damage collider
        if (dashDamageCollider != null)
            dashDamageCollider.enabled = false;

        if (upgradeManager != null) upgradeManager.DashEnd();
        if (rb != null && !rb.isKinematic) rb.linearVelocity = dashDirection * postDashMomentum;
    }

    void UpdateDashLine()
    {
        if (lineRenderer == null || shootOrigin == null) return;

        Vector3 cursorWorld = GetCursorWorldPosition();
        Vector3 origin = shootOrigin.position;
        Vector3 direction = (cursorWorld - origin).normalized;
        float intendedDistance = Mathf.Min(Vector3.Distance(origin, cursorWorld), CurrentDashRange);

        Ray ray = new Ray(origin, direction);
        // In teleport mode the player passes through walls — show full-range preview.
        Vector3 endPoint = (!isTeleportDash && Physics.Raycast(ray, out RaycastHit hit, intendedDistance, dashCollisionLayers))
            ? hit.point
            : origin + direction * intendedDistance;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    // Cursor on mouse; aim-stick direction at full range on gamepad / touch
    Vector3 GetCursorWorldPosition() =>
        GameInput.AimPoint(shootOrigin.position, CurrentDashRange);

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDashing) return;
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            if (playerStats != null) playerStats.RecordDashHitWall();
            if (upgradeManager != null) upgradeManager.DashHitWall();
        }
    }
}