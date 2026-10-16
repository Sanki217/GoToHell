using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Character slots (Diablo-style): create, delete, look up, and record runs.
/// The characters themselves live in SaveData.characters.
///
///   CharacterRoster.Characters                 all slots in order
///   CharacterRoster.Create(name, class, points, hardcore)
///   CharacterRoster.Delete(id)
///   CharacterRoster.Get(id)
///   CharacterRoster.RecordRun(run, victory)    RunManager calls this when a run ends
/// </summary>
public static class CharacterRoster
{
    public const int MaxCharacters = 8;
    public const int CreationPoints = 5;
    /// <summary>Every creation stat starts here; points are added on top (no class presets).</summary>
    public const int BaseStatValue = 1;
    public const string DefaultName = "Unknown Sinner";

    /// <summary>
    /// Stats a character starts with / can spend creation points on.
    /// Size and Cooldown are only gained in-game (upgrades), never at creation.
    /// </summary>
    public static readonly PrimaryStat[] CreationStats =
    {
        PrimaryStat.Agility, PrimaryStat.AttackDamage, PrimaryStat.Luck, PrimaryStat.Psyche, PrimaryStat.Health
    };

    public static bool IsCreationStat(PrimaryStat s) => System.Array.IndexOf(CreationStats, s) >= 0;
    public const int MaxNameLength = 20;

    public static event Action OnChanged;

    public static IReadOnlyList<CharacterData> Characters => SaveManager.Data.characters;
    public static bool IsFull => SaveManager.Data.characters.Count >= MaxCharacters;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => OnChanged = null;

    public static CharacterData Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (CharacterData c in SaveManager.Data.characters)
            if (c.id == id) return c;
        return null;
    }

    /// <summary>
    /// Creates and saves a character. <paramref name="points"/> = free points per
    /// stat (must sum to CreationPoints or less). Null if invalid or slots are full.
    /// </summary>
    public static CharacterData Create(string name, ClassDefinition cls,
                                       IReadOnlyDictionary<PrimaryStat, int> points, bool hardcore)
    {
        if (IsFull || cls == null || !cls.IsUnlocked) return null;

        int total = 0;
        if (points != null)
            foreach (KeyValuePair<PrimaryStat, int> kv in points)
            {
                if (kv.Value < 0 || (kv.Value > 0 && !IsCreationStat(kv.Key))) return null;
                total += kv.Value;
            }
        if (total > CreationPoints) return null;

        name = CleanName(name);
        var c = new CharacterData
        {
            id = Guid.NewGuid().ToString("N"),
            name = name,
            classId = cls.classId,
            createdUtc = DateTime.UtcNow.ToString("o"),
            hardcore = hardcore,
        };
        if (points != null)
            foreach (KeyValuePair<PrimaryStat, int> kv in points) c.SetAllocated(kv.Key, kv.Value);

        SaveManager.Data.characters.Add(c);
        SaveManager.Save();
        OnChanged?.Invoke();
        return c;
    }

    public static bool Delete(string id)
    {
        CharacterData c = Get(id);
        if (c == null) return false;
        SaveManager.Data.characters.Remove(c);
        SaveManager.Save();
        OnChanged?.Invoke();
        return true;
    }

    public static string CleanName(string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim();
        return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
    }

    /// <summary>
    /// Updates the run's character after a run ends (no save — the caller
    /// saves together with the run record). Hardcore death marks it dead.
    /// </summary>
    public static void RecordRun(RunStats run, bool victory)
    {
        if (run == null) return;
        CharacterData c = Get(run.characterId);
        if (c == null) return;

        c.runsPlayed++;
        if (victory) c.victories++;
        c.deepestLayer = Mathf.Max(c.deepestLayer, run.deepestLayer);
        c.bestDepth = Mathf.Max(c.bestDepth, run.maxDepthReached);
        c.totalKills += run.enemiesKilled;
        if (!string.IsNullOrEmpty(run.pactId)) c.lastPactId = run.pactId;
        if (c.hardcore && !victory) c.dead = true;

        OnChanged?.Invoke();
    }
}
