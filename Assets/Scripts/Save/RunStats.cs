using System;

/// <summary>
/// Serializable snapshot of a single run's statistics.
/// Mirrors the RUN HISTORY fields in PlayerStats.
/// Captured on death and stored in SaveData.runHistory.
/// </summary>
[Serializable]
public class RunStats
{
    // Meta
    public string playerName;
    public string dateUtc;          // when the run ended
    public string classId;          // selected class (leaderboard display)
    public string weaponId;         // selected weapon (leaderboard display)
    public int    deepestLayer;     // highest layer reached
    public int    finalLevel;       // player level at death
    public float  runDurationSeconds;
    public bool   victory;          // reached the end vs died

    // Max depth reached (world units below the layer start, summed across
    // layers). Tracked as lowest-Y-ever by DepthTracker — bouncing back up
    // and falling again never double-counts.
    public float maxDepthReached;

    // Movement
    public float distanceMovedLeft;
    public float distanceMovedRight;
    public float totalDistance;
    public int   jumpsPerformed;
    public float timeAirborne;
    public float timeGrounded;
    public int   wallSlideCount;
    public float totalWallSlideDuration;
    public int   hoverCount;
    public float totalHoverDuration;
    public float energySpentHovering;
    public int   dashCount;
    public float totalDashDistance;
    public float energySpentDashing;
    public int   dashesHitEnemy;
    public int   dashesHitWall;
    public int   slashCount;
    public int   slashesHitEnemy;

    // Combat
    public int   weakArrowsFired;
    public int   mediumArrowsFired;
    public int   chargedArrowsFired;
    public int   extraArrowsFired;
    public int   totalArrowsFired;
    public int   arrowsHitEnemy;
    public int   arrowsHitWall;
    public int   arrowsHitDestructible;
    public int   arrowsPickedUp;
    public int   chargesCancelledByEnergy;
    public int   enemiesKilled;
    public float totalDamageDealt;
    public float damageByArrow;
    public float damageByDash;
    public float damageByStatus;
    public float damageByExplosion;
    public float damageBySlash;
    public int   critsLanded;
    public int   burnApplied;
    public int   freezeApplied;
    public int   holyApplied;
    public int   shockApplied;

    // Resources
    public int   soulsCollected;
    public float xpGained;
    public float energyGainedTotal;
    public float energyFromKills;
    public float energyFromFalling;
    public float energyFromLava;
    public float energyFromWallSlide;
    public float energySpentCharging;

    // Survival
    public float damageTaken;
    public int   timesHit;
    public float hpRestored;
    public int   layersCompleted;
}
