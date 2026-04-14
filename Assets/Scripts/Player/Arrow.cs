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
    [HideInInspector] public float chargeDamageMultiplierPerPercent = 2f;

    /// <summary>Multiplicative damage scalar. 0.5 on Soul Arrow chain copies.</summary>
    [HideInInspector] public float damageMultiplier = 1f;

    /// <summary>Flat damage added after all multipliers (used by Soul Arrow for AP scaling).</summary>
    [HideInInspector] public float flatDamageBonus = 0f;

    /// <summary>When true this arrow is a Soul Arrow chain copy and will not trigger further chains.</summary>
    [HideInInspector] public bool isChainCopy = false;

    /// <summary>When true this arrow will force a critical hit on its next impact (Dead Man's Hand).</summary>
    [HideInInspector] public bool isForcedCrit = false;

    /// <summary>True once this arrow has stuck to a surface and stopped moving.</summary>
    public bool HasLanded => hasLanded;

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

        // Consume any one-shot damage multiplier primed by upgrades (e.g. New Sharp Set).
        // Chain-copy arrows skip this so the bonus only applies to player-fired shots.
        if (!isChainCopy && playerStats != null && playerStats.nextArrowDamageMultiplier != 1f)
        {
            damageMultiplier *= playerStats.nextArrowDamageMultiplier;
            playerStats.nextArrowDamageMultiplier = 1f;
        }

        // Track last player-fired arrow reference (Dead Man's Hand fly-back).
        if (!isChainCopy && playerStats != null)
            playerStats.lastFiredArrow = this;

        // Consume forced-crit flag (Dead Man's Hand — last arrow in quiver always crits).
        if (!isChainCopy && playerStats != null && playerStats.nextArrowForceCrit)
        {
            isForcedCrit = true;
            playerStats.nextArrowForceCrit = false;
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
            GameObject root = hit.collider.transform.root.gameObject;

            ExplosiveBarrel barrel = root.GetComponent<ExplosiveBarrel>()
                ?? hit.collider.GetComponent<ExplosiveBarrel>();
            Vase vase = root.GetComponent<Vase>()
                ?? hit.collider.GetComponent<Vase>();

            if (barrel != null)
            {
                barrel.TakeDamage(ArrowDamage());
                transform.position = nextPosition;
                return;
            }

            if (vase != null)
            {
                vase.TakeDamage(ArrowDamage());
                transform.position = nextPosition;
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
        float chargeMult = 1f + chargeAmount * chargeDamageMultiplierPerPercent;
        float streakMult = killStreak != null ? killStreak.DamageMultiplier : 1f;
        float bloodMult = playerStats != null ? playerStats.bloodArrowMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(
            base_dmg * chargeMult * streakMult * damageMultiplier * bloodMult + flatDamageBonus));
    }

    private void StickToSurface(Vector3 point)
    {
        hasLanded = true;
        direction = Vector3.zero;
        currentVelocity = 0f;
        transform.position = new Vector3(point.x, point.y, 0f);
        // Chain-copy arrows (Mirror Arrow, Soul Arrow) are never pickable — skip OnArrowLanded.
        if (!isChainCopy) GetComponent<ArrowPickup>()?.OnArrowLanded();
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
        float bloodMult = playerStats != null ? playerStats.bloodArrowMultiplier : 1f;
        float baseDamage = baseArrowDamage * chargeMult * streakMult * damageMultiplier * bloodMult
                           + flatDamageBonus;

        float finalDamage;
        bool isCrit;

        // Dead Man's Hand: temporarily guarantee a crit for the last-quiver arrow.
        float savedCritChance = 0f;
        if (isForcedCrit && playerStats != null)
        {
            savedCritChance = playerStats.critChance;
            playerStats.critChance = 2f; // > 1 guarantees Random.value < critChance
        }

        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, enemy.gameObject);
        else { finalDamage = baseDamage; isCrit = false; }

        if (isForcedCrit && playerStats != null)
            playerStats.critChance = savedCritChance; // restore original crit chance

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
        upgradeManager?.ArrowHitEnemy(enemy.gameObject, chargeAmount, isCrit);

        // Notify Soul Arrow (chain copies excluded to prevent infinite chaining)
        if (willKill && !isChainCopy)
            upgradeManager?.ArrowKill(enemy.gameObject);

        if (!willKill)
        {
            int maxPierces = playerStats != null ? playerStats.arrowPierceCount : 0;
            if (piercesUsed < maxPierces)
            {
                piercesUsed++;

                if (playerStats != null && playerStats.pierceDmgAPScaling > 0f)
                {
                    float pierceDmg = playerStats.pierceDamageBase
                                      + playerStats.pierceDmgAPScaling * playerStats.psyche;
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
        // willKill: arrow passes through the dying enemy for free
    }
}
