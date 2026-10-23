using System;
using UnityEngine;

/// <summary>
/// Unified player stat system — two-tier architecture.
///
/// PRIMARY STATS (player upgrades these 7):
///   Agility, AttackDamage, Luck, Psyche, Health, Size, Cooldown
///
/// DERIVED STATS (calculated from primaries — read-only at runtime):
///   All specific gameplay values: moveSpeed, arrowDamage, critChance, etc.
///   RecalculateDerived() runs whenever a primary stat changes and raises
///   OnStatsChanged, so dependants (PlayerHealth max HP, live upgrade values)
///   react to changes instead of polling every frame.
///
/// RULE: upgrades that want a permanent bonus modify a FORMULA input (a base
/// value or coefficient, e.g. baseBurnStrength) and call RecalculateDerived —
/// never a derived field directly, which the next recalculation overwrites.
///
/// FORMULAS (all coefficients editable in Inspector under "Derived Formulas"):
///   e.g. moveSpeed = baseMoveSpeed + moveSpeedPerAgility * agility
///
/// RUN HISTORY lives in <see cref="history"/> — the same RunStats type that is
/// saved to disk, so a new tracked stat is ONE field on RunStats plus a Record
/// call. Snapshot, lifetime totals, best-run and achievements pick it up.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ================================================================
    //  REFERENCES
    // ================================================================

    private PlayerShooting shooting;
    private PlayerHealth health;
    private PlayerUpgradeManager upgradeManager;

    /// <summary>Raised after every RecalculateDerived (primary stat or formula change).</summary>
    public event Action OnStatsChanged;

    private void Awake()
    {
        shooting = GetComponent<PlayerShooting>();
        health = GetComponent<PlayerHealth>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();

#if UNITY_EDITOR
        _lastReadArrows = _currentArrows;
        _lastReadHP = _currentHP;
#endif

        RecalculateDerived();
    }

    // ================================================================
    //  LEVEL
    // ================================================================

    [Header("--- LEVEL ---")]
    public int currentLevel = 1;

    // ================================================================
    //  PRIMARY STATS  (these are what players upgrade)
    // ================================================================

    [Header("=== PRIMARY STATS ===")]
    [Tooltip("Affects: move speed, dash distance, charge time")]
    public float agility = 0f;

    [Tooltip("Affects: arrow damage, dash damage, crit multiplier, knockback, slash damage")]
    public float attackDamage = 0f;

    [Tooltip("Affects: crit chance, dash invincibility, loot range, luck (rarity rolls)")]
    public float luck = 0f;

    [Tooltip("Affects: hover drain, dash cost, charge drain, XP multiplier, " +
             "max energy, burn/freeze/holy/shock strength, all upgrade ability-power scaling")]
    public float psyche = 0f;

    [Tooltip("Affects: max HP")]
    public float health_stat = 0f;   // named health_stat to avoid clash with PlayerHealth

    [Tooltip("Affects: AoE radii, collider sizes for upgrades and slash")]
    public float size = 0f;

    [Tooltip("Affects: arrow cooldown, dash cooldown, slash cooldown, upgrade cooldowns")]
    public float cooldown = 0f;

    // ================================================================
    //  DERIVED STAT FORMULAS  (Inspector-configurable coefficients)
    // ================================================================

    [Header("=== DERIVED FORMULAS — Movement ===")]
    public float baseMoveSpeed = 10f;
    public float moveSpeedPerAgility = 0.10f;   // +10% agility → +1 move speed per 10 agility

    public float baseAcceleration = 30f;
    public float accelerationPerAgility = 0.5f; // +0.5 acceleration per 1 agility point

    public float baseDashDistance = 5f;
    public float dashDistPerAgility = 0.10f;

    public float baseChargeTime = 1.5f;
    public float chargeTimePerAgility = -0.02f;  // negative = faster charge

    [Header("=== DERIVED FORMULAS — Combat ===")]
    public float baseArrowDamage = 10f;
    public float arrowDmgPerAttack = 1.20f;

    public float baseSlashDamage = 5f;
    public float slashDmgPerAttack = 0.60f;

    public float baseDashDamage = 10f;
    public float dashDmgPerAttack = 1.00f;

    public float baseCritChance = 0.10f;   // 10%
    public float critChancePerLuck = 0.0050f; // +0.5% per luck (50% of luck → as fraction)
    [Tooltip("Hard cap on crit chance (1.0 = 100%)")]
    public float maxCritChance = 1.00f;

    public float baseCritMultiplier = 1.50f;   // 150%
    public float critMultPerAttack = 0.0050f; // +0.5% per attack damage

    public float baseKnockback = 1f;
    public float knockbackPerAttack = 0.10f;
    [Tooltip("Hard cap on knockback")]
    public float maxKnockback = 10f;

    [Header("=== DERIVED FORMULAS — Economy ===")]
    public float baseMaxEnergy = 100f;
    public float energyPerPsyche = 0.10f;

    public float baseDashCost = 15f;
    public float dashCostPerPsyche = -0.20f;  // negative = cheaper
    [Tooltip("Minimum dash cost (floor)")]
    public float minDashCost = 5f;

    public float baseDashInvinc = 0.20f;
    public float dashInvincPerLuck = 0.0002f;

    public float baseHoverDrain = 10f;
    public float hoverDrainPerPsyche = -0.15f;
    [Tooltip("Minimum hover drain rate")]
    public float minHoverDrain = 3f;

    public float baseChargeDrain = 10f;
    public float chargeDrainPerPsyche = -0.30f;
    [Tooltip("Minimum charge drain rate")]
    public float minChargeDrain = 0.5f;

    public float baseLootRange = 3f;
    public float lootRangePerLuck = 0.05f;
    [Tooltip("Maximum loot range")]
    public float maxLootRange = 8f;

    public float baseLuckRoll = 1.4f;    // luck value fed to rarity roller
    public float luckRollPerLuck = 0.10f;

    [Header("=== DERIVED FORMULAS — Status Effects ===")]
    [Tooltip("Base burn strength (1.0 = 100% = 5 dmg/s)")]
    public float baseBurnStrength = 1.00f;
    public float burnPerPsyche = 0.0050f;

    public float baseFreezeStrength = 1.00f;
    public float freezePerPsyche = 0.0050f;

    public float baseHolyStrength = 1.00f;
    public float holyPerPsyche = 0.0100f;

    public float baseShockStrength = 1.00f;
    public float shockPerPsyche = 0.0050f;

    [Header("=== DERIVED FORMULAS — Health ===")]
    public float baseMaxHP = 100f;
    public float hpPerHealth = 1.00f;    // 100% health stat = +1 HP per point

    [Tooltip("Flat max HP from items/upgrades (PlayerHealth.IncreaseMaxHP). Survives recalculation.")]
    public float bonusMaxHP = 0f;

    [Tooltip("Flat crit chance from class passives / effects (0.1 = +10%). Survives recalculation.")]
    public float bonusCritChance = 0f;

    // ================================================================
    //  DERIVED STATS  (read-only — computed by RecalculateDerived)
    // ================================================================

    [Header("=== DERIVED STATS (read-only) ===")]
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float acceleration;
    [HideInInspector] public float dashDistance;
    [HideInInspector] public float arrowChargeDuration;
    [HideInInspector] public float arrowDamage;
    [HideInInspector] public float slashDamage;
    [HideInInspector] public float dashDamage;
    [HideInInspector] public float critChance;
    [HideInInspector] public float critMultiplier;
    [HideInInspector] public float knockbackForce;
    [HideInInspector] public float maxEnergy;
    [HideInInspector] public float dashCost;
    [HideInInspector] public float dashInvincibilityWindow;
    [HideInInspector] public float hoverDrainRate;
    [HideInInspector] public float arrowChargeDrainRate;
    [HideInInspector] public float lootRange;
    [HideInInspector] public float luckRoll;          // fed to UpgradeRarityRoller
    [HideInInspector] public float energyRegenMultiplier = 1f; // future upgrade hook
    [HideInInspector] public float burnStrength;
    [HideInInspector] public float freezeStrength;
    [HideInInspector] public float holyStrength;
    [HideInInspector] public float shockStrength;
    [HideInInspector] public int maxHP;

    // Lifesteal — internal only, acquired through upgrades
    [HideInInspector] public float lifeSteal = 0f;

    // Blood Arrow — damage multiplier when below HP threshold (set by UpgradeBloodArrow)
    [HideInInspector] public float bloodArrowMultiplier = 1f;

    // One-shot multiplier consumed by the next arrow spawned (read + reset in Arrow.Initialize)
    // Used by New Sharp Set. Other upgrades may also write to this.
    [HideInInspector] public float nextArrowDamageMultiplier = 1f;

    // One-shot force-crit flag consumed by Arrow.Initialize (used by Dead Man's Hand).
    // Causes the next non-chain arrow to guaranteed crit regardless of critChance.
    [HideInInspector] public bool nextArrowForceCrit = false;

    // Reference to the last non-chain arrow spawned — used by Dead Man's Hand for fly-back.
    [HideInInspector] public Arrow lastFiredArrow;

    // Pierce — set by upgrade
    [HideInInspector] public int arrowPierceCount = 0;
    [HideInInspector] public float pierceDamageBase = 5f;   // flat bonus per pierce
    [HideInInspector] public float pierceDmgAPScaling = 0f;   // fraction of Psyche added

    // Fixed values (not upgradeable via primaries)
    [Header("=== FIXED VALUES ===")]
    public float jumpForce = 12f;   // constant, not shown to player
    public float wallSlideSpeed = -3f; // constant

    // ================================================================
    //  RECALCULATE
    // ================================================================

    /// <summary>
    /// Recomputes all derived stats from current primary stats.
    /// Call this whenever any primary stat or formula input changes.
    /// </summary>
    public void RecalculateDerived()
    {
        // Movement
        moveSpeed = baseMoveSpeed + moveSpeedPerAgility * agility;
        acceleration = baseAcceleration + accelerationPerAgility * agility;
        dashDistance = baseDashDistance + dashDistPerAgility * agility;
        arrowChargeDuration = Mathf.Max(0.1f, baseChargeTime + chargeTimePerAgility * agility);

        // Combat
        arrowDamage = baseArrowDamage + arrowDmgPerAttack * attackDamage;
        slashDamage = baseSlashDamage + slashDmgPerAttack * attackDamage;
        dashDamage = baseDashDamage + dashDmgPerAttack * attackDamage;
        critChance = Mathf.Min(maxCritChance, baseCritChance + critChancePerLuck * luck + bonusCritChance);
        critMultiplier = baseCritMultiplier + critMultPerAttack * attackDamage;
        knockbackForce = Mathf.Min(maxKnockback, baseKnockback + knockbackPerAttack * attackDamage);

        // Economy
        maxEnergy = baseMaxEnergy + energyPerPsyche * psyche;
        dashCost = Mathf.Max(minDashCost, baseDashCost + dashCostPerPsyche * psyche);
        dashInvincibilityWindow = baseDashInvinc + dashInvincPerLuck * luck;
        hoverDrainRate = Mathf.Max(minHoverDrain, baseHoverDrain + hoverDrainPerPsyche * psyche);
        arrowChargeDrainRate = Mathf.Max(minChargeDrain, baseChargeDrain + chargeDrainPerPsyche * psyche);
        lootRange = Mathf.Min(maxLootRange, baseLootRange + lootRangePerLuck * luck);
        luckRoll = baseLuckRoll + luckRollPerLuck * luck;

        // Status effects
        burnStrength = baseBurnStrength + burnPerPsyche * psyche;
        freezeStrength = baseFreezeStrength + freezePerPsyche * psyche;
        holyStrength = baseHolyStrength + holyPerPsyche * psyche;
        shockStrength = baseShockStrength + shockPerPsyche * psyche;

        // Health
        maxHP = Mathf.Max(1, Mathf.RoundToInt(baseMaxHP + hpPerHealth * health_stat + bonusMaxHP));

        OnStatsChanged?.Invoke();
    }

    // ================================================================
    //  PRIMARY STAT MUTATION  (always call these, never set primaries directly)
    // ================================================================

    public void AddPrimary(PrimaryStat stat, float amount)
    {
        if (amount == 0f) return;
        switch (stat)
        {
            case PrimaryStat.Agility: agility += amount; break;
            case PrimaryStat.AttackDamage: attackDamage += amount; break;
            case PrimaryStat.Luck: luck += amount; break;
            case PrimaryStat.Psyche: psyche += amount; break;
            case PrimaryStat.Health: health_stat += amount; break;
            case PrimaryStat.Size: size += amount; break;
            case PrimaryStat.Cooldown: cooldown += amount; break;
        }
        RecalculateDerived();   // PlayerHealth listens and syncs max HP
    }

    public float GetPrimary(PrimaryStat stat) => stat switch
    {
        PrimaryStat.Agility => agility,
        PrimaryStat.AttackDamage => attackDamage,
        PrimaryStat.Luck => luck,
        PrimaryStat.Psyche => psyche,
        PrimaryStat.Health => health_stat,
        PrimaryStat.Size => size,
        PrimaryStat.Cooldown => cooldown,
        _ => 0f
    };

    /// <summary>Permanent flat max-HP bonus (items, upgrades). Survives recalculation.</summary>
    /// <summary>Permanent flat crit chance (class passives). 0.1 = +10%.</summary>
    public void AddBonusCritChance(float amount)
    {
        if (amount == 0f) return;
        bonusCritChance += amount;
        RecalculateDerived();
    }

    public void AddBonusMaxHP(float amount)
    {
        if (amount == 0f) return;
        bonusMaxHP += amount;
        RecalculateDerived();
    }

    // ================================================================
    //  AMMO / HP — live values come from PlayerShooting / PlayerHealth.
    //  The serialized fields are an Inspector mirror for live tuning in the
    //  Editor only; builds skip the per-frame sync entirely.
    // ================================================================

    [Header("--- AMMO (Inspector mirror) ---")]
    [SerializeField] private int _maxArrows = 3;
    [SerializeField] private int _currentArrows = 3;

    public int MaxArrows
    {
        get => shooting != null ? shooting.maxArrows : _maxArrows;
        set
        {
            _maxArrows = value;
            if (shooting != null) shooting.SetMaxArrows(value);
        }
    }

    public int CurrentArrows
    {
        get => shooting != null ? shooting.CurrentArrows : _currentArrows;
        set
        {
            _currentArrows = value;
            if (shooting != null) shooting.SetCurrentArrows(value);
        }
    }

    [Header("--- HP (Inspector mirror) ---")]
    [SerializeField] private int _currentHP = 100;

    public int CurrentHP
    {
        get => health != null ? health.CurrentHP : _currentHP;
        set
        {
            _currentHP = value;
#if UNITY_EDITOR
            _lastReadHP = value;
#endif
            if (health != null) health.SetHP(value);
        }
    }

    /// <summary>
    /// Called by PlayerHealth whenever HP changes so the Inspector mirror can
    /// never push a stale value back over a fresh heal/damage.
    /// </summary>
    public void SyncHPMirror(int hp)
    {
        _currentHP = hp;
#if UNITY_EDITOR
        _lastReadHP = hp;
#endif
    }

