using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverAbility : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverForce = 5f;
    public float maxUpwardSpeed = 5f;
    public float minEnergyToHover = 5f;

    [Tooltip("Default — overridden by PlayerStats.hoverDrainRate at runtime")]
    public float energyDrainPerSecond = 10f;

    private Rigidbody rb;
    private PlayerEnergy energySystem;
    private PlayerUpgradeManager upgradeManager;
    private PlayerMovement movement;
    private PlayerStats playerStats;

    private bool isHovering = false;
    private bool wasOutOfEnergy = false;
    private bool wasHovering = false;  // tracks start/end for history

    private float DrainRate => playerStats != null ? playerStats.hoverDrainRate : energyDrainPerSecond;

    private PlayerStateController stateCtrl;

    void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        movement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody>();
        energySystem = GetComponent<PlayerEnergy>();
        stateCtrl = GetComponent<PlayerStateController>();
    }

    void Update()
    {
        if (stateCtrl != null && !stateCtrl.HasControl())
        {
            if (isHovering) EndHover();
            return;
        }

        bool hasEnoughEnergy = energySystem.currentEnergy >= minEnergyToHover;
        if (hasEnoughEnergy) wasOutOfEnergy = false;

        bool wantsHover = GameInput.HoverHeld && hasEnoughEnergy
                          && !wasOutOfEnergy && !movement.isGrounded
                          && !movement.justWallJumped   // don't cap wall jump velocity
                          && !movement.justJumped;      // don't cap grounded jump velocity

        if (wantsHover && !isHovering)
        {
            isHovering = true;
            if (playerStats != null) playerStats.RecordHoverStart();
            if (upgradeManager != null) upgradeManager.HoverStart();
        }
        else if (!wantsHover && isHovering)
        {
            EndHover();
        }
    }

    void FixedUpdate()
    {
        if (!isHovering) return;

        rb.AddForce(Vector3.up * hoverForce, ForceMode.Acceleration);

        if (upgradeManager != null) upgradeManager.HoverTick(Time.fixedDeltaTime);
        if (playerStats != null) playerStats.RecordHoverTick(Time.fixedDeltaTime);

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y > maxUpwardSpeed)
            velocity.y = maxUpwardSpeed;
        rb.linearVelocity = velocity;

        float drain = DrainRate * Time.fixedDeltaTime;
        energySystem.DrainEnergy(drain);
        if (playerStats != null) playerStats.RecordEnergySpent(drain, EnergySpentSource.Hover);

        if (energySystem.currentEnergy < minEnergyToHover)
        {
            wasOutOfEnergy = true;
            EndHover();
        }
    }

    private void EndHover()
    {
        isHovering = false;
        if (upgradeManager != null) upgradeManager.HoverEnd();
    }

    private void OnDisable()
    {
        if (isHovering) EndHover();
    }
}