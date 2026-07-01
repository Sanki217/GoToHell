using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static achievement engine. Works in any scene.
/// Loads all AchievementDefinition assets from Resources/Achievements/.
///
/// - Event achievements:      TriggerEvent("id")
/// - SingleRun achievements:  CheckSingleRun(liveStats)  (polled by AchievementTracker)
/// - Cumulative achievements: CheckCumulative()          (called after a run is recorded)
///
/// When an achievement completes it: marks it in SaveManager, unlocks its pact,
/// adds it to EarnedThisRun (for the finish screen), and fires OnUnlocked (for popups).
/// </summary>
public static class Achievements
{
    /// <summary>Fired when an achievement is newly completed. AchievementPopupUI listens.</summary>
    public static event Action<AchievementDefinition> OnUnlocked;

    /// <summary>Achievements earned during the current run (for the finish screen).</summary>
    public static readonly List<AchievementDefinition> EarnedThisRun = new List<AchievementDefinition>();

    private static List<AchievementDefinition> all;

    private static void EnsureLoaded()
    {
        if (all != null) return;
        all = new List<AchievementDefinition>(
            Resources.LoadAll<AchievementDefinition>("Achievements"));
    }

    /// <summary>Clear the per-run earned list. Call when a new run starts.</summary>
    public static void ResetRun() => EarnedThisRun.Clear();

    // ================================================================
    //  CHECKS
    // ================================================================

    public static void TriggerEvent(string eventId)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(eventId)) return;

        foreach (var a in all)
        {
            if (a == null || a.type != AchievementType.Event) continue;
            if (a.eventId != eventId) continue;
            if (SaveManager.IsAchievementComplete(a.id)) continue;
            Complete(a);
        }
    }

    public static void CheckSingleRun(PlayerStats stats)
    {
        EnsureLoaded();
        if (stats == null) return;

        foreach (var a in all)
        {
            if (a == null || a.type != AchievementType.SingleRun) continue;
            if (SaveManager.IsAchievementComplete(a.id)) continue;
            if (GetLiveStat(stats, a.statField) >= a.threshold) Complete(a);
        }
    }

    public static void CheckCumulative()
    {
        EnsureLoaded();
        RunStats lifetime = SaveManager.Data.lifetimeTotals;

        foreach (var a in all)
        {
            if (a == null || a.type != AchievementType.Cumulative) continue;
            if (SaveManager.IsAchievementComplete(a.id)) continue;
            if (GetCumulativeStat(lifetime, a.statField) >= a.threshold) Complete(a);
        }
    }

    // ================================================================
    //  COMPLETE
    // ================================================================

    private static void Complete(AchievementDefinition a)
    {
        SaveManager.CompleteAchievement(a.id);
        if (a.pactToUnlock != null)
            SaveManager.UnlockPact(a.pactToUnlock.pactId);

        EarnedThisRun.Add(a);
        OnUnlocked?.Invoke(a);
    }

    // ================================================================
    //  STAT READERS
    // ================================================================

    private static float GetLiveStat(PlayerStats s, StatField f)
    {
        switch (f)
        {
            case StatField.EnemiesKilled:    return s.enemiesKilled;
            case StatField.TotalDistance:    return s.totalDistance;
            case StatField.SoulsCollected:   return s.soulsCollected;
            case StatField.TotalDamageDealt: return s.totalDamageDealt;
            case StatField.CritsLanded:      return s.critsLanded;
            case StatField.TotalArrowsFired: return s.totalArrowsFired;
            case StatField.ArrowsHitEnemy:   return s.arrowsHitEnemy;
            case StatField.DashesHitEnemy:   return s.dashesHitEnemy;
            case StatField.SlashesHitEnemy:  return s.slashesHitEnemy;
            case StatField.JumpsPerformed:   return s.jumpsPerformed;
            case StatField.WallSlideCount:   return s.wallSlideCount;
            case StatField.DashCount:        return s.dashCount;
            case StatField.HoverCount:       return s.hoverCount;
            case StatField.BurnApplied:      return s.burnApplied;
            case StatField.FreezeApplied:    return s.freezeApplied;
            case StatField.HolyApplied:      return s.holyApplied;
            case StatField.ShockApplied:     return s.shockApplied;
            case StatField.DamageTaken:      return s.damageTaken;
            case StatField.XpGained:         return s.xpGained;
            case StatField.DeepestLayer:     return RunConfig.I != null ? RunConfig.I.currentLayer : 1;
            case StatField.FinalLevel:       return s.currentLevel;
            default: return 0f;
        }
    }

    private static float GetCumulativeStat(RunStats t, StatField f)
    {
        switch (f)
        {
            case StatField.EnemiesKilled:    return t.enemiesKilled;
            case StatField.TotalDistance:    return t.totalDistance;
            case StatField.SoulsCollected:   return t.soulsCollected;
            case StatField.TotalDamageDealt: return t.totalDamageDealt;
            case StatField.CritsLanded:      return t.critsLanded;
            case StatField.TotalArrowsFired: return t.totalArrowsFired;
            case StatField.ArrowsHitEnemy:   return t.arrowsHitEnemy;
            case StatField.DashesHitEnemy:   return t.dashesHitEnemy;
            case StatField.SlashesHitEnemy:  return t.slashesHitEnemy;
            case StatField.JumpsPerformed:   return t.jumpsPerformed;
            case StatField.WallSlideCount:   return t.wallSlideCount;
            case StatField.DashCount:        return t.dashCount;
            case StatField.HoverCount:       return t.hoverCount;
            case StatField.BurnApplied:      return t.burnApplied;
            case StatField.FreezeApplied:    return t.freezeApplied;
            case StatField.HolyApplied:      return t.holyApplied;
            case StatField.ShockApplied:     return t.shockApplied;
            case StatField.DamageTaken:      return t.damageTaken;
            case StatField.XpGained:         return t.xpGained;
            case StatField.DeepestLayer:     return t.deepestLayer;
            case StatField.FinalLevel:       return t.finalLevel;
            default: return 0f;
        }
    }
}