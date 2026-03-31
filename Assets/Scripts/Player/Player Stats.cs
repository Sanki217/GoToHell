using UnityEngine;

/// <summary>
/// Unified player stat system.
///   1. Combat Stats   — live values modified by upgrades
///   2. Movement Stats — live values modified by upgrades
///   3. Run History    — accumulated record of everything done this run
///
/// CRIT SYSTEM: call RollDamage(baseDamage) from every damage source.
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

        // Initialize sync cache to match the backing fields at startup
        _lastReadArrows = _currentArrows;
        _lastReadHP = _currentHP;
    }

    // ================================================================
    //  1. COMBAT STATS  (live — upgrades modify these)
    // ================================================================

    [Header("--- LEVEL ---")]
    [Tooltip("Current player level. Read-only — driven by PlayerLevelSystem.")]
    public int currentLevel = 1;

    [Header("--- COMBAT STATS ---")]
    public int maxHP = 100;
    public float arrowDamage = 1f;
    public float dashDamage = 1f;
    public float critChance = 0f;
    public float critMultiplier = 1.5f;
    public float knockbackForce = 0f;

    [Tooltip("Set by Arrow Pierce upgrade. 0 = no pierce, 1 = pass through 1 non-kill, etc.")]
    public int arrowPierceCount = 0;

    [Tooltip("Lifesteal: heal player for this % of damage dealt to enemies. " +
             "0 = disabled, 0.1 = 10%, 1.0 = 100%")]
    public float lifeSteal = 0f;

    [Header("Status Effect Strength (1.0 = base 100%)")]
    public float burnStrength = 1f;
    public float freezeStrength = 1f;
    public float holyStrength = 1f;
    public float shockStrength = 1f;

    // ── Arrow count ── Inspector-editable backing fields synced to PlayerShooting ──
    // Change these in the Inspector during play and they take effect immediately.

    [Header("--- AMMO (editable in play mode) ---")]
    [SerializeField] private int _maxArrows = 3;
    [SerializeField] private int _currentArrows = 3;

    public int MaxArrows
    {
        get => _maxArrows;
        set
        {
            _maxArrows = value;
            if (shooting != null) shooting.maxArrows = value;
        }
    }

    public int CurrentArrows
    {
        get => _currentArrows;
        set
        {
            _currentArrows = value;
            if (shooting != null) shooting.SetCurrentArrows(value);
        }
    }

    // ── HP ── Inspector-editable backing field synced to PlayerHealth ──

    [Header("--- HP (editable in play mode) ---")]
    [SerializeField] private int _currentHP = 100;

    public int CurrentHP
    {
        get => _currentHP;
        set
        {
            _currentHP = value;
            if (health != null) health.SetHP(value);
        }
    }

    // ================================================================
    //  2. MOVEMENT STATS  (live — upgrades modify these)
    // ================================================================

    [Header("--- MOVEMENT STATS ---")]
    public float moveSpeed = 10f;
    public float jumpForce = 12f;
    public int maxJumps = 2;
    public float dashDistance = 10f;
    public float dashCost = 20f;

    [Tooltip("How long after a dash starts the player is immune to damage. " +
             "0 = only immune during the dash itself.")]
    public float dashInvincibilityWindow = 0.2f;

    public float maxEnergy = 100f;
    public float energyRegenMultiplier = 1f;
    public float wallSlideSpeed = -3f;
    public float hoverDrainRate = 10f;
    public float arrowChargeDrainRate = 5f;

    [Tooltip("How long it takes to reach 100% charge (seconds). " +
             "Lower = faster charge. Default 1.5s.")]
    public float arrowChargeDuration = 1.5f;

    [Tooltip("Radius of the Looter sphere collider — controls pickup range for all items.")]
    public float lootRange = 4f;

    [Tooltip("Luck: each point shifts 2% weight from Common toward higher rarities in upgrade rolls.")]
    public float luck = 0f;

    // ================================================================
    //  SYNC — bidirectional but conflict-safe
    //  We track what value we last read from the game system.
    //  If the backing field differs from our last-read value, the USER
    //  changed it in the Inspector → push to the game system.
    //  Otherwise, the game changed it internally → read it back.
    // ================================================================

    private int _lastReadArrows = -1;
    private int _lastReadHP = -1;

    private void Update()
    {
        // ── Arrow sync ──
        if (shooting != null)
        {
            int liveArrows = shooting.CurrentArrows;

            if (_currentArrows != _lastReadArrows)
            {
                // Inspector value changed by user — push to PlayerShooting
                shooting.SetCurrentArrows(_currentArrows);
                _lastReadArrows = shooting.CurrentArrows;
                _currentArrows = _lastReadArrows;
            }
            else
            {
                // Game changed it (shot fired, arrow picked up) — read back
                _currentArrows = liveArrows;
                _lastReadArrows = liveArrows;
            }

            // Max arrows sync (one direction: Inspector → PlayerShooting)
            if (shooting.maxArrows != _maxArrows)
                shooting.maxArrows = _maxArrows;
            else
                _maxArrows = shooting.maxArrows;
        }

        // ── HP sync ──
        if (health != null)
        {
            int liveHP = health.CurrentHP;

            if (_currentHP != _lastReadHP)
            {
                // Inspector value changed by user — push to PlayerHealth
                health.SetHP(_currentHP);
                _lastReadHP = health.CurrentHP;
                _currentHP = _lastReadHP;
            }
            else
            {
                // Game changed it (damage taken, heal) — read back
                _currentHP = liveHP;
                _lastReadHP = liveHP;
            }
        }
    }

    // ================================================================
    //  3. RUN HISTORY  (accumulated — reset each run)
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
    public int critsLanded;
    public int burnApplied;
    public int freezeApplied;
    public int holyApplied;
    public int shockApplied;
    public int mythicUpgradesTriggered;
    public int cursedUpgradesTaken;

    [Header("--- RUN HISTORY: Resources ---")]
    public int soulsCollected;
    public float xpGained;
    public float energyGainedTotal;
    public float energyFromKills;
    public float energyFromFalling;
    public float energyFromLava;
    public float energyFromWallSlide;
    public float energySpentCharging;
    public int chestsOpenedCommon;
    public int chestsOpenedRare;
    public int chestsOpenedLegendary;
    public int merchantPurchases;

    [Header("--- RUN HISTORY: Survival ---")]
    public float damageTaken;
    public int timesHit;
    public float hpRestored;
    public int layersCompleted;
    public bool reviveUsed;

    // ================================================================
    //  CRIT SYSTEM
    // ================================================================

    public (float damage, bool isCrit) RollDamage(float baseDamage)
    {
        bool isCrit = Random.value < critChance;
        float finalDamage = isCrit ? baseDamage * critMultiplier : baseDamage;
        if (isCrit) { critsLanded++; upgradeManager?.CriticalHit(null, finalDamage); }
        return (finalDamage, isCrit);
    }

    public (float damage, bool isCrit) RollDamage(float baseDamage, GameObject enemy)
    {
        bool isCrit = Random.value < critChance;
        float finalDamage = isCrit ? baseDamage * critMultiplier : baseDamage;
        if (isCrit) { critsLanded++; upgradeManager?.CriticalHit(enemy, finalDamage); }
        return (finalDamage, isCrit);
    }

    // ================================================================
    //  RECORD METHODS
    // ================================================================

    public void RecordJump() => jumpsPerformed++;
    public void RecordWallSlideStart() => wallSlideCount++;
    public void RecordWallSlideTick(float dt) => totalWallSlideDuration += dt;
    public void RecordHoverStart() => hoverCount++;
    public void RecordHoverTick(float dt) => totalHoverDuration += dt;

    public void RecordDash(float distance) { dashCount++; totalDashDistance += distance; }
    public void RecordDashHitEnemy() => dashesHitEnemy++;
    public void RecordDashHitWall() => dashesHitWall++;

    public void RecordMovement(float dx, float dy, float dt, bool grounded, bool airborne)
    {
        if (dx < 0) distanceMovedLeft += Mathf.Abs(dx);
        else distanceMovedRight += dx;
        totalDistance += new Vector2(dx, dy).magnitude;
        if (grounded) timeGrounded += dt;
        if (airborne) timeAirborne += dt;
    }

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

    public void RecordArrowHitEnemy() => arrowsHitEnemy++;
    public void RecordArrowHitWall() => arrowsHitWall++;
    public void RecordArrowHitDestructible() => arrowsHitDestructible++;
    public void RecordArrowPickedUp() => arrowsPickedUp++;
    public void RecordChargeCancelled() => chargesCancelledByEnergy++;

    public void RecordDamageDealt(float amount, DamageSource source)
    {
        totalDamageDealt += amount;
        switch (source)
        {
            case DamageSource.Arrow: damageByArrow += amount; break;
            case DamageSource.Dash: damageByDash += amount; break;
            case DamageSource.Status: damageByStatus += amount; break;
            case DamageSource.Explosion: damageByExplosion += amount; break;
        }

        // Lifesteal: heal player for lifeSteal% of damage dealt
        if (lifeSteal > 0f && health != null)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(amount * lifeSteal));
            health.RestoreHP(healAmount);
        }
    }

    public void RecordPlayerLevelUp(int newLevel) { currentLevel = newLevel; }
    public void RecordEnemyKilled() => enemiesKilled++;
    public void RecordCritLanded() => critsLanded++;

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

    public void RecordSoulCollected(int amount) { soulsCollected += amount; xpGained += amount; }

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

    public void RecordChestOpened(ChestRarity rarity)
    {
        switch (rarity)
        {
            case ChestRarity.Common: chestsOpenedCommon++; break;
            case ChestRarity.Rare: chestsOpenedRare++; break;
            case ChestRarity.Legendary: chestsOpenedLegendary++; break;
        }
    }

    public void RecordDamageTaken(float amount) { damageTaken += amount; timesHit++; }
    public void RecordHPRestored(int amount) => hpRestored += amount;
    public void RecordLayerCompleted() => layersCompleted++;
    public void RecordReviveUsed() => reviveUsed = true;
}

// ================================================================
//  SUPPORTING ENUMS
// ================================================================

public enum DamageSource { Arrow, Dash, Status, Explosion }
public enum EnergySpentSource { Dash, Hover, Charging }
public enum ArrowFireType { Weak, Medium, Charged, Extra }