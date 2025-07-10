using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverAbility : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverForce = 5f;
    public float energyDrainPerSecond = 10f;
    public float maxUpwardSpeed = 5f;
    public float minEnergyToHover = 5f;

    private Rigidbody rb;
    private PlayerEnergy energySystem;
    private bool isHovering = false;
    private bool wasOutOfEnergy = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        energySystem = GetComponent<PlayerEnergy>();
    }

    void Update()
    {
        bool hasEnoughEnergy = energySystem.currentEnergy >= minEnergyToHover;

        // If player was out of energy and now above threshold, allow hover again
        if (hasEnoughEnergy)
            wasOutOfEnergy = false;

        // Start hover if holding space and energy is above threshold
        if (Input.GetKey(KeyCode.Space) && hasEnoughEnergy && !wasOutOfEnergy)
        {
            isHovering = true;
        }
        else
        {
            isHovering = false;
        }
    }

    void FixedUpdate()
    {
        if (isHovering)
        {
            // Apply force
            rb.AddForce(Vector3.up * hoverForce, ForceMode.Acceleration);

            // Clamp vertical velocity
            Vector3 velocity = rb.linearVelocity;
            if (velocity.y > maxUpwardSpeed)
                velocity.y = maxUpwardSpeed;
            rb.linearVelocity = velocity;

            // Drain energy
            energySystem.DrainEnergy(energyDrainPerSecond * Time.fixedDeltaTime);

            // If energy ran out, block further hovering until restored
            if (energySystem.currentEnergy < minEnergyToHover)
                wasOutOfEnergy = true;
        }
    }
}
