using UnityEngine;

public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashCost = 20f;
    public float dashForce = 600f;
    public float dashDuration = 0.3f;
    public int dashDamage = 1;
    public float maxDashRange = 10f;

    [Header("References")]
    public Camera mainCamera;

    private Rigidbody rb;
    private PlayerEnergy playerEnergy;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;

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

            rb.linearVelocity = Vector3.zero; // Reset velocity for consistency
            rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);

            isDashing = true;
            dashTimer = dashDuration;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = Vector3.zero; // Stop abruptly or replace with damping if desired
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
