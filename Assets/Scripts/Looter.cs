using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform; // usually parent
    private PlayerInventory playerInventory;
    private PlayerShooting player;

    // option: auto-detect orb components in trigger enter
    void Start()
    {
        player = GetComponentInParent<PlayerShooting>();

        if (playerTransform == null && transform.parent != null)
            playerTransform = transform.parent;

        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Orb>(out Orb orb))
        {
            // tell the orb to attract to the player
            orb.StartAttract(playerTransform, playerInventory);
        }

        ArrowPickup pickup = other.GetComponent<ArrowPickup>();

        if (pickup != null && pickup.canPickUp && !pickup.isBeingSucked)
        {
            // NEW: do not suck if player is full
            if (player != null && player.HasMaxArrows())
                return;

            pickup.StartSuck(player.transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        ArrowPickup pickup = other.GetComponent<ArrowPickup>();

        if (pickup != null && pickup.canPickUp && !pickup.isBeingSucked)
        {
            // If player had max arrows before but no longer has max, start sucking now
            if (!player.HasMaxArrows())
            {
                pickup.StartSuck(player.transform);
            }
        }
    }

    // no need for OnTriggerExit for this implementation
}
