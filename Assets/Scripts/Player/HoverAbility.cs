using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverAbility : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverForce = 5f;
    public float maxUpwardSpeed = 5f;
    public float minEnergyToHover = 5f;

    [Tooltip("Default value — overridden at runtime by PlayerStats.hoverDrainRate")]
    public float energyDrainPerSecond = 10f;

    private Rigidbody rb;
    private PlayerEnergy energySystem;
    private PlayerUpgradeManager upgradeManager;
    private PlayerMovement movement;
    private PlayerStats playerStats;

    private bool isHovering = false;
    private bool wasOutOfEnergy = false;

    // reads live value from PlayerStats if available
    private float DrainRate => playerStats != null ? playerStats.hoverDrainRate : energyDrainPerSecond;

    void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        movement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody>();
        energySystem = GetComponent<PlayerEnergy>();
    }

    void Update()
    {
        if (!GetComponent<PlayerStateController>().HasControl())
        {
            isHovering = false;
            return;
        }

        bool hasEnoughEnergy = energySystem.currentEnergy >= minEnergyToHover;

        if (hasEnoughEnergy)
            wasOutOfEnergy = false;

        if (Input.GetKey(KeyCode.Space) && hasEnoughEnergy && !wasOutOfEnergy && !movement.isGrounded)
        {
            isHovering = true;
            upgradeManager?.HoverStart();
        }
        else
        {
            isHovering = false;
            upgradeManager?.HoverEnd();
        }
    }

    void FixedUpdate()
    {
        if (!isHovering) return;

        rb.AddForce(Vector3.up * hoverForce, ForceMode.Acceleration);

        upgradeManager?.HoverTick(Time.fixedDeltaTime);

        // Clamp upward velocity
        Vector3 velocity = rb.linearVelocity;
        if (velocity.y > maxUpwardSpeed)
            velocity.y = maxUpwardSpeed;
        rb.linearVelocity = velocity;

        // Drain energy using live stat value
        energySystem.DrainEnergy(DrainRate * Time.fixedDeltaTime);

        if (energySystem.currentEnergy < minEnergyToHover)
            wasOutOfEnergy = true;
    }
}