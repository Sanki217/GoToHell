using UnityEngine;

public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform;
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

    private void OnTriggerEnter(Collider other)
    {
        // Soul pickup
        if (other.TryGetComponent<Soul>(out Soul soul))
        {
            soul.StartAttract(playerTransform, playerInventory);
        }

        // Arrow pickup
        ArrowPickup pickup = other.GetComponent<ArrowPickup>();
        if (pickup != null && pickup.canPickUp && !pickup.isBeingSucked)
        {
            if (player != null && player.HasMaxArrows())
                return;

            pickup.StartSuck(player.transform);
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

    private void OnTriggerStay(Collider other)
    {
        ArrowPickup pickup = other.GetComponent<ArrowPickup>();

        if (pickup != null && pickup.canPickUp && !pickup.isBeingSucked)
        {
            if (!player.HasMaxArrows())
            {
                pickup.StartSuck(player.transform);
            }
        }
    }
}