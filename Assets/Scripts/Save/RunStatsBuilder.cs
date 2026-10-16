using UnityEngine;

/// <summary>
/// Converts a live PlayerStats into a serializable RunStats snapshot.
/// Called when a run ends (death or victory) before recording to SaveManager.
///
/// The live history already IS a RunStats, so this is a clone plus the run's
/// meta data — new stats need no changes here.
/// </summary>
public static class RunStatsBuilder
{
    public static RunStats Build(PlayerStats s, string playerName, int deepestLayer,
                                 float runDuration, bool victory)
    {
        RunStats r = s != null && s.history != null ? s.history.Clone() : new RunStats();

        r.playerName         = playerName;
        r.dateUtc            = System.DateTime.UtcNow.ToString("o");
        r.deepestLayer       = deepestLayer;
        r.finalLevel         = s != null ? s.currentLevel : 1;
        r.runDurationSeconds = runDuration;
        r.victory            = victory;
        return r;
    }
}
