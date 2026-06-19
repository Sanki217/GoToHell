using UnityEngine;

/// <summary>
/// Converts a live PlayerStats into a serializable RunStats snapshot.
/// Called when a run ends (death or victory) before recording to SaveManager.
/// </summary>
public static class RunStatsBuilder
{
    public static RunStats Build(PlayerStats s, string playerName, int deepestLayer,
                                 float runDuration, bool victory)
    {
        RunStats r = new RunStats
        {
            playerName         = playerName,
            dateUtc            = System.DateTime.UtcNow.ToString("o"),
            deepestLayer       = deepestLayer,
            finalLevel         = s.currentLevel,
            runDurationSeconds = runDuration,
            victory            = victory,

            distanceMovedLeft      = s.distanceMovedLeft,
            distanceMovedRight     = s.distanceMovedRight,
            totalDistance          = s.totalDistance,
            jumpsPerformed         = s.jumpsPerformed,
            timeAirborne           = s.timeAirborne,
            timeGrounded           = s.timeGrounded,
            wallSlideCount         = s.wallSlideCount,
            totalWallSlideDuration = s.totalWallSlideDuration,
            hoverCount             = s.hoverCount,
            totalHoverDuration     = s.totalHoverDuration,
            energySpentHovering    = s.energySpentHovering,
            dashCount              = s.dashCount,
            totalDashDistance      = s.totalDashDistance,
            energySpentDashing     = s.energySpentDashing,
            dashesHitEnemy         = s.dashesHitEnemy,
            dashesHitWall          = s.dashesHitWall,
            slashCount             = s.slashCount,
            slashesHitEnemy        = s.slashesHitEnemy,

            weakArrowsFired        = s.weakArrowsFired,
            mediumArrowsFired      = s.mediumArrowsFired,
            chargedArrowsFired     = s.chargedArrowsFired,
            extraArrowsFired       = s.extraArrowsFired,
            totalArrowsFired       = s.totalArrowsFired,
            arrowsHitEnemy         = s.arrowsHitEnemy,
            arrowsHitWall          = s.arrowsHitWall,
            arrowsHitDestructible  = s.arrowsHitDestructible,
            arrowsPickedUp         = s.arrowsPickedUp,
            chargesCancelledByEnergy = s.chargesCancelledByEnergy,
            enemiesKilled          = s.enemiesKilled,
            totalDamageDealt       = s.totalDamageDealt,
            damageByArrow          = s.damageByArrow,
            damageByDash           = s.damageByDash,
            damageByStatus         = s.damageByStatus,
            damageByExplosion      = s.damageByExplosion,
            damageBySlash          = s.damageBySlash,
            critsLanded            = s.critsLanded,
            burnApplied            = s.burnApplied,
            freezeApplied          = s.freezeApplied,
            holyApplied            = s.holyApplied,
            shockApplied           = s.shockApplied,

            soulsCollected         = s.soulsCollected,
            xpGained               = s.xpGained,
            energyGainedTotal      = s.energyGainedTotal,
            energyFromKills        = s.energyFromKills,
            energyFromFalling      = s.energyFromFalling,
            energyFromLava         = s.energyFromLava,
            energyFromWallSlide    = s.energyFromWallSlide,
            energySpentCharging    = s.energySpentCharging,

            damageTaken            = s.damageTaken,
            timesHit               = s.timesHit,
            hpRestored             = s.hpRestored,
            layersCompleted        = s.layersCompleted
        };

        return r;
    }
}
