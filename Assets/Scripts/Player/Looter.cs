using UnityEngine;

/// <summary>
/// Child of the Player. Handles pickup of souls, arrows, and upgrade orbs.
/// 
/// The sphere collider radius on this GameObject IS the pickup range for everything.
/// It is driven by PlayerStats.lootRange so upgrades can increase it.
/// 
/// For arrows embedded in walls (outside the sphere), a Physics.OverlapSphere
/// scan runs every frame using the same radius.
/// </summary>
public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform;

    private PlayerInventory playerInventory;
    private PlayerShooting player;
    private PlayerStats playerStats;
    private SphereCollider sphereCollider;

    void Start()
    {
        player = GetComponentInParent<PlayerShooting>();
        playerStats = GetComponentInParent<PlayerStats>();

        if (playerTransform == null && transform.parent != null)
            playerTransform = transform.parent;

        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();

        sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()
    {
        // Keep sphere collider radius in sync with PlayerStats.lootRange
        float range = playerStats != null ? playerStats.lootRange : 4f;
        if (sphereCollider != null && !Mathf.Approximately(sphereCollider.radius, range))
            sphereCollider.radius = range;

        // Scan for arrows within pickup range — catches arrows inside walls
        // that the sphere trigger can't physically overlap
        if (player != null && !player.HasMaxArrows())
        {
            Collider[] nearby = Physics.OverlapSphere(
                playerTransform.position,
                range
            );

            foreach (Collider col in nearby)
            {
                ArrowPickup pickup = col.GetComponent<ArrowPickup>();
                if (pickup == null || !pickup.canPickUp || pickup.isBeingSucked) continue;

                pickup.StartSuck(player.transform);
                break; // only start one suck per frame
            }
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