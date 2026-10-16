using UnityEngine;

/// <summary>
/// Single owner of the run lifecycle. Every piece of state that must not leak
/// between runs gets reset here — when adding a new run-scoped static or
/// system, wire its reset into ResetRunScopedState instead of hoping a scene
/// load clears it.
///
///   BeginRun     — SceneFlow.StartRunAtLevel1 (Play / Restart).
///   CompleteRun  — death or victory. Records the run ONCE (history, best,
///                  lifetime totals) and evaluates cumulative achievements.
///                  Safe to call from several places; later calls return the
///                  already-recorded snapshot.
///   EndRun       — SceneFlow.GoToMainMenu.
/// </summary>
public static class RunManager
{
    private static RunStats recordedRun;

    /// <summary>The snapshot recorded for the current run, or null while it's still in progress.</summary>
    public static RunStats RecordedRun => recordedRun;

    /// <summary>A fresh run is starting (Level 1).</summary>
    public static void BeginRun()
    {
        ResetRunScopedState();
        if (RunConfig.I != null)
        {
            RunConfig.I.MarkRunStart();
            // Remember the pact loadout for next time
            SaveManager.Data.lastPactIds = new System.Collections.Generic.List<string>(RunConfig.I.selectedPactIds);
        }
        RunModifiers.MarkDirty();
    }

    /// <summary>
    /// The run just ended (death or victory). Builds the RunStats snapshot from
    /// the live player, records it, and checks cumulative achievements.
    /// Idempotent per run. Returns null if there is no player to record.
    /// </summary>
    public static RunStats CompleteRun(bool victory)
    {
        if (recordedRun != null) return recordedRun;

        PlayerStats stats = PlayerRefs.I != null ? PlayerRefs.I.Stats : Object.FindFirstObjectByType<PlayerStats>();
        if (stats == null) return null;

        RunConfig cfg = RunConfig.I;
        string playerName = cfg != null ? cfg.playerName : "Sinner";
        int deepestLayer  = cfg != null ? cfg.currentLayer : 1;
        float duration    = cfg != null ? cfg.RunDuration : Time.timeSinceLevelLoad;

        RunStats run = RunStatsBuilder.Build(stats, playerName, deepestLayer, duration, victory);
        run.classId  = cfg != null ? cfg.selectedClassId  : "";
        run.weaponId = cfg != null ? cfg.selectedWeaponId : "";
        run.pactIds  = cfg != null ? string.Join(",", cfg.selectedPactIds) : "";
        run.heat     = RunModifiers.TotalHeat;

        // Max depth = banked layers + this layer's live tracker
        DepthTracker tracker = stats.GetComponent<DepthTracker>();
        if (tracker == null) tracker = Object.FindFirstObjectByType<DepthTracker>();
        run.maxDepthReached = (cfg != null ? cfg.bankedDepth : 0f)
                            + (tracker != null ? tracker.CurrentDepth : 0f);

        // Meta currency: computed from the finished run, saved together with it
        run.metaCurrencyEarned = MetaProgression.ComputePayout(run);
        MetaProgression.AddCurrency(run.metaCurrencyEarned, save: false);

        SaveManager.RecordRun(run);   // saves
        Achievements.CheckCumulative();

        recordedRun = run;
        return run;
    }

    /// <summary>The run is over and we're leaving it (quit to menu).</summary>
    public static void EndRun()
    {
        ResetRunScopedState();
        if (RunConfig.I != null) RunConfig.I.Reset();
    }

    /// <summary>
    /// Everything static/global that is scoped to a single run.
    /// Add new run-scoped resets HERE and nowhere else.
    /// </summary>
    private static void ResetRunScopedState()
    {
        recordedRun = null;
        PlayerHealth.HasExtraLife = false;      // unused extra life must not carry over
        UpgradeRarityRoller.ResetPity();        // pity is a per-run mechanic
        Achievements.ResetRun();                // per-run earned list for the finish screen
        GameTime.ResetAll();                    // never enter/leave a run frozen or slowed
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => recordedRun = null;   // Enter Play Mode without domain reload
}
