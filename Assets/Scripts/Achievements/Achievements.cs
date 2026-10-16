using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static achievement engine. Works in any scene.
/// Loads all AchievementDefinition assets from Resources/Achievements/.
///
/// - Event achievements:      TriggerEvent("id")
/// - SingleRun achievements:  CheckSingleRun(liveStats)  (polled by AchievementTracker,
///                                                        which PlayerRefs adds automatically)
/// - Cumulative achievements: CheckCumulative()          (RunManager.CompleteRun, after the
///                                                        run is recorded)
///
/// Live and lifetime stats are both RunStats, so ONE reader maps a StatField
/// to a value — a new achievement stat is one enum entry + one switch line.
///
/// When an achievement completes it: marks it in SaveManager, unlocks its
/// rewards, adds it to EarnedThisRun (for the finish screen), and fires
/// OnUnlocked (for popups).
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        all = null;
        OnUnlocked = null;
        EarnedThisRun.Clear();
    }

    // ================================================================
    //  CHECKS
    // ================================================================

    public static void TriggerEvent(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return;
        EnsureLoaded();

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
        if (stats == null) return;
        EnsureLoaded();

        RunStats live = stats.history;
        int layer = RunConfig.I != null ? RunConfig.I.currentLayer : 1;

        foreach (var a in all)
        {
            if (a == null || a.type != AchievementType.SingleRun) continue;
            if (SaveManager.IsAchievementComplete(a.id)) continue;

            float value = a.statField switch
            {
                StatField.DeepestLayer => layer,
                StatField.FinalLevel   => stats.currentLevel,
                _                      => GetStat(live, a.statField)
            };
            if (value >= a.threshold) Complete(a);
        }
    }

    public static void CheckCumulative()
    {
        EnsureLoaded();
        RunStats lifetime = SaveManager.Data.lifetimeTotals;
        RunStats best = SaveManager.Data.bestRun;

        foreach (var a in all)
        {
            if (a == null || a.type != AchievementType.Cumulative) continue;
            if (SaveManager.IsAchievementComplete(a.id)) continue;

            // Depth and level don't sum across runs — use the best run instead
            RunStats source = a.statField == StatField.DeepestLayer || a.statField == StatField.FinalLevel
                ? best : lifetime;
            if (GetStat(source, a.statField) >= a.threshold) Complete(a);
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
        if (a.classToUnlock != null)
            SaveManager.UnlockClass(a.classToUnlock.classId);
        if (a.weaponToUnlock != null)
            SaveManager.UnlockWeapon(a.weaponToUnlock.weaponId);

        EarnedThisRun.Add(a);
        OnUnlocked?.Invoke(a);
    }

    // ================================================================
    //  STAT READER — one mapping for live and lifetime stats
    // ================================================================

    public static float GetStat(RunStats t, StatField f)
    {
        if (t == null) return 0f;
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
