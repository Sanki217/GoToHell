using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float damageVelocityThreshold = 0.1f;

    private Vector3 direction;
    private bool hasLanded = false;
    private float currentVelocity;

    [Header("Collision Layers")]
    public LayerMask stickableLayers;

    public void Initialize(Vector3 shootDirection, LayerMask stickLayers)
    {
        direction = shootDirection.normalized;
        stickableLayers = stickLayers;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    private void Update()
    {
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
        // Damage only if arrow is still moving
        if (currentVelocity < damageVelocityThreshold)
            return;

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);
        }
    }
}
