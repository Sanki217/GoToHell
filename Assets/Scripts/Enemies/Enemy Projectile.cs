using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy projectile. POOLED — spawned via Pool.Spawn by EnemyShooter and
/// returned with Pool.Despawn on hit or lifetime expiry (never Destroy).
///
/// Owner-collision ignores are tracked and undone on despawn, so a reused
/// projectile never inherits a previous owner's ignore pairs.
/// </summary>
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
    private float lifeTimer;

    private Collider myCollider;
    private readonly List<Collider> ignoredColliders = new List<Collider>();

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    public void Initialize(Vector3 dir, float moveSpeed, Transform owner)
    {
        direction = dir.normalized;
        speed = moveSpeed;
        ownerRoot = owner;
        lifeTimer = 0f;

        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        // Ignore ALL colliders on the shooting enemy (root + children),
        // remembering each pair so it can be undone on despawn.
        if (myCollider != null && ownerRoot != null)
        {
            foreach (Collider c in ownerRoot.GetComponentsInChildren<Collider>())
            {
                if (c == null) continue;
                Physics.IgnoreCollision(myCollider, c);
                ignoredColliders.Add(c);
            }
        }
    }

    private void OnDisable()
    {
        // Undo owner ignores so pooled reuse starts clean
        foreach (Collider c in ignoredColliders)
            if (c != null && myCollider != null)
                Physics.IgnoreCollision(myCollider, c, false);
        ignoredColliders.Clear();
        ownerRoot = null;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
            Pool.Despawn(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore owner
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Ignore pickups / sensors
        if (other.CompareTag("EnemySensor") || other.CompareTag("Orb") || other.CompareTag("Looter"))
            return;

        // Only react to allowed layers
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // ── PLAYER ──────────────────────────────────────────────────
        if (other.CompareTag("Player"))
        {
            // Directional damage — the Warrior's shield can block it (projectile
            // is still despawned on a blocked hit: the shield absorbs it).
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage, transform.position);
            Pool.Despawn(gameObject);
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

            Pool.Despawn(gameObject);
            return;
        }

        // ── WORLD ───────────────────────────────────────────────────
        Pool.Despawn(gameObject);
    }
}
