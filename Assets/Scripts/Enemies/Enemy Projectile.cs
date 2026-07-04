using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Combat")]
    public int damage = 1;
    public float lifeTime = 5f;

    [Header("Collision")]
    public LayerMask hitLayers;   // World + Player + Enemy

    private Vector3 direction;
    private float speed;
    private Transform ownerRoot;

    public void Initialize(Vector3 dir, float moveSpeed, Transform owner)
    {
        direction = dir.normalized;
        speed = moveSpeed;
        ownerRoot = owner;

        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        Collider myCollider = GetComponent<Collider>();

        // Ignore ALL colliders on the shooting enemy (root + children)
        if (myCollider && ownerRoot)
        {
            foreach (Collider c in ownerRoot.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(myCollider, c);
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore owner
        if (other.transform.root == ownerRoot) return;

        // Ignore pickups / sensors
        if (other.CompareTag("EnemySensor") || other.CompareTag("Orb") || other.CompareTag("Looter"))
            return;

        // Only react to allowed layers
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // ── PLAYER ──────────────────────────────────────────────────
        if (other.CompareTag("Player"))
        {
            // Directional damage — the Warrior's shield can block it (projectile
            // is still destroyed on a blocked hit: the shield absorbs it).
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage, transform.position);
            Destroy(gameObject);
            return;
        }

        // ── OTHER ENEMY ─────────────────────────────────────────────
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && enemy.transform.root != ownerRoot)
            {
                // Show damage number at the hit position
                FloatingTextManager.Show(damage, other.transform.position,
                    FloatingTextManager.HitType.Normal);

                enemy.TakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        // ── WORLD ───────────────────────────────────────────────────
        Destroy(gameObject);
    }
}
