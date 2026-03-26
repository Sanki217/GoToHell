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

    [Header("References")]
    public Camera mainCamera;
    public LineRenderer lineRenderer;
    public Transform shootOrigin;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
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
        rb = GetComponent<Rigidbody>();
        playerEnergy = GetComponent<PlayerEnergy>();
        if (!mainCamera) mainCamera = Camera.main;
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
            playerStats?.RecordDash(dashStartDistance);
            isDashing = false;
            upgradeManager?.DashEnd();
            rb.linearVelocity = dashDirection * postDashMomentum;
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
                // Wall hit detected at dash start — will be recorded in OnCollisionEnter during dash
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

            upgradeManager?.DashStart();
        }
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

    // Detect actual wall collision during dash
    private void OnCollisionEnter(Collision collision)
    {
        if (!isDashing) return;
        // If we hit something that's not an enemy, it's a wall
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            playerStats?.RecordDashHitWall();
            upgradeManager?.DashHitWall();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDashing || !other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        float baseDamage = CurrentDashDmg;
        float finalDamage;
        bool isCrit;

        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, other.gameObject);
        else { finalDamage = baseDamage; isCrit = false; }

        // Zero Z before normalizing to prevent 3D direction errors
        Vector3 rawDir = other.transform.position - transform.position;
        rawDir.z = 0f;
        Vector3 kbDir = rawDir.magnitude > 0.001f ? rawDir.normalized : Vector3.right;

        enemy.TakeDamage(
            Mathf.Max(1, Mathf.RoundToInt(finalDamage)),
            other.transform.position,
            kbDir,
            CurrentKnockback,
            isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal
        );

        playerStats?.RecordDashHitEnemy();
        playerStats?.RecordDamageDealt(finalDamage, DamageSource.Dash);
        upgradeManager?.DashHitEnemy(other.gameObject);
    }
}