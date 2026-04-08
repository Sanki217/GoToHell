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

    [HideInInspector] public ArrowFireType fireType = ArrowFireType.Weak;
    [HideInInspector] public float chargeAmount = 0f;

    // Injected by PlayerShooting — how much extra damage per 1% charge
    // Default 2f means 100% charge = 2× base damage bonus on top of base
    [HideInInspector] public float chargeDamageMultiplierPerPercent = 2f;

    private Vector3 direction;
    private bool hasLanded = false;
    private float currentVelocity;
    private float lifeTime;
    private int piercesUsed = 0;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private KillStreak killStreak;

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
            killStreak = player.GetComponent<KillStreak>();
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

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit,
                            move.magnitude, stickableLayers))
        {
            // ── Check for destructibles before sticking ─────────────
            GameObject root = hit.collider.transform.root.gameObject;

            ExplosiveBarrel barrel = root.GetComponent<ExplosiveBarrel>()
                ?? hit.collider.GetComponent<ExplosiveBarrel>();
            Vase vase = root.GetComponent<Vase>()
                ?? hit.collider.GetComponent<Vase>();

            if (barrel != null)
            {
                int dmg = ArrowDamage();
                barrel.TakeDamage(dmg);
                Destroy(gameObject);
                return;
            }

            if (vase != null)
            {
                int dmg = ArrowDamage();
                vase.TakeDamage(dmg);
                Destroy(gameObject);
                return;
            }

            StickToSurface(hit.point);
            playerStats?.RecordArrowHitWall();
            upgradeManager?.ArrowHitWall(hit.point);
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    private int ArrowDamage()
    {
        float base_dmg = playerStats != null ? playerStats.arrowDamage : 1f;
        // charge scales at chargeDamageMultiplierPerPercent per 1% charge
        float chargeMult = 1f + chargeAmount * chargeDamageMultiplierPerPercent;
        float streakMult = killStreak != null ? killStreak.DamageMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(base_dmg * chargeMult * streakMult));
    }

    private void StickToSurface(Vector3 point)
    {
        hasLanded = true;
        direction = Vector3.zero;
        currentVelocity = 0f;
        transform.position = new Vector3(point.x, point.y, 0f);
        GetComponent<ArrowPickup>()?.OnArrowLanded();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentVelocity < damageVelocityThreshold) return;
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        float baseArrowDamage = playerStats != null ? playerStats.arrowDamage : 1f;
        float chargeMult = 1f + chargeAmount * chargeDamageMultiplierPerPercent;
        float streakMult = killStreak != null ? killStreak.DamageMultiplier : 1f;
        float baseDamage = baseArrowDamage * chargeMult * streakMult;

        float finalDamage;
        bool isCrit;

        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, other.gameObject);
        else { finalDamage = baseDamage; isCrit = false; }

        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;
        Vector3 rawDir = other.transform.position - transform.position;
        rawDir.z = 0f;
        Vector3 kbDir = rawDir.magnitude > 0.001f ? rawDir.normalized : Vector3.right;

        int roundedDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
        bool willKill = enemy.WillDie(roundedDamage);

        enemy.TakeDamage(roundedDamage, transform.position, kbDir, kbForce, isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal);

        playerStats?.RecordArrowHitEnemy();
        playerStats?.RecordDamageDealt(finalDamage, DamageSource.Arrow);
        upgradeManager?.ArrowHitEnemy(other.gameObject, chargeAmount, isCrit);

        if (!willKill)
        {
            int maxPierces = playerStats != null ? playerStats.arrowPierceCount : 0;
            if (piercesUsed < maxPierces)
            {
                piercesUsed++;

                if (playerStats != null && playerStats.pierceDmgAPScaling > 0f)
                {
                    float pierceDmg = playerStats.pierceDamageBase
                                      + playerStats.pierceDmgAPScaling * playerStats.abilityPower;
                    pierceDmg *= streakMult;
                    int pierceRound = Mathf.Max(1, Mathf.RoundToInt(pierceDmg));
                    enemy.TakeDamage(pierceRound, transform.position, kbDir, 0f, false,
                        FloatingTextManager.HitType.Normal);
                    playerStats.RecordDamageDealt(pierceDmg, DamageSource.Arrow);
                }
            }
            else
                Destroy(gameObject);
        }
        // If willKill: arrow always passes through (free)
    }
}