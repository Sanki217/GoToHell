using UnityEngine;

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
    public Camera mainCamera;
    public LineRenderer lineRenderer;
    public Transform shootOrigin;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private Rigidbody rb;
    private PlayerEnergy playerEnergy;

    public bool isDashing = false;
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
        if (!mainCamera) mainCamera = Camera.main;

        // Damage collider starts disabled — only active during dash
        if (dashDamageCollider != null)
            dashDamageCollider.enabled = false;
    }

    void Update()
    {
        HandleInput();
        UpdateDashLine();
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
        if (Input.GetMouseButtonDown(1) && !isDashing)
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
            if (!playerEnergy.SpendEnergy(cost)) return;

            playerStats?.RecordEnergySpent(cost, EnergySpentSource.Dash);

            dashDirection = (dashTarget - origin).normalized;
            dashVelocity = dashDirection * (Vector3.Distance(origin, dashTarget) / dashDuration);
            dashTimer = dashDuration;
            dashStartDistance = Vector3.Distance(origin, dashTarget);
            isDashing = true;

            // Enable dash damage collider
            if (dashDamageCollider != null)
                dashDamageCollider.enabled = true;

            // Start invincibility window (covers dash + a bit after)
            float invincDuration = dashDuration +
                (playerStats != null ? playerStats.dashInvincibilityWindow : 0f);
            playerHealth?.StartDashInvincibility(invincDuration);

            upgradeManager?.DashStart();
        }
    }

    void EndDash()
    {
        playerStats?.RecordDash(dashStartDistance);
        isDashing = false;

        // Disable damage collider
        if (dashDamageCollider != null)
            dashDamageCollider.enabled = false;

        upgradeManager?.DashEnd();
        rb.linearVelocity = dashDirection * postDashMomentum;
    }

    void UpdateDashLine()
    {
        if (lineRenderer == null || shootOrigin == null) return;

        Vector3 cursorWorld = GetCursorWorldPosition();
        Vector3 origin = shootOrigin.position;
        Vector3 direction = (cursorWorld - origin).normalized;
        float intendedDistance = Mathf.Min(Vector3.Distance(origin, cursorWorld), CurrentDashRange);

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint = Physics.Raycast(ray, out RaycastHit hit, intendedDistance, dashCollisionLayers)
            ? hit.point
            : origin + direction * intendedDistance;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    Vector3 GetCursorWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, shootOrigin.position);
        if (plane.Raycast(ray, out float distance)) return ray.GetPoint(distance);
        return shootOrigin.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDashing) return;
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            playerStats?.RecordDashHitWall();
            upgradeManager?.DashHitWall();
        }
    }
}