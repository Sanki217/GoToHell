using UnityEngine;

public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashCost = 20f;
    public float dashForce = 600f;
    public float dashDuration = 0.3f;
    public float decelerationFactor = 2f; // Higher = faster deceleration
    public int dashDamage = 1;
    public float maxDashRange = 10f;

    [Header("References")]
    public Camera mainCamera;

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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && playerEnergy.SpendEnergy(dashCost))
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            worldMousePos.z = transform.position.z;

            dashDirection = (worldMousePos - transform.position);
            dashDirection.z = 0f;

            float dashDistance = Mathf.Min(dashDirection.magnitude, maxDashRange);
            dashDirection.Normalize();

            rb.linearVelocity = Vector3.zero; // Reset for consistency
            initialVelocity = dashDirection * (dashDistance / dashDuration); // Adjust speed based on range and duration
            rb.linearVelocity = initialVelocity;

            isDashing = true;
            dashTimer = dashDuration;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            // Smoothly reduce velocity over time
            float decelerationMultiplier = Mathf.Clamp01(dashTimer / dashDuration); // 1 ? 0 over duration
            rb.linearVelocity = initialVelocity * decelerationMultiplier;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = Vector3.zero; // Fully stop at end
            }
        }
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
