using System;
using System.Collections.Generic;

/// <summary>
/// The complete persistent save. One instance lives on disk as JSON.
/// Holds unlocked pacts, completed achievements, and run history.
///
/// Pacts and achievements are identified by string ID (the ScriptableObject's name
/// or a defined id field) so adding new ones never breaks old saves.
/// </summary>
[Serializable]
public class SaveData
{
    // IDs of pacts the player has unlocked
    public List<string> unlockedPactIds = new List<string>();

    // IDs of achievements the player has completed
    public List<string> completedAchievementIds = new List<string>();

    // Every run the player has finished (newest appended at end)
    public List<RunStats> runHistory = new List<RunStats>();

    // Aggregated best-in-a-single-run values, updated whenever a run is recorded.
    // Stored as a flat dictionary-like pair of lists for JsonUtility compatibility
    // (JsonUtility can't serialize Dictionary directly).
    public RunStats bestRun = new RunStats();

    // Lifetime totals across all runs (optional aggregate; summed each time a run ends)
    public RunStats lifetimeTotals = new RunStats();
}