#if UNITY_EDITOR
    private int _lastReadArrows = -1;
    private int _lastReadHP = -1;

    // Two-way Inspector sync: edit a mirror field in Play mode and it is pushed
    // to the live component; otherwise the mirror follows the live value.
    private void Update()
    {
        if (shooting != null)
        {
            if (_currentArrows != _lastReadArrows)
                shooting.SetCurrentArrows(_currentArrows);
            _currentArrows = _lastReadArrows = shooting.CurrentArrows;

            // Inspector edit since last frame → push; otherwise follow (shop/upgrade changes)
            if (_maxArrowsSeen != -1 && _maxArrows != _maxArrowsSeen)
                shooting.SetMaxArrows(_maxArrows);
            _maxArrows = _maxArrowsSeen = shooting.maxArrows;
        }

        if (health != null)
        {
            if (_currentHP != _lastReadHP)
                health.SetHP(_currentHP);
            _currentHP = _lastReadHP = health.CurrentHP;
        }
    }

    private int _maxArrowsSeen = -1;
#endif

    // ================================================================
    //  CRIT SYSTEM
    // ================================================================

    /// <summary>
    /// Rolls crit for a hit. <paramref name="forceCrit"/> guarantees a crit
    /// (Dead Man's Hand) without touching critChance.
    /// </summary>
    public (float damage, bool isCrit) RollDamage(float baseDamage, GameObject target = null,
                                                  bool forceCrit = false)
    {
        bool isCrit = forceCrit || UnityEngine.Random.value < critChance;
        float finalDmg = isCrit ? baseDamage * critMultiplier : baseDamage;
        if (isCrit)
        {
            history.critsLanded++;
            if (upgradeManager != null) upgradeManager.CriticalHit(target, finalDmg);
        }
        return (finalDmg, isCrit);
    }

    // ================================================================
    //  RUN HISTORY
    // ================================================================

    [Header("--- RUN HISTORY ---")]
    [Tooltip("Everything recorded this run. Same type as the saved run snapshot.")]
    public RunStats history = new RunStats();

    // Lifesteal: fractional healing carried between hits so small percentages
    // still add up instead of each hit healing a rounded-up minimum of 1.
    private float lifestealCarry;

    // ── Record helpers ──

    public void RecordJump() => history.jumpsPerformed++;
    public void RecordWallSlideStart() => history.wallSlideCount++;
    public void RecordWallSlideTick(float dt) => history.totalWallSlideDuration += dt;
    public void RecordHoverStart() => history.hoverCount++;
    public void RecordHoverTick(float dt) => history.totalHoverDuration += dt;
    public void RecordDash(float dist) { history.dashCount++; history.totalDashDistance += dist; }
    public void RecordDashHitEnemy() => history.dashesHitEnemy++;
    public void RecordDashHitWall() => history.dashesHitWall++;
    public void RecordSlashUsed() => history.slashCount++;
    public void RecordSlashHitEnemy() => history.slashesHitEnemy++;
    public void RecordArrowHitEnemy() => history.arrowsHitEnemy++;
    public void RecordArrowHitWall() => history.arrowsHitWall++;
    public void RecordArrowHitDestructible() => history.arrowsHitDestructible++;
    public void RecordArrowPickedUp() => history.arrowsPickedUp++;
    public void RecordChargeCancelled() => history.chargesCancelledByEnergy++;
    public void RecordCritLanded() => history.critsLanded++;
    public void RecordLayerCompleted() => history.layersCompleted++;
    public void RecordHPRestored(int amount) => history.hpRestored += amount;
    public void RecordDamageTaken(float amount) { history.damageTaken += amount; history.timesHit++; }
    public void RecordSoulCollected(int amount) => history.soulsCollected += amount;
    public void RecordXpGained(float amount) => history.xpGained += amount;

    public void RecordEnemyKilled()
    {
        history.enemiesKilled++;
        HitStopManager.NotifyKill();
    }

    public void RecordArrowFired(ArrowFireType type)
    {
        history.totalArrowsFired++;
        switch (type)
        {
            case ArrowFireType.Weak: history.weakArrowsFired++; break;
            case ArrowFireType.Medium: history.mediumArrowsFired++; break;
            case ArrowFireType.Charged: history.chargedArrowsFired++; break;
            case ArrowFireType.Extra: history.extraArrowsFired++; break;
        }
    }

    public void RecordMovement(float dx, float dy, float dt, bool grounded, bool airborne)
    {
        if (dx < 0) history.distanceMovedLeft += -dx; else history.distanceMovedRight += dx;
        history.totalDistance += Mathf.Sqrt(dx * dx + dy * dy);
        if (grounded) history.timeGrounded += dt;
        if (airborne) history.timeAirborne += dt;
    }

    public void RecordDamageDealt(float amount, DamageSource source, GameObject target = null)
    {
        HitStopManager.NotifyDamageDealt(amount, source);

        history.totalDamageDealt += amount;
        switch (source)
        {
            case DamageSource.Arrow: history.damageByArrow += amount; break;
            case DamageSource.Dash: history.damageByDash += amount; break;
            case DamageSource.Status: history.damageByStatus += amount; break;
            case DamageSource.Explosion: history.damageByExplosion += amount; break;
            case DamageSource.Slash: history.damageBySlash += amount; break;
        }

        // Lifesteal — never procs off the training dummy (infinite-heal exploit)
        if (lifeSteal <= 0f || health == null || amount <= 0f) return;
        if (target != null && target.GetComponentInParent<TrainingDummy>() != null) return;

        lifestealCarry += amount * lifeSteal;
        int heal = Mathf.FloorToInt(lifestealCarry);
        if (heal <= 0) return;
        lifestealCarry -= heal;
        health.RestoreHP(heal);
    }

    public void RecordStatusApplied(StatusType type)
    {
        switch (type)
        {
            case StatusType.Burn: history.burnApplied++; break;
            case StatusType.Freeze: history.freezeApplied++; break;
            case StatusType.Holy: history.holyApplied++; break;
            case StatusType.Shock: history.shockApplied++; break;
        }
    }

    public void RecordEnergyGained(float amount, EnergySource source)
    {
        history.energyGainedTotal += amount;
        switch (source)
        {
            case EnergySource.Kill: history.energyFromKills += amount; break;
            case EnergySource.Falling: history.energyFromFalling += amount; break;
            case EnergySource.Lava: history.energyFromLava += amount; break;
            case EnergySource.WallSlide: history.energyFromWallSlide += amount; break;
        }
    }

    public void RecordEnergySpent(float amount, EnergySpentSource source)
    {
        switch (source)
        {
            case EnergySpentSource.Dash: history.energySpentDashing += amount; break;
            case EnergySpentSource.Hover: history.energySpentHovering += amount; break;
            case EnergySpentSource.Charging: history.energySpentCharging += amount; break;
        }
    }

    public void RecordPlayerLevelUp(int newLevel)
    {
        currentLevel = newLevel;
        history.finalLevel = newLevel;
    }
}

// ================================================================
//  ENUMS
// ================================================================

public enum PrimaryStat
{
    Agility, AttackDamage, Luck, Psyche, Health, Size, Cooldown
}

public enum DamageSource { Arrow, Dash, Status, Explosion, Slash }
public enum EnergySpentSource { Dash, Hover, Charging }
public enum ArrowFireType { Weak, Medium, Charged, Extra }
