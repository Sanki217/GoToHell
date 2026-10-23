using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Serializable statistics for one run. The SAME type is used for:
///   • the live run (PlayerStats.history — written by the Record* helpers),
///   • the saved snapshot (SaveData.runHistory),
///   • best-run maxima and lifetime totals (SaveData.bestRun / lifetimeTotals).
///
/// ADDING A STAT: add one public int/float field below and record it in
/// PlayerStats. Snapshot copy, lifetime summing and best-run maxima are
/// automatic (reflection over numeric fields), so the four hand-maintained
/// field lists that used to drift out of sync are gone.
/// </summary>
[Serializable]
public class RunStats
{
    // Meta
    public string playerName;
    public string dateUtc;          // when the run ended
    public string classId;          // selected class (leaderboard display)
    public string weaponId;         // selected weapon (leaderboard display)
    public int    deepestLayer;     // highest layer reached
    public int    finalLevel;       // player level at death
    public float  runDurationSeconds;
    public bool   victory;          // reached the end vs died
    public string characterId;      // CharacterData.id
    public bool   hardcore;
    public string pactId;           // RMB skill taken into the run
    public int    metaCurrencyEarned;
    public int    xpEarned;           // character XP from this run
    public int    levelsGained;       // character levels gained from this run

    // Max depth reached (world units below the layer start, summed across
    // layers). Tracked as lowest-Y-ever by DepthTracker — bouncing back up
    // and falling again never double-counts.
    public float maxDepthReached;

    // Movement
    public float distanceMovedLeft;
    public float distanceMovedRight;
    public float totalDistance;
    public int   jumpsPerformed;
    public float timeAirborne;
    public float timeGrounded;
    public int   wallSlideCount;
    public float totalWallSlideDuration;
    public int   hoverCount;
    public float totalHoverDuration;
    public float energySpentHovering;
    public int   dashCount;
    public float totalDashDistance;
    public float energySpentDashing;
    public int   dashesHitEnemy;
    public int   dashesHitWall;
    public int   slashCount;
    public int   slashesHitEnemy;

    // Combat
    public int   weakArrowsFired;
    public int   mediumArrowsFired;
    public int   chargedArrowsFired;
    public int   extraArrowsFired;
    public int   totalArrowsFired;
    public int   arrowsHitEnemy;
    public int   arrowsHitWall;
    public int   arrowsHitDestructible;
    public int   arrowsPickedUp;
    public int   chargesCancelledByEnergy;
    public int   enemiesKilled;
    public float totalDamageDealt;
    public float damageByArrow;
    public float damageByDash;
    public float damageByStatus;
    public float damageByExplosion;
    public float damageBySlash;
    public int   critsLanded;
    public int   burnApplied;
    public int   freezeApplied;
    public int   holyApplied;
    public int   shockApplied;

    // Resources
    public int   soulsCollected;
    public float xpGained;
    public float energyGainedTotal;
    public float energyFromKills;
    public float energyFromFalling;
    public float energyFromLava;
    public float energyFromWallSlide;
    public float energySpentCharging;

    // Survival
    public float damageTaken;
    public int   timesHit;
    public float hpRestored;
    public int   layersCompleted;

    // ================================================================
    //  NUMERIC FIELD HELPERS
    // ================================================================

    // Per-run facts that must never be summed into lifetime totals
    // (they are tracked as maxima in bestRun instead).
    private static readonly HashSet<string> NotSummed = new HashSet<string>
    {
        nameof(deepestLayer), nameof(finalLevel), nameof(maxDepthReached)
    };

    private static FieldInfo[] numericFields;

    private static FieldInfo[] NumericFields
    {
        get
        {
            if (numericFields != null) return numericFields;
            var list = new List<FieldInfo>();
            foreach (FieldInfo f in typeof(RunStats).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (f.FieldType == typeof(int) || f.FieldType == typeof(float))
                    list.Add(f);
            numericFields = list.ToArray();
            return numericFields;
        }
    }

    /// <summary>Deep copy (all fields, including meta).</summary>
    public RunStats Clone() => JsonUtility.FromJson<RunStats>(JsonUtility.ToJson(this));

    /// <summary>Adds every summable numeric field of <paramref name="run"/> into this (lifetime totals).</summary>
    public void AddTotals(RunStats run)
    {
        if (run == null) return;
        foreach (FieldInfo f in NumericFields)
        {
            if (NotSummed.Contains(f.Name)) continue;
            if (f.FieldType == typeof(int)) f.SetValue(this, (int)f.GetValue(this) + (int)f.GetValue(run));
            else f.SetValue(this, (float)f.GetValue(this) + (float)f.GetValue(run));
        }
    }

    /// <summary>Keeps the per-field maximum of this and <paramref name="run"/> (best-in-a-single-run).</summary>
    public void MaxWith(RunStats run)
    {
        if (run == null) return;
        foreach (FieldInfo f in NumericFields)
        {
            if (f.FieldType == typeof(int)) f.SetValue(this, Math.Max((int)f.GetValue(this), (int)f.GetValue(run)));
            else f.SetValue(this, Math.Max((float)f.GetValue(this), (float)f.GetValue(run)));
        }
    }
}
