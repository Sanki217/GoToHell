using UnityEngine;

/// <summary>
/// Child of the Player. Handles pickup of souls, arrows, and upgrade orbs.
///
/// Arrow pickup fix: tracks how many arrows are currently being sucked toward the player.
/// This count is added to CurrentArrows to determine effective capacity,
/// preventing more arrows from starting their suck than the player can hold.
/// </summary>
public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform;

    private PlayerInventory playerInventory;
    private PlayerShooting player;
    private PlayerStats playerStats;
    private SphereCollider sphereCollider;

    // Count of arrows currently flying toward the player (sucking)
    private int arrowsInFlight = 0;

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

        // Scan for arrows — catches wall-embedded ones the trigger can't reach
        if (player == null) return;

        // Effective capacity = how many more arrows we can still hold
        // accounting for arrows already flying toward us
        int effectiveCapacity = player.maxArrows - player.CurrentArrows - arrowsInFlight;
        if (effectiveCapacity <= 0) return;

        Collider[] nearby = Physics.OverlapSphere(playerTransform.position, range);

        foreach (Collider col in nearby)
        {
            if (effectiveCapacity <= 0) break;

            ArrowPickup pickup = col.GetComponent<ArrowPickup>();
            if (pickup == null || !pickup.canPickUp || pickup.isBeingSucked) continue;

            arrowsInFlight++;
            effectiveCapacity--;

            // When this arrow arrives it calls RestoreArrow — we decrement arrowsInFlight then
            pickup.StartSuck(player.transform, OnArrowArrived);
        }
    }

    private void OnArrowArrived()
    {
        arrowsInFlight = Mathf.Max(0, arrowsInFlight - 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Soul pickup
        if (other.TryGetComponent<Soul>(out Soul soul))
            soul.StartAttract(playerTransform, playerInventory);

        // Upgrade orb pickup
        UpgradeOrb upgradeOrb = other.GetComponent<UpgradeOrb>();
        if (upgradeOrb != null)
        {
            var mgr = playerTransform?.GetComponent<PlayerUpgradeManager>();
            if (mgr != null) upgradeOrb.Apply(mgr);
        }
    }
}