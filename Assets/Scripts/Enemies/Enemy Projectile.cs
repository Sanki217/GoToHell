using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy projectile. POOLED — spawned via Pool.Spawn by EnemyShooter /
/// EnemyStalker and returned with Pool.Despawn on hit or lifetime expiry
/// (never Destroy).
///
/// OWNER: the shooting enemy's transform (not transform.root — enemies placed
/// under a scene folder would otherwise share a root and ignore each other).
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
    private Transform owner;
    private float lifeTimer;
    private bool spent;           // hit something this frame — ignore further triggers
    private int hitDamage;        // damage vs the player, scaled by the shooter (EnemyDefinition / layer / pacts)

    private Collider myCollider;
    private readonly List<Collider> ignoredColliders = new List<Collider>();

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        spent = false;
        lifeTimer = 0f;
    }

    public void Initialize(Vector3 dir, float moveSpeed, Transform shooter)
    {
        direction = dir.normalized;
        speed = moveSpeed;
        lifeTimer = 0f;
        spent = false;

        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        SetOwner(shooter);

        Enemy shooterEnemy = shooter != null ? shooter.GetComponentInParent<Enemy>() : null;
        hitDamage = shooterEnemy != null ? shooterEnemy.ScaleProjectileDamage(damage) : damage;
    }

    private void OnDisable()
    {
        ClearOwnerIgnores();
        owner = null;
    }

    private void SetOwner(Transform newOwner)
    {
        ClearOwnerIgnores();
        owner = newOwner;
        if (myCollider == null || owner == null) return;

        // Ignore ALL colliders on the owner (root + children), remembering each
        // pair so it can be undone on despawn.
        foreach (Collider c in owner.GetComponentsInChildren<Collider>())
        {
            if (c == null) continue;
            Physics.IgnoreCollision(myCollider, c);
            ignoredColliders.Add(c);
        }
    }

    private void ClearOwnerIgnores()
    {
        foreach (Collider c in ignoredColliders)
            if (c != null && myCollider != null)
                Physics.IgnoreCollision(myCollider, c, false);
        ignoredColliders.Clear();
    }

    /// <summary>
    /// Shield reflect: fly back toward the original shooter (or straight back
    /// if it's gone). The player becomes the new owner, so the projectile now
    /// damages enemies — including the shooter — and ignores the player.
    /// </summary>
    private void Reflect(Transform newOwner)
    {
        Transform shooter = owner;
        SetOwner(newOwner);

        Vector3 dir = shooter != null ? shooter.position - transform.position : -direction;
        dir.z = 0f;
        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : -direction;

        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        lifeTimer = 0f;   // fresh flight time for the return trip
    }

    private void Update()
    {
        if (spent) return;

        transform.position += direction * speed * Time.deltaTime;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
            Despawn();
    }

    private void Despawn()
    {
        if (spent) return;
        spent = true;
        Pool.Despawn(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spent) return;

        // Ignore owner
        if (owner != null && other.transform.IsChildOf(owner)) return;

        // Ignore pickups / sensors
        if (other.CompareTag("EnemySensor") || other.CompareTag("Orb") || other.CompareTag("Looter")
            || other.CompareTag("SoulLooter"))
            return;

        // Only react to allowed layers
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // ── PLAYER ──────────────────────────────────────────────────
        if (other.CompareTag("Player"))
        {
            // Warrior's shield: reflect back at the shooter (costs energy per
            // projectile). Can't afford it → the shield absorbs the hit.
            ShieldAbility shield = other.GetComponentInParent<ShieldAbility>();
            if (shield != null && shield.isActiveAndEnabled && shield.IsBlockingFrom(transform.position))
            {
                if (shield.TryPayReflectCost())
                    Reflect(shield.transform);
                else
                    Despawn();
                return;
            }

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(hitDamage > 0 ? hitDamage : damage, transform.position);
            Despawn();
            return;
        }

        // ── ENEMY ───────────────────────────────────────────────────
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && (owner == null || !enemy.transform.IsChildOf(owner)))
                enemy.TakeDamage(damage, transform.position, Vector3.zero, 0f,
                                 false, FloatingTextManager.HitType.Normal);

            Despawn();
            return;
        }

        // ── WORLD ───────────────────────────────────────────────────
        Despawn();
    }
}
