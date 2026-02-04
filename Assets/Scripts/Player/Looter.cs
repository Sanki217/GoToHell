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
        if (other.TryGetComponent<Orb>(out Orb orb))
        {
            orb.StartAttract(playerTransform, playerInventory);
        }

        ArrowPickup pickup = other.GetComponent<ArrowPickup>();

        if (pickup != null && pickup.canPickUp && !pickup.isBeingSucked)
        {
            //do not suck if player is full
            if (player != null && player.HasMaxArrows())
                return;

            pickup.StartSuck(player.transform);
        }

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
            // If player had max arrows before but no longer has max, start sucking now
            if (!player.HasMaxArrows())
            {
                pickup.StartSuck(player.transform);
            }
        }
    }

    // no need for OnTriggerExit for this implementation
}
