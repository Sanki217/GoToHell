using UnityEngine;

/// <summary>
/// Unified player stat system. Three categories:
///   1. Combat Stats   — live values modified by upgrades (damage, crit, HP, arrows)
///   2. Movement Stats — live values modified by upgrades (speed, dash, energy)
///   3. Run History    — accumulated read-only record of everything done this run
///
/// Add this component to the Player GameObject.
/// Upgrades write to Combat/Movement stats. Other scripts call Record* methods.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ================================================================
    //  REFERENCES
    // ================================================================

    private PlayerShooting shooting;
    private PlayerHealth health;

    private void Awake()
    {
        shooting = GetComponent<PlayerShooting>();
        health = GetComponent<PlayerHealth>();
    }

    // ================================================================
    //  1. COMBAT STATS  (live — upgrades modify these)
    // ================================================================

    [Header("--- COMBAT STATS ---")]
    public int maxHP = 100;
    public float arrowDamage = 1f;
    public float dashDamage = 1f;
    public float critChance = 0f;    // 0.0 = 0%,  1.0 = 100%
    public float critMultiplier = 1.5f;  // 1.5 = 150%
    public float knockbackForce = 0f;    // 0 = knockback disabled

    [Header("Status Effect Strength (1.0 = base 100%)")]
    public float burnStrength = 1f;
    public float freezeStrength = 1f;
    public float holyStrength = 1f;
    public float shockStrength = 1f;

    // Arrow count — live references into PlayerShooting
    public int MaxArrows
    {
        get => shooting != null ? shooting.maxArrows : 3;
        set { if (shooting != null) shooting.maxArrows = value; }
    }

    public int CurrentArrows
        => shooting != null ? shooting.CurrentArrows : 0;

    // HP — live reference into PlayerHealth
    public int CurrentHP
        => health != null ? health.CurrentHP : 0;

    // ================================================================
    //  2. MOVEMENT STATS  (live — upgrades modify these)
    // ================================================================

    [Header("--- MOVEMENT STATS ---")]
    public float moveSpeed = 10f;
    public float jumpForce = 12f;
    public int maxJumps = 2;
    public float dashDistance = 10f;
    public float dashCost = 20f;
    public float maxEnergy = 100f;
    public float energyRegenMultiplier = 1f;
    public float wallSlideSpeed = -3f;
    public float hoverDrainRate = 10f;
    public float arrowChargeDrainRate = 5f;

    // ================================================================
    //  3. RUN HISTORY  (accumulated — reset each run)
    // ================================================================

    [Header("--- RUN HISTORY: Movement ---")]
    public float distanceMovedLeft;
    public float distanceMovedRight;
    public float totalDistance;
    public int jumpsPerformed;
    public int doubleJumpsPerformed;
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
    //  RECORD METHODS — called by other systems to update Run History
    // ================================================================

    public void RecordSoulCollected(int amount)
    {
        soulsCollected += amount;
        xpGained += amount;
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

    public void RecordDamageTaken(float amount)
    {
        damageTaken += amount;
        timesHit++;
    }

    public void RecordHPRestored(int amount)
    {
        hpRestored += amount;
    }

    public void RecordEnemyKilled()
    {
        enemiesKilled++;
    }

    // ================================================================
    //  STAT SHEET — formatted string for UI / pause menu
    // ================================================================

    public string GetStatSheet()
    {
        return
            "=== COMBAT ===\n" +
            $"HP:               {CurrentHP} / {maxHP}\n" +
            $"Arrow Damage:     {arrowDamage:F1}\n" +
            $"Dash Damage:      {dashDamage:F1}\n" +
            $"Crit Chance:      {critChance * 100f:F1}%\n" +
            $"Crit Multiplier:  {critMultiplier * 100f:F0}%\n" +
            $"Knockback Force:  {knockbackForce:F1}\n" +
            $"Burn Strength:    {burnStrength * 100f:F0}%\n" +
            $"Freeze Strength:  {freezeStrength * 100f:F0}%\n" +
            $"Holy Strength:    {holyStrength * 100f:F0}%\n" +
            $"Shock Strength:   {shockStrength * 100f:F0}%\n" +
            "\n=== MOVEMENT ===\n" +
            $"Move Speed:       {moveSpeed:F1}\n" +
            $"Jump Force:       {jumpForce:F1}\n" +
            $"Max Jumps:        {maxJumps}\n" +
            $"Dash Distance:    {dashDistance:F1}\n" +
            $"Dash Cost:        {dashCost:F1}\n" +
            $"Max Energy:       {maxEnergy:F1}\n" +
            $"Hover Drain/s:    {hoverDrainRate:F1}\n" +
            $"Charge Drain/s:   {arrowChargeDrainRate:F1}\n" +
            "\n=== AMMO ===\n" +
            $"Arrows:           {CurrentArrows} / {MaxArrows}\n";
    }
}

// ================================================================
//  SUPPORTING ENUM
// ================================================================

public enum DamageSource
{
    Arrow,
    Dash,
    Status,
    Explosion
}