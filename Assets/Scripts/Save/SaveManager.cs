using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads/saves SaveData to disk as JSON. Static access from anywhere.
/// Auto-loads on first access. Call Save() after any change.
///
/// File location: Application.persistentDataPath/save.json
/// </summary>
public static class SaveManager
{
    private static SaveData _data;
    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Data
    {
        get
        {
            if (_data == null) Load();
            return _data;
        }
    }

    // ================================================================
    //  LOAD / SAVE
    // ================================================================

    public static void Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                _data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch
            {
                Debug.LogWarning("[SaveManager] Save file corrupt — starting fresh.");
                _data = new SaveData();
            }
        }
        else
        {
            _data = new SaveData();
        }
    }

    public static void Save()
    {
        if (_data == null) return;
        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        _data = new SaveData();
    }

    // ================================================================
    //  PACTS
    // ================================================================

    public static bool IsPactUnlocked(string pactId) =>
        Data.unlockedPactIds.Contains(pactId);

    public static void UnlockPact(string pactId)
    {
        if (string.IsNullOrEmpty(pactId) || Data.unlockedPactIds.Contains(pactId)) return;
        Data.unlockedPactIds.Add(pactId);
        Save();
    }

    // ================================================================
    //  ACHIEVEMENTS
    // ================================================================

    public static bool IsAchievementComplete(string achievementId) =>
        Data.completedAchievementIds.Contains(achievementId);

    public static void CompleteAchievement(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId) || Data.completedAchievementIds.Contains(achievementId)) return;
        Data.completedAchievementIds.Add(achievementId);
        Save();
    }

    // ================================================================
    //  RUN HISTORY
    // ================================================================

    /// <summary>Record a completed run: append to history, update best + lifetime totals.</summary>
    public static void RecordRun(RunStats run)
    {
        if (run == null) return;

        Data.runHistory.Add(run);
        UpdateBest(run);
        AddToLifetime(run);
        Save();
    }

    private static void UpdateBest(RunStats r)
    {
        RunStats b = Data.bestRun;

        b.deepestLayer       = Mathf.Max(b.deepestLayer, r.deepestLayer);
        b.finalLevel         = Mathf.Max(b.finalLevel, r.finalLevel);
        b.runDurationSeconds = Mathf.Max(b.runDurationSeconds, r.runDurationSeconds);

        b.totalDistance      = Mathf.Max(b.totalDistance, r.totalDistance);
        b.jumpsPerformed     = Mathf.Max(b.jumpsPerformed, r.jumpsPerformed);
        b.dashCount          = Mathf.Max(b.dashCount, r.dashCount);
        b.slashCount         = Mathf.Max(b.slashCount, r.slashCount);
        b.wallSlideCount     = Mathf.Max(b.wallSlideCount, r.wallSlideCount);
        b.hoverCount         = Mathf.Max(b.hoverCount, r.hoverCount);

        b.totalArrowsFired   = Mathf.Max(b.totalArrowsFired, r.totalArrowsFired);
        b.arrowsHitEnemy     = Mathf.Max(b.arrowsHitEnemy, r.arrowsHitEnemy);
        b.enemiesKilled      = Mathf.Max(b.enemiesKilled, r.enemiesKilled);
        b.totalDamageDealt   = Mathf.Max(b.totalDamageDealt, r.totalDamageDealt);
        b.critsLanded        = Mathf.Max(b.critsLanded, r.critsLanded);
        b.burnApplied        = Mathf.Max(b.burnApplied, r.burnApplied);
        b.freezeApplied      = Mathf.Max(b.freezeApplied, r.freezeApplied);
        b.holyApplied        = Mathf.Max(b.holyApplied, r.holyApplied);
        b.shockApplied       = Mathf.Max(b.shockApplied, r.shockApplied);

        b.soulsCollected     = Mathf.Max(b.soulsCollected, r.soulsCollected);
        b.xpGained           = Mathf.Max(b.xpGained, r.xpGained);
        b.energyGainedTotal  = Mathf.Max(b.energyGainedTotal, r.energyGainedTotal);

        b.damageTaken        = Mathf.Max(b.damageTaken, r.damageTaken);
        b.timesHit           = Mathf.Max(b.timesHit, r.timesHit);
        b.hpRestored         = Mathf.Max(b.hpRestored, r.hpRestored);
        b.layersCompleted    = Mathf.Max(b.layersCompleted, r.layersCompleted);
    }

    private static void AddToLifetime(RunStats r)
    {
        RunStats t = Data.lifetimeTotals;

        t.totalDistance        += r.totalDistance;
        t.distanceMovedLeft    += r.distanceMovedLeft;
        t.distanceMovedRight   += r.distanceMovedRight;
        t.jumpsPerformed       += r.jumpsPerformed;
        t.timeAirborne         += r.timeAirborne;
        t.timeGrounded         += r.timeGrounded;
        t.wallSlideCount       += r.wallSlideCount;
        t.totalWallSlideDuration += r.totalWallSlideDuration;
        t.hoverCount           += r.hoverCount;
        t.totalHoverDuration   += r.totalHoverDuration;
        t.dashCount            += r.dashCount;
        t.totalDashDistance    += r.totalDashDistance;
        t.dashesHitEnemy       += r.dashesHitEnemy;
        t.dashesHitWall        += r.dashesHitWall;
        t.slashCount           += r.slashCount;
        t.slashesHitEnemy      += r.slashesHitEnemy;

        t.totalArrowsFired     += r.totalArrowsFired;
        t.weakArrowsFired      += r.weakArrowsFired;
        t.mediumArrowsFired    += r.mediumArrowsFired;
        t.chargedArrowsFired   += r.chargedArrowsFired;
        t.extraArrowsFired     += r.extraArrowsFired;
        t.arrowsHitEnemy       += r.arrowsHitEnemy;
        t.arrowsHitWall        += r.arrowsHitWall;
        t.arrowsHitDestructible += r.arrowsHitDestructible;
        t.arrowsPickedUp       += r.arrowsPickedUp;
        t.enemiesKilled        += r.enemiesKilled;
        t.totalDamageDealt     += r.totalDamageDealt;
        t.damageByArrow        += r.damageByArrow;
        t.damageByDash         += r.damageByDash;
        t.damageByStatus       += r.damageByStatus;
        t.damageByExplosion    += r.damageByExplosion;
        t.damageBySlash        += r.damageBySlash;
        t.critsLanded          += r.critsLanded;
        t.burnApplied          += r.burnApplied;
        t.freezeApplied        += r.freezeApplied;
        t.holyApplied          += r.holyApplied;
        t.shockApplied         += r.shockApplied;

        t.soulsCollected       += r.soulsCollected;
        t.xpGained             += r.xpGained;
        t.energyGainedTotal    += r.energyGainedTotal;
        t.energyFromKills       += r.energyFromKills;
        t.energyFromFalling     += r.energyFromFalling;
        t.energyFromLava        += r.energyFromLava;
        t.energyFromWallSlide   += r.energyFromWallSlide;
        t.energySpentCharging   += r.energySpentCharging;
        t.energySpentDashing    += r.energySpentDashing;
        t.energySpentHovering   += r.energySpentHovering;

        t.damageTaken          += r.damageTaken;
        t.timesHit             += r.timesHit;
        t.hpRestored           += r.hpRestored;
        t.layersCompleted      += r.layersCompleted;
    }
}
