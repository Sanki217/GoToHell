using UnityEngine;

/// <summary>
/// Child of the Player. Handles pickup of souls, orbs, and arrows.
///
/// Detection uses a physics overlap scan, NOT trigger events: the pickup
/// layer ignores itself in the collision matrix (PhysicsLayerSetup), which
/// also suppresses trigger callbacks between objects on that layer — and this
/// Looter lives on it. Overlap queries don't consult the matrix, so the scan
/// always works. OnTriggerEnter is kept as a harmless backup (all StartAttract
/// methods are guarded/idempotent).
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
    private static readonly Collider[] hitBuffer = new Collider[128];

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
        if (playerTransform == null) return;

        float range = playerStats != null ? playerStats.lootRange : 4f;
        if (sphereCollider != null && !Mathf.Approximately(sphereCollider.radius, range))
            sphereCollider.radius = range;

        int count = Physics.OverlapSphereNonAlloc(playerTransform.position, range, hitBuffer);

        int arrowCapacity = player != null
            ? player.maxArrows - player.CurrentArrows - arrowsInFlight
            : 0;

        for (int i = 0; i < count; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null) continue;

            // Souls / orbs — attract (each StartAttract self-guards, so
            // hitting the same object on consecutive frames is a no-op).
            if (col.TryGetComponent<Soul>(out Soul soul))
            {
                soul.StartAttract(playerTransform, playerInventory);
                continue;
            }
            if (col.TryGetComponent<HealthOrb>(out HealthOrb healthOrb))
            {
                healthOrb.StartAttract(playerTransform, playerHealth);
                continue;
            }
            if (col.TryGetComponent<UpgradeOrb>(out UpgradeOrb upgradeOrb))
            {
                upgradeOrb.StartAttract(playerTransform, upgradeManager, playerStats);
                continue;
            }

            // Stuck arrows — only up to free quiver capacity
            if (arrowCapacity > 0)
            {
                ArrowPickup pickup = col.GetComponent<ArrowPickup>();
                if (pickup == null || !pickup.canPickUp || pickup.isBeingSucked) continue;

                arrowsInFlight++;
                arrowCapacity--;
                pickup.StartSuck(player.transform, OnArrowArrived);
            }
        }
    }

    private void OnArrowArrived()
    {
        arrowsInFlight = Mathf.Max(0, arrowsInFlight - 1);
    }

    // Backup path — harmless duplicate of the scan (guarded StartAttracts).
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Soul>(out Soul soul))
            soul.StartAttract(playerTransform, playerInventory);

        if (other.TryGetComponent<HealthOrb>(out HealthOrb healthOrb))
            healthOrb.StartAttract(playerTransform, playerHealth);

        if (other.TryGetComponent<UpgradeOrb>(out UpgradeOrb upgradeOrb))
            upgradeOrb.StartAttract(playerTransform, upgradeManager, playerStats);
    }
}
