using UnityEngine;

public class ArrowPickup : MonoBehaviour
{
    public bool canPickUp = false;
    public float pickupDelay = 0.2f;

    public bool isBeingSucked = false;
    private Transform target; // player
    private float suckSpeed = 12f;

    private Arrow arrow;

    private void Awake()
    {
        arrow = GetComponent<Arrow>();
    }

    private void Start()
    {
        // Pre-placed arrow = instantly collectible
        if (arrow == null || arrow.speed == 0)
        {
            canPickUp = true;
            return;
        }

        // Shot arrow gets collectible after short delay
        StartCoroutine(EnablePickupDelayed());
    }

    private System.Collections.IEnumerator EnablePickupDelayed()
    {
        yield return new WaitForSeconds(pickupDelay);
        canPickUp = true;
    }

    public void StartSuck(Transform targetPlayer)
    {
        target = targetPlayer;
        isBeingSucked = true;

        // Disable collision so arrow doesn’t hit walls again
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // stop arrow movement logic
        if (arrow != null)
            arrow.enabled = false;
    }

    private void Update()
    {
        if (!isBeingSucked || target == null) return;

        // Smooth ease-in sucking motion
        transform.position = Vector3.Lerp(transform.position, target.position, suckSpeed * Time.deltaTime);

        // When close enough → pickup complete
        if (Vector3.Distance(transform.position, target.position) < 0.4f)
        {
            target.GetComponent<PlayerShooting>().RestoreArrow();
            Destroy(gameObject);
        }
    }
}
