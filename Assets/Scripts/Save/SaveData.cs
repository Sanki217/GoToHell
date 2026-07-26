using System;
using System.Collections.Generic;

/// <summary>
/// The complete persistent save. One instance lives on disk as JSON.
/// Holds unlocked classes/weapons/pacts, completed achievements, and run history.
///
/// Everything is identified by string ID so adding new content never breaks
/// old saves. Content flagged unlockedByDefault on its ScriptableObject never
/// needs an entry here.
/// </summary>
[Serializable]
public class SaveData
{
    // Bump when the save format changes incompatibly; lets future versions
    // migrate old files instead of discarding them.
    public int saveVersion = 1;

    // Last name the player entered — pre-fills the Level 1 name prompt.
    public string lastPlayerName = "";

    // IDs of classes the player has unlocked (beyond defaults)
    public List<string> unlockedClassIds = new List<string>();

    // IDs of weapons the player has unlocked (beyond defaults)
    public List<string> unlockedWeaponIds = new List<string>();

    // IDs of pacts the player has unlocked (beyond defaults)
    public List<string> unlockedPactIds = new List<string>();

    // IDs of achievements the player has completed
    public List<string> completedAchievementIds = new List<string>();

    // Every run the player has finished (newest appended at end)
    public List<RunStats> runHistory = new List<RunStats>();

    // Aggregated best-in-a-single-run values, updated whenever a run is recorded.
    public RunStats bestRun = new RunStats();

    // Lifetime totals across all runs (summed each time a run ends)
    public RunStats lifetimeTotals = new RunStats();
}
