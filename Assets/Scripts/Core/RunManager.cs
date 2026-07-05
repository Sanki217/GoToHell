/// <summary>
/// Single owner of the run lifecycle. Every piece of state that must not leak
/// between runs gets reset here — when adding a new run-scoped static or
/// system, wire its reset into BeginRun/EndRun instead of hoping a scene load
/// clears it.
///
/// Called by SceneFlow: BeginRun on StartRunAtLevel1, EndRun on GoToMainMenu.
/// </summary>
public static class RunManager
{
    /// <summary>A fresh run is starting (Character Creator → Level 1).</summary>
    public static void BeginRun()
    {
        ResetRunScopedState();
        RunConfig.I?.MarkRunStart();
    }

    /// <summary>The run is over (death, victory, or quit to menu).</summary>
    public static void EndRun()
    {
        ResetRunScopedState();
        RunConfig.I?.Reset();
    }

    /// <summary>
    /// Everything static/global that is scoped to a single run.
    /// Add new run-scoped resets HERE and nowhere else.
    /// </summary>
    private static void ResetRunScopedState()
    {
        PlayerHealth.HasExtraLife = false;      // unused extra life must not carry over
        UpgradeRarityRoller.ResetPity();        // pity is a per-run mechanic
        GameTime.Resume();                      // never enter/leave a run frozen or slowed
    }
}
