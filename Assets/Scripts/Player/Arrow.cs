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

    // Set by PlayerShooting
    [HideInInspector] public ArrowFireType fireType = ArrowFireType.Weak;
    [HideInInspector] public float chargeAmount = 0f;  // 0–1, scales damage

    private Vector3 direction;
    private bool hasLanded = false;
    private float currentVelocity;
    private float lifeTime;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;

    public void Initialize(Vector3 shootDirection, LayerMask stickLayers)
    {
        direction = shootDirection.normalized;
        stickableLayers = stickLayers;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            upgradeManager = player.GetComponent<PlayerUpgradeManager>();
            playerStats = player.GetComponent<PlayerStats>();
        }
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;

        if (hasLanded) { currentVelocity = 0f; return; }

        Vector3 move = direction * speed * Time.deltaTime;
        currentVelocity = move.magnitude / Time.deltaTime;

        Vector3 nextPosition = transform.position + move;
        nextPosition.z = 0f;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, move.magnitude, stickableLayers))
        {
            StickToSurface(hit.point);
            playerStats?.RecordArrowHitWall();
            upgradeManager?.ArrowHitWall(hit.point);
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

        // Notify ArrowPickup — now collectible
        GetComponent<ArrowPickup>()?.OnArrowLanded();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentVelocity < damageVelocityThreshold) return;
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        // Base damage from PlayerStats, scaled by charge amount
        // 0% charge = 1.0x, 100% charge = 2.0x  (130% at 30% charge, etc.)
        float baseArrowDamage = playerStats != null ? playerStats.arrowDamage : 1f;
        float chargeMult = 1f + chargeAmount;  // chargeAmount is 0–1
        float baseDamage = baseArrowDamage * chargeMult;

        // Roll for crit
        float finalDamage;
        bool isCrit;

        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, other.gameObject);
        else
        {
            finalDamage = baseDamage;
            isCrit = false;
        }

        // Knockback — zero Z first, then normalize to avoid direction errors
        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;
        Vector3 rawDir = other.transform.position - transform.position;
        rawDir.z = 0f;
        Vector3 kbDir = rawDir.magnitude > 0.001f ? rawDir.normalized : Vector3.right;

        int roundedDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
        bool willKill = enemy.WillDie(roundedDamage);

        enemy.TakeDamage(
            roundedDamage,
            transform.position,
            kbDir,
            kbForce,
            isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal
        );

        playerStats?.RecordArrowHitEnemy();
        playerStats?.RecordDamageDealt(finalDamage, DamageSource.Arrow);
        upgradeManager?.ArrowHitEnemy(other.gameObject, chargeAmount, isCrit);

        // Pierce logic: stop on surviving enemy, continue through killed enemy
        if (!willKill)
            Destroy(gameObject);
    }
}