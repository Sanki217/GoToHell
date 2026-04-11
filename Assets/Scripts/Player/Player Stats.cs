using UnityEngine;

/// <summary>
/// Unified player stat system — two-tier architecture.
///
/// PRIMARY STATS (player upgrades these 8):
///   Agility, AttackDamage, AbilityPower, Luck, Psyche, Health, Size, Cooldown
///
/// DERIVED STATS (calculated from primaries — read-only at runtime):
///   All specific gameplay values: moveSpeed, arrowDamage, critChance, etc.
///   RecalculateDerived() is called automatically whenever a primary stat changes.
///
/// FORMULAS (all coefficients editable in Inspector under "Derived Formulas"):
///   e.g. moveSpeed = baseMoveSpeed + moveSpeedPerAgility * agility
///
/// Run History is unchanged — still records everything.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ================================================================
    //  REFERENCES
    // ================================================================

    private PlayerShooting shooting;
    private PlayerHealth health;
    private PlayerUpgradeManager upgradeManager;

    private void Awake()
    {
        shooting = GetComponent<PlayerShooting>();
        health = GetComponent<PlayerHealth>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();

        _lastReadArrows = _currentArrows;
        _lastReadHP = _currentHP;

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

    [Tooltip("Affects: burn/freeze/holy/shock strength, max energy, ability-power-scaling upgrades")]
    public float abilityPower = 0f;

    [Tooltip("Affects: crit chance, dash invincibility, loot range, luck (rarity rolls)")]
    public float luck = 0f;

    [Tooltip("Affects: hover drain, dash cost, charge drain, XP multiplier")]
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
    public float energyPerAbilityPower = 0.10f;

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
    public float burnPerAbilityPower = 0.0050f;

    public float baseFreezeStrength = 1.00f;
    public float freezePerAbilityPower = 0.0050f;

    public float baseHolyStrength = 1.00f;
    public float holyPerAbilityPower = 0.0100f;  // double rate

    public float baseShockStrength = 1.00f;
    public float shockPerAbilityPower = 0.0050f;

    [Header("=== DERIVED FORMULAS — Health ===")]
    public float baseMaxHP = 100f;
    public float hpPerHealth = 1.00f;    // 100% health stat = +1 HP per point

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
    // Used by First Strike. Other upgrades may also write to this.
    [HideInInspector] public float nextArrowDamageMultiplier = 1f;

    // Pierce — set by upgrade
    [HideInInspector] public int arrowPierceCount = 0;
    [HideInInspector] public float pierceDamageBase = 5f;   // flat bonus per pierce
    [HideInInspector] public float pierceDmgAPScaling = 0f;   // fraction of abilityPower added

    // Fixed values (not upgradeable via primaries)
    [Header("=== FIXED VALUES ===")]
    public float jumpForce = 12f;   // constant, not shown to player
    public float wallSlideSpeed = -3f; // constant

    // ================================================================
    //  RECALCULATE
    // ================================================================

    /// <summary>
    /// Recomputes all derived stats from current primary stats.
    /// Call this whenever any primary stat changes.
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
        critChance = Mathf.Min(maxCritChance, baseCritChance + critChancePerLuck * luck);
        critMultiplier = baseCritMultiplier + critMultPerAttack * attackDamage;
        knockbackForce = Mathf.Min(maxKnockback, baseKnockback + knockbackPerAttack * attackDamage);

        // Economy
        maxEnergy = baseMaxEnergy + energyPerAbilityPower * abilityPower;
        dashCost = Mathf.Max(minDashCost, baseDashCost + dashCostPerPsyche * psyche);
        dashInvincibilityWindow = baseDashInvinc + dashInvincPerLuck * luck;
        hoverDrainRate = Mathf.Max(minHoverDrain, baseHoverDrain + hoverDrainPerPsyche * psyche);
        arrowChargeDrainRate = Mathf.Max(minChargeDrain, baseChargeDrain + chargeDrainPerPsyche * psyche);
        lootRange = Mathf.Min(maxLootRange, baseLootRange + lootRangePerLuck * luck);
        luckRoll = baseLuckRoll + luckRollPerLuck * luck;

        // Status effects
        burnStrength = baseBurnStrength + burnPerAbilityPower * abilityPower;
        freezeStrength = baseFreezeStrength + freezePerAbilityPower * abilityPower;
        holyStrength = baseHolyStrength + holyPerAbilityPower * abilityPower;
        shockStrength = baseShockStrength + shockPerAbilityPower * abilityPower;

        // Health
        maxHP = Mathf.RoundToInt(baseMaxHP + hpPerHealth * health_stat);
    }

    // ================================================================
    //  PRIMARY STAT MUTATION  (always call these, never set primaries directly)
    // ================================================================

    public void AddPrimary(PrimaryStat stat, float amount)
    {
        switch (stat)
        {
            case PrimaryStat.Agility: agility += amount; break;
            case PrimaryStat.AttackDamage: attackDamage += amount; break;
            case PrimaryStat.AbilityPower: abilityPower += amount; break;
            case PrimaryStat.Luck: luck += amount; break;
            case PrimaryStat.Psyche: psyche += amount; break;
            case PrimaryStat.Health: health_stat += amount; break;
            case PrimaryStat.Size: size += amount; break;
            case PrimaryStat.Cooldown: cooldown += amount; break;
        }
        RecalculateDerived();
        // Sync HP ceiling if Health changed
        if (stat == PrimaryStat.Health && health != null)
            health.IncreaseMaxHP(0); // triggers slider update with new maxHP
    }

    public float GetPrimary(PrimaryStat stat) => stat switch
    {
        PrimaryStat.Agility => agility,
        PrimaryStat.AttackDamage => attackDamage,
        PrimaryStat.AbilityPower => abilityPower,
        PrimaryStat.Luck => luck,
        PrimaryStat.Psyche => psyche,
        PrimaryStat.Health => health_stat,
        PrimaryStat.Size => size,
        PrimaryStat.Cooldown => cooldown,
        _ => 0f
    };

    // ================================================================
    //  AMMO  (Inspector-editable, synced to PlayerShooting)
    // ================================================================

    [Header("--- AMMO ---")]
    [SerializeField] private int _maxArrows = 3;
    [SerializeField] private int _currentArrows = 3;
    private int _lastReadArrows = -1;
    private int _lastReadHP = -1;

    public int MaxArrows
    {
        get => _maxArrows;
        set { _maxArrows = value; if (shooting != null) shooting.maxArrows = value; }
    }

    public int CurrentArrows
    {
        get => _currentArrows;
        set { _currentArrows = value; if (shooting != null) shooting.SetCurrentArrows(value); }
    }

    // ── HP sync backing field ──
    [Header("--- HP ---")]
    [SerializeField] private int _currentHP = 100;

    public int CurrentHP
    {
        get => _currentHP;
        set { _currentHP = value; if (health != null) health.SetHP(value); }
    }

    private void Update()
    {
        // Arrow sync
        if (shooting != null)
        {
            int live = shooting.CurrentArrows;
            if (_currentArrows != _lastReadArrows)
            {
                shooting.SetCurrentArrows(_currentArrows);
                _lastReadArrows = shooting.CurrentArrows;
                _currentArrows = _lastReadArrows;
            }
            else
            {
                _currentArrows = live;
                _lastReadArrows = live;
            }
            if (shooting.maxArrows != _maxArrows) shooting.maxArrows = _maxArrows;
            else _maxArrows = shooting.maxArrows;
        }

        // HP sync
        if (health != null)
        {
            int live = health.CurrentHP;
            if (_currentHP != _lastReadHP)
            {
                health.SetHP(_currentHP);
                _lastReadHP = health.CurrentHP;
                _currentHP = _lastReadHP;
            }
            else
            {
                _currentHP = live;
                _lastReadHP = live;
            }
        }
    }

    // ================================================================
    //  CRIT SYSTEM
    // ================================================================

    public (float damage, bool isCrit) RollDamage(float baseDamage)
    {
        bool isCrit = Random.value < critChance;
        float finalDmg = isCrit ? baseDamage * critMultiplier : baseDamage;
        if (isCrit) { critsLanded++; upgradeManager?.CriticalHit(null, finalDmg); }
        return (finalDmg, isCrit);
    }

    public (float damage, bool isCrit) RollDamage(float baseDamage, GameObject enemy)
    {
        bool isCrit = Random.value < critChance;
        float finalDmg = isCrit ? baseDamage * critMultiplier : baseDamage;
        if (isCrit) { critsLanded++; upgradeManager?.CriticalHit(enemy, finalDmg); }
        return (finalDmg, isCrit);
    }

    // ================================================================
    //  RUN HISTORY
    // ================================================================

    [Header("--- RUN HISTORY: Movement ---")]
    public float distanceMovedLeft;
    public float distanceMovedRight;
    public float totalDistance;
    public int jumpsPerformed;
    public float timeAirborne;
    public float timeGrounded;
    public int wallSlideCount;
    public float totalWallSlideDuration;
    public int hoverCount;
    public float totalHoverDuration;
    public float energySpentHovering;
    public int dashCount;
    public float totalDashDistance;
    public float energySpentDashing;
    public int dashesHitEnemy;
    public int dashesHitWall;
    public int slashCount;
    public int slashesHitEnemy;

    [Header("--- RUN HISTORY: Combat ---")]
    public int weakArrowsFired;
    public int mediumArrowsFired;
    public int chargedArrowsFired;
    public int extraArrowsFired;
    public int totalArrowsFired;
    public int arrowsHitEnemy;
    public int arrowsHitWall;
    public int arrowsHitDestructible;
    public int arrowsPickedUp;
    public int chargesCancelledByEnergy;
    public int enemiesKilled;
    public float totalDamageDealt;
    public float damageByArrow;
    public float damageByDash;
    public float damageByStatus;
    public float damageByExplosion;
    public float damageBySlash;
    public int critsLanded;
    public int burnApplied;
    public int freezeApplied;
    public int holyApplied;
    public int shockApplied;

    [Header("--- RUN HISTORY: Resources ---")]
    public int soulsCollected;
    public float xpGained;
    public float energyGainedTotal;
    public float energyFromKills;
    public float energyFromFalling;
    public float energyFromLava;
    public float energyFromWallSlide;
    public float energySpentCharging;

    [Header("--- RUN HISTORY: Survival ---")]
    public float damageTaken;
    public int timesHit;
    public float hpRestored;
    public int layersCompleted;

    // ── Record helpers ──

    public void RecordJump() => jumpsPerformed++;
    public void RecordWallSlideStart() => wallSlideCount++;
    public void RecordWallSlideTick(float dt) => totalWallSlideDuration += dt;
    public void RecordHoverStart() => hoverCount++;
    public void RecordHoverTick(float dt) => totalHoverDuration += dt;
    public void RecordDash(float dist) { dashCount++; totalDashDistance += dist; }
    public void RecordDashHitEnemy() => dashesHitEnemy++;
    public void RecordDashHitWall() => dashesHitWall++;
    public void RecordSlashUsed() => slashCount++;
    public void RecordSlashHitEnemy() => slashesHitEnemy++;
    public void RecordArrowHitEnemy() => arrowsHitEnemy++;
    public void RecordArrowHitWall() => arrowsHitWall++;
    public void RecordArrowHitDestructible() => arrowsHitDestructible++;
    public void RecordArrowPickedUp() => arrowsPickedUp++;
    public void RecordChargeCancelled() => chargesCancelledByEnergy++;
    public void RecordEnemyKilled() => enemiesKilled++;
    public void RecordCritLanded() => critsLanded++;
    public void RecordLayerCompleted() => layersCompleted++;
    public void RecordHPRestored(int amount) => hpRestored += amount;
    public void RecordDamageTaken(float amount) { damageTaken += amount; timesHit++; }
    public void RecordSoulCollected(int amount) { soulsCollected += amount; xpGained += amount; }

    public void RecordArrowFired(ArrowFireType type)
    {
        totalArrowsFired++;
        switch (type)
        {
            case ArrowFireType.Weak: weakArrowsFired++; break;
            case ArrowFireType.Medium: mediumArrowsFired++; break;
            case ArrowFireType.Charged: chargedArrowsFired++; break;
            case ArrowFireType.Extra: extraArrowsFired++; break;
        }
    }

    public void RecordMovement(float dx, float dy, float dt, bool grounded, bool airborne)
    {
        if (dx < 0) distanceMovedLeft += Mathf.Abs(dx); else distanceMovedRight += dx;
        totalDistance += new Vector2(dx, dy).magnitude;
        if (grounded) timeGrounded += dt;
        if (airborne) timeAirborne += dt;
    }

    public void RecordDamageDealt(float amount, DamageSource source)
    {
        totalDamageDealt += amount;
        switch (source)
        {
            case DamageSource.Arrow: damageByArrow += amount; break;
            case DamageSource.Dash: damageByDash += amount; break;
            case DamageSource.Status: damageByStatus += amount; break;
            case DamageSource.Explosion: damageByExplosion += amount; break;
            case DamageSource.Slash: damageBySlash += amount; break;
        }
        if (lifeSteal > 0f && health != null)
            health.RestoreHP(Mathf.Max(1, Mathf.RoundToInt(amount * lifeSteal)));
    }

    public void RecordStatusApplied(StatusType type)
    {
        switch (type)
        {
            case StatusType.Burn: burnApplied++; break;
            case StatusType.Freeze: freezeApplied++; break;
            case StatusType.Holy: holyApplied++; break;
            case StatusType.Shock: shockApplied++; break;
        }
    }

    public void RecordEnergyGained(float amount, EnergySource source)
    {
        energyGainedTotal += amount;
        switch (source)
        {
            case EnergySource.Kill: energyFromKills += amount; break;
            case EnergySource.Falling: energyFromFalling += amount; break;
            case EnergySource.Lava: energyFromLava += amount; break;
            case EnergySource.WallSlide: energyFromWallSlide += amount; break;
        }
    }

    public void RecordEnergySpent(float amount, EnergySpentSource source)
    {
        switch (source)
        {
            case EnergySpentSource.Dash: energySpentDashing += amount; break;
            case EnergySpentSource.Hover: energySpentHovering += amount; break;
            case EnergySpentSource.Charging: energySpentCharging += amount; break;
        }
    }

    public void RecordPlayerLevelUp(int newLevel) => currentLevel = newLevel;
}

// ================================================================
//  ENUMS
// ================================================================

public enum PrimaryStat
{
    Agility, AttackDamage, AbilityPower, Luck, Psyche, Health, Size, Cooldown
}

public enum DamageSource { Arrow, Dash, Status, Explosion, Slash }
public enum EnergySpentSource { Dash, Hover, Charging }
public enum ArrowFireType { Weak, Medium, Charged, Extra }