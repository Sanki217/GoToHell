using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A flying arrow. Moves itself (no rigidbody physics), sticks into
/// stickableLayers, damages enemies and destructibles on the way.
///
/// HIT DETECTION is swept: every frame the arrow sphere-casts along the
/// distance it is about to travel, so fast arrows can't tunnel through small
/// enemies at low frame rates (50 u/s at 30 fps = 1.7 units per frame — wider
/// than most enemies). The old trigger callback is kept as a fallback. Each
/// enemy is hit at most once per arrow, however many colliders it has.
///
/// DAMAGE = arrowDamage × charge × killstreak × damageMultiplier × bloodArrow
///          + flatDamageBonus, then crit (forced for Dead Man's Hand).
/// </summary>
public class Arrow : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float damageVelocityThreshold = 0.1f;

    [Header("Collision Layers")]
    public LayerMask stickableLayers;

    [Header("Hit Detection")]
    [Tooltip("Radius of the per-frame enemy sweep. Roughly the arrow head's size.")]
    public float hitRadius = 0.12f;

    [HideInInspector] public ArrowFireType fireType = ArrowFireType.Weak;
    [HideInInspector] public float chargeAmount = 0f;
    [HideInInspector] public float chargeDamageMultiplierPerPercent = 2f;

    /// <summary>Multiplicative damage scalar. 0.5 on Soul Arrow chain copies.</summary>
    [HideInInspector] public float damageMultiplier = 1f;

    /// <summary>Flat damage added after all multipliers (used by Soul Arrow for AP scaling).</summary>
    [HideInInspector] public float flatDamageBonus = 0f;

    /// <summary>When true this arrow is a Soul Arrow chain copy and will not trigger further chains.</summary>
    [HideInInspector] public bool isChainCopy = false;

    /// <summary>When true this arrow always crits (Dead Man's Hand).</summary>
    [HideInInspector] public bool isForcedCrit = false;

    /// <summary>True once this arrow has stuck to a surface and stopped moving.</summary>
    public bool HasLanded => hasLanded;

    private Vector3 direction;
    private bool hasLanded = false;
    private bool consumed = false;          // destroyed by a hit this frame
    private float currentVelocity;
    private float lifeTime;
    private int piercesUsed = 0;

    private readonly HashSet<int> hitTargets = new HashSet<int>();
    private static readonly RaycastHit[] sweepBuffer = new RaycastHit[16];
    private static readonly Collider[] destructibleScratch = new Collider[16];

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private KillStreak killStreak;

    public void Initialize(Vector3 shootDirection, LayerMask stickLayers)
    {
        direction = shootDirection.normalized;
        stickableLayers = stickLayers;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        var refs = PlayerRefs.I;
        if (refs != null)
        {
            upgradeManager = refs.Upgrades;
            playerStats = refs.Stats;
            killStreak = refs.Killstreak;
        }

        if (isChainCopy || playerStats == null) return;

        // Consume any one-shot damage multiplier primed by upgrades (e.g. New Sharp Set).
        // Chain-copy arrows skip this so the bonus only applies to player-fired shots.
        if (playerStats.nextArrowDamageMultiplier != 1f)
        {
            damageMultiplier *= playerStats.nextArrowDamageMultiplier;
            playerStats.nextArrowDamageMultiplier = 1f;
        }

        // Track last player-fired arrow reference (Dead Man's Hand fly-back).
        playerStats.lastFiredArrow = this;

        // Consume forced-crit flag (Dead Man's Hand — last arrow in quiver always crits).
        if (playerStats.nextArrowForceCrit)
        {
            isForcedCrit = true;
            playerStats.nextArrowForceCrit = false;
        }
    }

    // ================================================================
    //  FLIGHT
    // ================================================================

    private void Update()
    {
        lifeTime += Time.deltaTime;
        if (hasLanded || consumed) { currentVelocity = 0f; return; }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // paused

        currentVelocity = speed;
        float dist = speed * dt;
        if (dist <= 0f) return;

        Vector3 origin = transform.position;
        bool hitSolid = Physics.Raycast(origin, direction, out RaycastHit solidHit, dist,
                                        stickableLayers, QueryTriggerInteraction.Ignore);
        float travel = hitSolid ? solidHit.distance : dist;

        // Enemies between here and the wall (or the end of this frame's step).
        // stopAt = where the arrow was used up, so nothing behind that point is hit.
        bool alive = SweepEnemies(origin, travel, out float stopAt);

        // Vases / barrels are usually TRIGGER colliders on the Destructibles layer,
        // which the solid raycast ignores — sweep for them separately.
        SweepDestructibles(origin, stopAt);
        if (!alive) return;   // arrow consumed

        Vector3 next = origin + direction * dist;
        next.z = 0f;

        if (!hitSolid)
        {
            transform.position = next;
            return;
        }

        // Solid (non-trigger) destructible in the way: damage it and fly on through
        IDestructible destructible = CombatQuery.DestructibleOf(solidHit.collider);
        if (destructible != null)
        {
            HitDestructible(destructible);
            transform.position = next;
            return;
        }

        StickToSurface(solidHit.point);
        if (playerStats != null) playerStats.RecordArrowHitWall();
        if (upgradeManager != null) upgradeManager.ArrowHitWall(solidHit.point);
    }

    /// <summary>
    /// Hits every enemy along the segment, nearest first. Returns false if the
    /// arrow was consumed; <paramref name="stopAt"/> is how far the arrow got.
    /// </summary>
    private bool SweepEnemies(Vector3 origin, float distance, out float stopAt)
    {
        stopAt = distance;
        int count = Physics.SphereCastNonAlloc(origin, hitRadius, direction, sweepBuffer, distance,
                                               Layers.EnemyMask, QueryTriggerInteraction.Collide);
        if (count == 0) return true;

        System.Array.Sort(sweepBuffer, 0, count, HitDistanceComparer.Instance);

        for (int i = 0; i < count; i++)
        {
            RaycastHit h = sweepBuffer[i];
            sweepBuffer[i] = default;
            Collider col = h.collider;
            if (col == null || !col.CompareTag("Enemy")) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            // Initial-overlap hits report distance 0 and no valid point
            Vector3 point = h.distance > 0f ? h.point : origin;
            if (!HitEnemy(enemy, point))
            {
                stopAt = h.distance;
                ClearSweep(i + 1, count);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Damages every destructible (vase, barrel…) along the segment once.
    /// Triggers included; layer = Destructibles or anything in stickableLayers.
    /// </summary>
    private void SweepDestructibles(Vector3 origin, float distance)
    {
        int mask = stickableLayers | Layers.MaskOf(Layers.Destructibles);
        int count = Physics.SphereCastNonAlloc(origin, hitRadius, direction, sweepBuffer, distance,
                                               mask, QueryTriggerInteraction.Collide);
        if (count == 0) return;

        System.Array.Sort(sweepBuffer, 0, count, HitDistanceComparer.Instance);

        // Copy the colliders out first: a barrel exploding inside TakeDamage can
        // run other physics queries before this loop is done.
        int n = Mathf.Min(count, destructibleScratch.Length);
        for (int i = 0; i < n; i++) destructibleScratch[i] = sweepBuffer[i].collider;
        ClearSweep(0, count);

        for (int i = 0; i < n; i++)
        {
            IDestructible d = CombatQuery.DestructibleOf(destructibleScratch[i]);
            destructibleScratch[i] = null;
            if (d != null) HitDestructible(d);
        }
    }

    private void HitDestructible(IDestructible destructible)
    {
        if (!(destructible is Object uo) || uo == null) return;
        if (!hitTargets.Add(uo.GetInstanceID())) return;

        destructible.TakeDamage(ArrowDamage());
        if (playerStats != null) playerStats.RecordArrowHitDestructible();
        if (upgradeManager != null && uo is Component c && c != null)
            upgradeManager.ArrowHitDestructible(c.gameObject);
    }

    private static void ClearSweep(int from, int count)
    {
        for (int i = from; i < count; i++) sweepBuffer[i] = default;
    }

    private sealed class HitDistanceComparer : IComparer<RaycastHit>
    {
        public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

    // ================================================================
    //  DAMAGE
    // ================================================================

    private float BaseDamage()
    {
        float baseArrowDamage = playerStats != null ? playerStats.arrowDamage : 1f;
        float chargeMult = 1f + chargeAmount * chargeDamageMultiplierPerPercent;
        float streakMult = killStreak != null ? killStreak.DamageMultiplier : 1f;
        float bloodMult = playerStats != null ? playerStats.bloodArrowMultiplier : 1f;
        return baseArrowDamage * chargeMult * streakMult * damageMultiplier * bloodMult + flatDamageBonus;
    }

    private int ArrowDamage() => Mathf.Max(1, Mathf.RoundToInt(BaseDamage()));

    /// <summary>Applies one arrow hit. Returns false when the arrow is used up.</summary>
    private bool HitEnemy(Enemy enemy, Vector3 hitPoint)
    {
        if (enemy.IsDead || !hitTargets.Add(enemy.GetInstanceID())) return true;

        float baseDamage = BaseDamage();
        float finalDamage;
        bool isCrit;
        if (playerStats != null)
            (finalDamage, isCrit) = playerStats.RollDamage(baseDamage, enemy.gameObject, isForcedCrit);
        else { finalDamage = baseDamage; isCrit = isForcedCrit; }

        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;
        Vector3 kbDir = CombatQuery.PlanarDirection(transform.position, enemy.transform.position, direction);

        GameObject enemyGO = enemy.gameObject;
        bool killed = enemy.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(finalDamage)), hitPoint, kbDir, kbForce,
            isCrit, isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal);

        if (playerStats != null)
        {
            playerStats.RecordArrowHitEnemy();
            playerStats.RecordDamageDealt(finalDamage, DamageSource.Arrow, enemyGO);
        }
        if (upgradeManager != null) upgradeManager.ArrowHitEnemy(enemyGO, chargeAmount, isCrit);

        // Soul Arrow etc. (chain copies excluded to prevent infinite chaining)
        if (killed)
        {
            if (!isChainCopy && upgradeManager != null) upgradeManager.ArrowKill(enemyGO);
            return true;   // arrow passes through the dying enemy for free
        }

        int maxPierces = playerStats != null ? playerStats.arrowPierceCount : 0;
        if (piercesUsed < maxPierces)
        {
            piercesUsed++;

            if (playerStats != null && playerStats.pierceDmgAPScaling > 0f && !enemy.IsDead)
            {
                float streakMult = killStreak != null ? killStreak.DamageMultiplier : 1f;
                float pierceDmg = (playerStats.pierceDamageBase
                                   + playerStats.pierceDmgAPScaling * playerStats.psyche) * streakMult;
                enemy.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(pierceDmg)), hitPoint, kbDir, 0f, false,
                                 FloatingTextManager.HitType.Normal);
                playerStats.RecordDamageDealt(pierceDmg, DamageSource.Arrow, enemyGO);
            }
            return true;
        }

        consumed = true;
        Destroy(gameObject);
        return false;
    }

    // ================================================================
    //  STICK / TRIGGER FALLBACK
    // ================================================================

    private void StickToSurface(Vector3 point)
    {
        hasLanded = true;
        direction = Vector3.zero;
        currentVelocity = 0f;
        transform.position = new Vector3(point.x, point.y, 0f);

        // Chain-copy arrows (Mirror Arrow, Soul Arrow) are never pickable — skip OnArrowLanded.
        if (!isChainCopy && TryGetComponent(out ArrowPickup pickup)) pickup.OnArrowLanded();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed || hasLanded) return;
        if (currentVelocity < damageVelocityThreshold) return;
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null) HitEnemy(enemy, transform.position);
    }
}
