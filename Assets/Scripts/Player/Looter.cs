using UnityEngine;

public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform;

    [Header("Arrow Scan")]
    [Tooltip("Radius within which the player scans for pickable arrows each frame")]
    public float arrowPickupRadius = 8f;

    private PlayerInventory playerInventory;
    private PlayerShooting player;

    void Start()
    {
        player = GetComponentInParent<PlayerShooting>();

        if (playerTransform == null && transform.parent != null)
            playerTransform = transform.parent;

        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
    }

    void Update()
    {
        // Scan for nearby pickable arrows every frame.
        // No layer mask — finds arrows by component check, works for any layer.
        // This catches arrows embedded in walls that the Looter trigger can't reach.
        if (player == null || player.HasMaxArrows()) return;

        Collider[] nearby = Physics.OverlapSphere(
            playerTransform.position,
            arrowPickupRadius
        );

        foreach (Collider col in nearby)
        {
            ArrowPickup pickup = col.GetComponent<ArrowPickup>();
            if (pickup == null || !pickup.canPickUp || pickup.isBeingSucked) continue;

            pickup.StartSuck(player.transform);
            break; // attract one arrow per frame — prevents double-restoring
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Soul pickup
        if (other.TryGetComponent<Soul>(out Soul soul))
        {
            soul.StartAttract(playerTransform, playerInventory);
        }

        // Upgrade orb pickup
        UpgradeOrb upgradeOrb = other.GetComponent<UpgradeOrb>();
        if (upgradeOrb != null)
        {
            var mgr = playerTransform.GetComponent<PlayerUpgradeManager>();
            if (mgr != null)
                upgradeOrb.Apply(mgr);
        }
    }
}