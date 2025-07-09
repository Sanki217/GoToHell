using UnityEngine;

public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashCost = 20f;
    public float dashDuration = 0.3f;
    public int dashDamage = 1;

    [Header("References")]
    public Camera mainCamera;
    public LineRenderer dashLine;

    private Rigidbody rb;
    private PlayerEnergy playerEnergy;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;
    private Vector3 initialVelocity;

    public bool IsDashing => isDashing;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerEnergy = GetComponent<PlayerEnergy>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (dashLine != null)
            dashLine.enabled = false;
    }

    void Update()
    {
        UpdateDashLine();

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && playerEnergy.SpendEnergy(dashCost))
        {
            Vector3 targetPos;
            if (TryGetDashTarget(out targetPos))
            {
                dashDirection = targetPos - transform.position;
                float dashDistance = dashDirection.magnitude;
                dashDirection.Normalize();

                rb.linearVelocity = Vector3.zero;
                initialVelocity = dashDirection * (dashDistance / dashDuration);
                rb.linearVelocity = initialVelocity;

                isDashing = true;
                dashTimer = dashDuration;
                if (dashLine != null) dashLine.enabled = false;
            }
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            float decelerationMultiplier = Mathf.Clamp01(dashTimer / dashDuration);
            rb.linearVelocity = initialVelocity * decelerationMultiplier;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }

    void UpdateDashLine()
    {
        if (isDashing || dashLine == null)
        {
            dashLine.enabled = false;
            return;
        }

        if (playerEnergy.currentEnergy < dashCost)
        {
            dashLine.enabled = false;
            return;
        }

        Vector3 targetPos;
        if (TryGetDashTarget(out targetPos))
        {
            dashLine.enabled = true;
            dashLine.positionCount = 2;
            dashLine.SetPosition(0, transform.position);
            dashLine.SetPosition(1, targetPos);
        }
        else
        {
            dashLine.enabled = false;
        }
    }

    bool TryGetDashTarget(out Vector3 targetPos)
    {
        targetPos = transform.position;

        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane dashPlane = new Plane(Vector3.forward, transform.position); // For 2.5D side view

        if (dashPlane.Raycast(mouseRay, out float enter))
        {
            Vector3 worldMousePos = mouseRay.GetPoint(enter);
            worldMousePos.z = transform.position.z;

            targetPos = worldMousePos;
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDashing && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(dashDamage);
            }
        }
    }
}
