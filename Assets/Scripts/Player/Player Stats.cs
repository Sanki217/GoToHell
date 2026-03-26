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
    }

    // ================================================================
    //  1. COMBAT STATS  (live — upgrades modify these)
    // ================================================================

    [Header("--- COMBAT STATS ---")]
    public int maxHP = 100;
    public float arrowDamage = 1f;
    public float dashDamage = 1f;
    public float critChance = 0f;
    public float critMultiplier = 1.5f;
    public float knockbackForce = 0f;

    [Header("Status Effect Strength (1.0 = base 100%)")]
    public float burnStrength = 1f;
    public float freezeStrength = 1f;
    public float holyStrength = 1f;
    public float shockStrength = 1f;

    // ── Arrow count ── live references into PlayerShooting, Inspector-editable
    public int MaxArrows
    {
        get => shooting != null ? shooting.maxArrows : 3;
        set { if (shooting != null) shooting.maxArrows = value; }
    }

    public int CurrentArrows
    {
        get => shooting != null ? shooting.CurrentArrows : 0;
        set { if (shooting != null) shooting.SetCurrentArrows(value); }
    }

    // ── HP ── live references into PlayerHealth, Inspector-editable
    public int CurrentHP
    {
        get => health != null ? health.CurrentHP : 0;
        set { if (health != null) health.SetHP(value); }
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
             "0 = only immune during the dash itself. " +
             "Set higher to give a post-dash grace window.")]
    public float dashInvincibilityWindow = 0.2f;

    public float maxEnergy = 100f;
    public float energyRegenMultiplier = 1f;
    public float wallSlideSpeed = -3f;
    public float hoverDrainRate = 10f;
    public float arrowChargeDrainRate = 5f;

    [Tooltip("Radius of the Looter sphere collider — controls pickup range for all items. " +
             "Upgrades increase this value to improve loot range.")]
    public float lootRange = 4f;

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
    }

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