using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    public bool hasControl = false;
    private PlayerMovement movement;
    private PlayerShooting shooting;
    private HoverAbility hover;

    private Rigidbody rb;
    public bool allowKickMovement = false;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        shooting = GetComponent<PlayerShooting>();
        hover = GetComponent<HoverAbility>();
        rb = GetComponent<Rigidbody>();
    }

    public bool AllowKickMovement()
    {
        return allowKickMovement;
    }

    public void EnableControl()
    {
        hasControl = true;
        allowKickMovement = false; // stop allowing external kick forces
    }

    public void DisableControl()
    {
        hasControl = false;
        rb.linearVelocity = Vector3.zero;
    }

    public bool HasControl() => hasControl;
}
