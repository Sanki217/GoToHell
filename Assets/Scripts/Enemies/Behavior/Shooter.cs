using UnityEngine;

public class Shooter : EnemyBehavior
{
    [Header("References")]
    public RangeSensor sensor;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    public float projectileSpeed = 8f;
    public float muzzleOffset = 0.6f;

    private float timer;
    private Collider enemyCollider;

    protected override void Awake()
    {
        base.Awake();
        enemyCollider = GetComponent<Collider>();
    }

    public override void TickBehavior()
    {
        if (!player || !sensor || !sensor.playerInRange)
            return;

        timer += Time.deltaTime;
        if (timer < attackCooldown)
            return;

        timer = 0f;

        Vector3 dir = (player.position - transform.position);
        dir.z = 0f;
        dir.Normalize();

        Vector3 spawnPos = transform.position + dir * muzzleOffset;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        EnemyProjectile projectile = proj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(dir, projectileSpeed, transform.root);
        }
    }
}
