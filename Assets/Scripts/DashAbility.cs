using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashCost = 20f;
    public float dashDuration = 0.3f;
    public float maxDashRange = 10f;
    public float postDashMomentum = 10f;
    public int dashDamage = 1;
    public LayerMask dashCollisionLayers;

    [Header("References")]
    public Camera mainCamera;
    public LineRenderer lineRenderer;
    public Transform shootOrigin;

    private Rigidbody rb;
    private PlayerEnergy playerEnergy;
    public bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;
    private Vector3 dashVelocity;

    void Start()
    {
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
        if (isDashing)
        {
            rb.linearVelocity = dashVelocity;
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = dashDirection * postDashMomentum;
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing)
        {
            Vector3 cursorWorld = GetCursorWorldPosition();
            Vector3 origin = shootOrigin.position;
            Vector3 direction = (cursorWorld - origin).normalized;
            float distanceToCursor = Vector3.Distance(origin, cursorWorld);

            // Clamp to max dash range
            float intendedDistance = Mathf.Min(distanceToCursor, maxDashRange);

            // Raycast to avoid going through walls
            Ray ray = new Ray(origin, direction);
            Vector3 dashTarget;

            if (Physics.Raycast(ray, out RaycastHit hit, intendedDistance, dashCollisionLayers))
            {
                dashTarget = hit.point;

                // Too close to wall � skip dash and energy drain
                if (Vector3.Distance(origin, dashTarget) < 0.5f)
                    return;
            }
            else
            {
                dashTarget = origin + direction * intendedDistance;
            }

            // Spend energy
            if (!playerEnergy.SpendEnergy(dashCost))
                return;

            // Start dash
            dashDirection = (dashTarget - origin).normalized;
            dashVelocity = dashDirection * (Vector3.Distance(origin, dashTarget) / dashDuration);
            dashTimer = dashDuration;
            isDashing = true;
            Camera.main.GetComponent<CameraFollow>()?.Shake(0.1f, 0.1f); // big shake

        }
    }

    void UpdateDashLine()
    {
        if (lineRenderer == null || shootOrigin == null) return;

        Vector3 cursorWorld = GetCursorWorldPosition();
        Vector3 origin = shootOrigin.position;
        Vector3 direction = (cursorWorld - origin).normalized;
        float distanceToCursor = Vector3.Distance(origin, cursorWorld);
        float intendedDistance = Mathf.Min(distanceToCursor, maxDashRange);

        Ray ray = new Ray(origin, direction);
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, intendedDistance, dashCollisionLayers))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = origin + direction * intendedDistance;
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    Vector3 GetCursorWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, shootOrigin.position);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return shootOrigin.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDashing && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(dashDamage);
        }
    }
}
