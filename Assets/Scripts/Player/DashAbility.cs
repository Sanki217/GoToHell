using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Default value — overridden at runtime by PlayerStats.dashCost")]
    public float dashCost = 20f;
    [Tooltip("Default value — overridden at runtime by PlayerStats.dashDistance")]
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

    // ── runtime values read from PlayerStats each frame ──
    private float CurrentDashCost => playerStats != null ? playerStats.dashCost : dashCost;
    private float CurrentDashRange => playerStats != null ? playerStats.dashDistance : maxDashRange;
    private float CurrentDashDmg => playerStats != null ? playerStats.dashDamage : dashDamage;

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
                if (Vector3.Distance(origin, dashTarget) < 0.5f)
                    return;
            }
            else
            {
                dashTarget = origin + direction * intendedDistance;
            }

            if (!playerEnergy.SpendEnergy(CurrentDashCost))
                return;

            dashDirection = (dashTarget - origin).normalized;
            dashVelocity = dashDirection * (Vector3.Distance(origin, dashTarget) / dashDuration);
            dashTimer = dashDuration;
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
        float distanceToCursor = Vector3.Distance(origin, cursorWorld);
        float intendedDistance = Mathf.Min(distanceToCursor, CurrentDashRange);

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, intendedDistance, dashCollisionLayers))
            endPoint = hit.point;
        else
            endPoint = origin + direction * intendedDistance;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    Vector3 GetCursorWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, shootOrigin.position);
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return shootOrigin.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDashing || !other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        // Read knockback from PlayerStats
        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;
        Vector3 kbDir = (other.transform.position - transform.position).normalized;

        enemy.TakeDamage(
            Mathf.RoundToInt(CurrentDashDmg),
            other.transform.position,
            kbDir,
            kbForce,
            false,
            FloatingTextManager.HitType.Normal
        );

        upgradeManager?.DashHitEnemy(other.gameObject);
    }
}