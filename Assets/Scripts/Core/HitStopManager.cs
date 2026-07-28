using UnityEngine;
using System.Collections;

/// <summary>
/// Hitstop — brief near-freeze of game time for combat impact.
/// Put ONE on a manager object in each gameplay scene; all tuning is in the
/// Inspector. Every trigger can be toggled off.
///
/// TRIGGERS:
///   • Player deals damage — duration scales with the hit's damage inside the
///     damageRefMin..damageRefMax reference range. Status ticks (burn etc.)
///     are excluded by default so DoT never stutters the game.
///   • Player kills an enemy — flat duration (usually the longest).
///   • Player takes damage — duration scales with damage relative to max HP.
///
/// DAMAGE REFERENCE RANGE: damageRefMin/Max define what counts as the weakest
/// and strongest possible hit right now. With autoCalibrate on, damageRefMax
/// grows to the biggest hit seen this scene, so the scaling stays meaningful
/// as the player's damage climbs during a run.
///
/// TIME OWNERSHIP: plays nice with GameTime — a stop is skipped while the game
/// is paused (pickers, chest UI), aborts if another system grabs the timescale
/// (charge slow-mo, resurrection), and only restores time it still owns.
/// </summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Header("Global")]
    [Tooltip("Timescale during a stop. 0.05 = near-freeze, higher = softer.")]
    [Range(0.01f, 0.5f)] public float stopTimeScale = 0.05f;

    [Header("Player Deals Damage")]
    public bool stopOnDamageDealt = true;
    [Tooltip("Stop duration (realtime seconds) for a hit at/below damageRefMin.")]
    public float dealtMinDuration = 0.02f;
    [Tooltip("Stop duration (realtime seconds) for a hit at/above damageRefMax.")]
    public float dealtMaxDuration = 0.12f;
    [Tooltip("Also stop on status/DoT ticks (burn, shock...). Usually too twitchy.")]
    public bool includeStatusDamage = false;
    [Tooltip("Also stop on explosion damage (barrels, Enemy Explosion upgrade).")]
    public bool includeExplosionDamage = true;

    [Header("Damage Reference Range (scaling)")]
    [Tooltip("Damage treated as the weakest possible hit.")]
    public float damageRefMin = 1f;
    [Tooltip("Damage treated as the strongest possible hit.")]
    public float damageRefMax = 25f;
    [Tooltip("Grow damageRefMax automatically to the biggest hit seen, so scaling tracks the player's current damage.")]
    public bool autoCalibrate = true;

    [Header("Player Kills")]
    public bool stopOnKill = true;
    [Tooltip("Flat stop duration (realtime seconds) on a kill.")]
    public float killDuration = 0.15f;

    [Header("Player Takes Damage")]
    public bool stopOnPlayerHurt = true;
    [Tooltip("Stop duration when the hit is a tiny fraction of max HP.")]
    public float hurtMinDuration = 0.05f;
    [Tooltip("Stop duration when the hit is the player's whole max HP.")]
    public float hurtMaxDuration = 0.18f;

    // ── state ───────────────────────────────────────────────────────
    private float stopEndRealtime = -1f;
    private Coroutine stopRoutine;

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ================================================================
    //  STATIC NOTIFY API — safe no-ops when no manager is in the scene
    // ================================================================

    public static void NotifyDamageDealt(float amount, DamageSource source)
        => Instance?.OnDamageDealt(amount, source);

    public static void NotifyKill()
        => Instance?.OnKill();

    public static void NotifyPlayerHurt(float amount, float maxHP)
        => Instance?.OnPlayerHurt(amount, maxHP);

    // ================================================================
    //  TRIGGERS
    // ================================================================

    private void OnDamageDealt(float amount, DamageSource source)
    {
        // Calibrate even when the trigger is disabled — keeps the range honest
        if (autoCalibrate && amount > damageRefMax) damageRefMax = amount;

        if (!stopOnDamageDealt) return;
        if (source == DamageSource.Status && !includeStatusDamage) return;
        if (source == DamageSource.Explosion && !includeExplosionDamage) return;

        float t = Mathf.InverseLerp(damageRefMin, damageRefMax, amount);
        RequestStop(Mathf.Lerp(dealtMinDuration, dealtMaxDuration, t));
    }

    private void OnKill()
    {
        if (!stopOnKill) return;
        RequestStop(killDuration);
    }

    private void OnPlayerHurt(float amount, float maxHP)
    {
        if (!stopOnPlayerHurt) return;
        float t = maxHP > 0f ? Mathf.Clamp01(amount / maxHP) : 1f;
        RequestStop(Mathf.Lerp(hurtMinDuration, hurtMaxDuration, t));
    }

    // ================================================================
    //  CORE
    // ================================================================

    /// <summary>Near-freeze time for `duration` realtime seconds. A longer
    /// request extends an active stop; shorter ones are absorbed by it.</summary>
    public void RequestStop(float duration)
    {
        if (duration <= 0f) return;
        if (GameTime.IsPaused) return;                            // menus/pickers own the pause
        if (Time.timeScale < 1f && stopRoutine == null) return;   // another system runs slow-mo — don't fight it

        float end = Time.realtimeSinceStartup + duration;
        if (end <= stopEndRealtime) return;                       // active stop already covers it
        stopEndRealtime = end;

        if (stopRoutine == null)
            stopRoutine = StartCoroutine(StopRoutine());
    }

    private IEnumerator StopRoutine()
    {
        GameTime.SetScale(stopTimeScale);

        while (Time.realtimeSinceStartup < stopEndRealtime)
        {
            // Another system took the timescale (pause, charge slow-mo, death) — back off
            if (!Mathf.Approximately(Time.timeScale, stopTimeScale))
            {
                stopRoutine = null;
                stopEndRealtime = -1f;
                yield break;
            }
            yield return null;
        }

        // Restore only if we still own the timescale
        if (Mathf.Approximately(Time.timeScale, stopTimeScale))
            GameTime.Resume();

        stopRoutine = null;
        stopEndRealtime = -1f;
    }
}
