using UnityEngine;

public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashCost = 20f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;

    [Header("References")]
    public Camera mainCamera;

    private Rigidbody rb;
    private PlayerEnergy playerEnergy;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;

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

            dashDirection = (worldMousePos - transform.position);
            dashDirection.z = 0f;
            dashDirection.Normalize();

            isDashing = true;
            dashTimer = dashDuration;
        }

        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;

            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }
    }
}
