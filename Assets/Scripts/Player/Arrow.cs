using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float damageVelocityThreshold = 0.1f;

    [Header("Spawn Safety")]
    public float armDelay = 0.05f;

    [Header("Collision Layers")]
    public LayerMask stickableLayers;

    private Vector3 direction;
    private bool hasLanded = false;
    private float currentVelocity;
    private float lifeTime;

    // Reference to the upgrade manager so we can fire events
    private PlayerUpgradeManager upgradeManager;

    public void Initialize(Vector3 shootDirection, LayerMask stickLayers)
    {
        direction = shootDirection.normalized;
        stickableLayers = stickLayers;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        // Find the upgrade manager on the player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            upgradeManager = player.GetComponent<PlayerUpgradeManager>();
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;

        if (hasLanded)
        {
            currentVelocity = 0f;
            return;
        }

        Vector3 move = direction * speed * Time.deltaTime;
        currentVelocity = move.magnitude / Time.deltaTime;

        Vector3 nextPosition = transform.position + move;
        nextPosition.z = 0f;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, move.magnitude, stickableLayers))
        {
            StickToSurface(hit.point);
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    private void StickToSurface(Vector3 point)
    {
        hasLanded = true;
        direction = Vector3.zero;
        currentVelocity = 0f;
        transform.position = new Vector3(point.x, point.y, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only damage if arrow is still moving
        if (currentVelocity < damageVelocityThreshold)
            return;

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);

            // Fire the upgrade event so upgrades can react to arrow hits
            upgradeManager?.ArrowHitEnemy(other.gameObject);
        }
    }
}