using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads/saves SaveData to disk as JSON. Static access from anywhere.
/// Auto-loads on first access. Call Save() after any change.
///
/// File location: Application.persistentDataPath/save.json
///
/// CRASH SAFETY (a corrupt save would wipe years of unlocks):
///   • Writes go to save.json.tmp first, then atomically replace save.json;
///     the previous good file is kept as save.json.bak.
///   • Load falls back to the backup if the main file is unreadable, and a
///     corrupt file is preserved as save.json.corrupt instead of being
///     silently overwritten.
///   • saveVersion + Migrate() give old saves an upgrade path.
/// </summary>
public static class SaveManager
{
    /// <summary>Current save format. Bump when the format changes and handle it in Migrate().</summary>
    public const int CurrentVersion = 2;

    private static SaveData _data;
    private static string FilePath   => Path.Combine(Application.persistentDataPath, "save.json");
    private static string TempPath   => FilePath + ".tmp";
    private static string BackupPath => FilePath + ".bak";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => _data = null;   // Enter Play Mode without domain reload

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
        _data = TryRead(FilePath);

        if (_data == null && File.Exists(FilePath))
        {
            // Keep the broken file for inspection instead of overwriting it on the next Save
            Debug.LogWarning("[SaveManager] Save file corrupt — trying backup.");
            TryCopy(FilePath, FilePath + ".corrupt");
        }

        if (_data == null) _data = TryRead(BackupPath);
        if (_data == null) _data = new SaveData();

        EnsureValid(_data);
        Migrate(_data);
    }

    public static void Save()
    {
        if (_data == null) return;
        try
        {
            _data.saveVersion = CurrentVersion;
            string json = JsonUtility.ToJson(_data, true);

            // Write-then-swap: a crash mid-write can never truncate the real save
            File.WriteAllText(TempPath, json);
            if (File.Exists(FilePath))
                File.Replace(TempPath, FilePath, BackupPath, true);
            else
                File.Move(TempPath, FilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
        }
    }

    public static void DeleteSave()
    {
        TryDelete(FilePath);
        TryDelete(TempPath);
        TryDelete(BackupPath);
        _data = new SaveData();
    }

    /// <summary>Upgrade older save formats in place. Add a step per version bump.</summary>
    private static void Migrate(SaveData d)
    {
        // v1 → v2: meta progression fields. New lists are created by
        // EnsureValid; nothing to convert.
        if (d.saveVersion < 2) d.saveVersion = 2;
        if (d.saveVersion > CurrentVersion)
            Debug.LogWarning($"[SaveManager] Save is from a newer build (v{d.saveVersion}).");
    }

    /// <summary>Old or hand-edited saves may lack collections — never hand out nulls.</summary>
    private static void EnsureValid(SaveData d)
    {
        if (d.unlockedClassIds == null)        d.unlockedClassIds = new List<string>();
        if (d.unlockedWeaponIds == null)       d.unlockedWeaponIds = new List<string>();
        if (d.unlockedPactIds == null)         d.unlockedPactIds = new List<string>();
        if (d.completedAchievementIds == null) d.completedAchievementIds = new List<string>();
        if (d.runHistory == null)              d.runHistory = new List<RunStats>();
        if (d.bestRun == null)                 d.bestRun = new RunStats();
        if (d.lifetimeTotals == null)          d.lifetimeTotals = new RunStats();
        if (d.lastPlayerName == null)          d.lastPlayerName = "";
        if (d.metaRanks == null)               d.metaRanks = new List<MetaRank>();
        if (d.lastPactIds == null)             d.lastPactIds = new List<string>();
        if (d.metaCurrency < 0)                d.metaCurrency = 0;
    }

    private static SaveData TryRead(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Could not read {Path.GetFileName(path)}: {e.Message}");
            return null;
        }
    }

    private static void TryCopy(string from, string to)
    {
        try { File.Copy(from, to, true); } catch { /* best effort */ }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    // ================================================================
    //  PLAYER NAME
    // ================================================================

    /// <summary>Last name the player entered — pre-fills the Level 1 name prompt.</summary>
    public static string LastPlayerName => Data.lastPlayerName;

    public static void SetLastPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Data.lastPlayerName == name) return;
        Data.lastPlayerName = name;
        Save();
    }

    // ================================================================
    //  CLASSES
    // ================================================================

    public static bool IsClassUnlocked(string classId) =>
        Data.unlockedClassIds.Contains(classId);

    public static void UnlockClass(string classId)
    {
        if (string.IsNullOrEmpty(classId) || Data.unlockedClassIds.Contains(classId)) return;
        Data.unlockedClassIds.Add(classId);
        Save();
    }

    // ================================================================
    //  WEAPONS
    // ================================================================

    public static bool IsWeaponUnlocked(string weaponId) =>
        Data.unlockedWeaponIds.Contains(weaponId);

    public static void UnlockWeapon(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId) || Data.unlockedWeaponIds.Contains(weaponId)) return;
        Data.unlockedWeaponIds.Add(weaponId);
        Save();
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

    // Every numeric RunStats field is handled automatically — see RunStats.
    private static void UpdateBest(RunStats r) => Data.bestRun.MaxWith(r);
    private static void AddToLifetime(RunStats r) => Data.lifetimeTotals.AddTotals(r);
}
