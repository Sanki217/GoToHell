using UnityEngine;

/// <summary>
/// Child of the Player. Handles pickup of souls, arrows, and upgrade orbs.
/// </summary>
public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform;

    private PlayerInventory playerInventory;
    private PlayerShooting player;
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private PlayerUpgradeManager upgradeManager;
    private SphereCollider sphereCollider;

    private int arrowsInFlight = 0;

    // Reusable hit buffer — avoids per-frame allocation in the scan loop.
    private static readonly Collider[] hitBuffer = new Collider[64];

    void Start()
    {
        player = GetComponentInParent<PlayerShooting>();
        playerStats = GetComponentInParent<PlayerStats>();
        playerHealth = GetComponentInParent<PlayerHealth>();
        upgradeManager = GetComponentInParent<PlayerUpgradeManager>();

        if (playerTransform == null && transform.parent != null)
            playerTransform = transform.parent;

        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();

        sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()
    {
        float range = playerStats != null ? playerStats.lootRange : 4f;
        if (sphereCollider != null && !Mathf.Approximately(sphereCollider.radius, range))
            sphereCollider.radius = range;

        if (player == null) return;

        int effectiveCapacity = player.maxArrows - player.CurrentArrows - arrowsInFlight;
        if (effectiveCapacity <= 0) return;

        int count = Physics.OverlapSphereNonAlloc(playerTransform.position, range, hitBuffer);

        for (int i = 0; i < count; i++)
        {
            if (effectiveCapacity <= 0) break;

            ArrowPickup pickup = hitBuffer[i].GetComponent<ArrowPickup>();
            if (pickup == null || !pickup.canPickUp || pickup.isBeingSucked) continue;

            arrowsInFlight++;
            effectiveCapacity--;
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

        // Health orb — attracts in, heals on arrival
        if (other.TryGetComponent<HealthOrb>(out HealthOrb healthOrb))
            healthOrb.StartAttract(playerTransform, playerHealth);

        // Upgrade orb � attract it in, applies itself on arrival
        if (other.TryGetComponent<UpgradeOrb>(out UpgradeOrb upgradeOrb))
            upgradeOrb.StartAttract(playerTransform, upgradeManager, playerStats);
    }
}