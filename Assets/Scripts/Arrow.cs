using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    private Vector3 direction;
    private bool hasLanded = false;

    [Header("Collision Layers")]
    public LayerMask stickableLayers; // Platforms + Walls layer mask

    public void Initialize(Vector3 shootDirection, LayerMask stickLayers)
    {
        direction = shootDirection.normalized;
        stickableLayers = stickLayers;

        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction); // Align visual rotation (2D)
    }

    private void Update()
    {
        if (hasLanded) return;

        Vector3 move = direction * speed * Time.deltaTime;
        Vector3 nextPosition = transform.position + move;
        nextPosition.z = 0f;

        // Raycast ahead to detect collision with walls/platforms
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
        transform.position = new Vector3(point.x, point.y, 0f);
        transform.SetParent(null);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
            }
        }
        else if (hasLanded && other.CompareTag("Player"))
        {
            other.GetComponent<PlayerShooting>().RestoreArrow();
            Destroy(gameObject);
        }
    }
}
