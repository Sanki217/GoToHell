using UnityEngine;

public class ArrowPickup : MonoBehaviour
{
    // canPickUp is only set to true once the arrow has landed on a wall
    public bool canPickUp = false;

    public bool isBeingSucked = false;
    private System.Action onArrivedCallback;
    private Transform target;

    [Header("Suck Settings")]
    public float minSuckSpeed = 8f;
    public float maxSuckSpeed = 25f;
    public float maxSuckDistance = 15f;   // distance at which speed is maxSuckSpeed

    private Arrow arrow;

    private void Awake()
    {
        arrow = GetComponent<Arrow>();
    }

    private void Start()
    {
        // Pre-placed arrow (speed = 0) → instantly collectible
        if (arrow == null || arrow.speed == 0)
        {
            canPickUp = true;
        }
        // Shot arrows: canPickUp is set to true by Arrow.cs when it hits a wall
        // (see Arrow.StickToSurface → ArrowPickup.OnArrowLanded)
    }

    /// <summary>
    /// Called by Arrow when it sticks to a surface.
    /// Only at this point does the arrow become collectible.
    /// </summary>
    public void OnArrowLanded()
    {
        canPickUp = true;
    }

    public void StartSuck(Transform targetPlayer, System.Action onArrived = null)
    {
        target = targetPlayer;
        isBeingSucked = true;

        onArrivedCallback = onArrived;
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (arrow != null)
            arrow.enabled = false;
    }

    private void Update()
    {
        if (!isBeingSucked || target == null) return;

        // Speed scales with distance — faster when far, but never slower than min
        float dist = Vector3.Distance(transform.position, target.position);
        float t = Mathf.Clamp01(dist / maxSuckDistance);
        float suckSpeed = Mathf.Lerp(minSuckSpeed, maxSuckSpeed, t);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            suckSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.4f)
        {
            PlayerShooting shooting = target.GetComponent<PlayerShooting>();
            PlayerStats stats = target.GetComponent<PlayerStats>();
            PlayerUpgradeManager mgr = target.GetComponent<PlayerUpgradeManager>();

            shooting?.RestoreArrow();
            stats?.RecordArrowPickedUp();
            mgr?.ArrowPickedUp();

            onArrivedCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}