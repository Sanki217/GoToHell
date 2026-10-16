using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single owner of Time.timeScale and Time.fixedDeltaTime.
/// NOTHING else in the project writes those two values directly.
///
/// OWNERSHIP MODEL (why): several systems want to stop or slow time at the
/// same moment — pause menu, level-up picker, chest roll, run summary, hit
/// stop, charge-shot slow-mo, resurrection. With a single global flag the
/// last caller wins: closing the level-up picker used to resume the game
/// while the pause menu was still open. Now every request is keyed by its
/// owner and they compose:
///
///   • Pause(owner) / Unpause(owner) — time is frozen while ANY owner pauses.
///   • SetSlowMo(owner, scale) / ClearSlowMo(owner) — the SLOWEST active
///     request wins (hit stop during charge slow-mo = the hit stop's scale).
///
/// Effective timeScale = paused ? 0 : min(all slow-mo scales, 1).
/// fixedDeltaTime follows the scale so physics stays smooth in slow motion.
///
/// Everything resets automatically on scene load, so an owner destroyed
/// mid-request (scene change during a pause) can never leave the game frozen.
/// </summary>
public static class GameTime
{
    /// <summary>Project default fixed timestep (matches Project Settings → Time).</summary>
    public const float BaseFixedDelta = 0.02f;

    private static readonly HashSet<object> pauseOwners = new HashSet<object>();
    private static readonly Dictionary<object, float> slowMoOwners = new Dictionary<object, float>();
    private static readonly List<object> pruneBuffer = new List<object>();

    /// <summary>True while any owner holds a pause.</summary>
    public static bool IsPaused
    {
        get
        {
            if (PruneDestroyedOwners()) Apply();
            return pauseOwners.Count > 0;
        }
    }

    /// <summary>
    /// True if some owner OTHER than <paramref name="owner"/> holds a pause —
    /// e.g. the level-up picker ignoring Q while the pause menu is open.
    /// </summary>
    public static bool IsPausedByOtherThan(object owner)
    {
        if (PruneDestroyedOwners()) Apply();
        int mine = owner != null && pauseOwners.Contains(owner) ? 1 : 0;
        return pauseOwners.Count > mine;
    }

    /// <summary>True if this specific owner currently holds a pause.</summary>
    public static bool IsPausedBy(object owner) => owner != null && pauseOwners.Contains(owner);

    /// <summary>Current gameplay time scale ignoring pauses (1 = normal speed).</summary>
    public static float SlowMoScale { get; private set; } = 1f;

    // ================================================================
    //  PAUSE
    // ================================================================

    /// <summary>Freeze gameplay. UI keeps running on unscaled time.</summary>
    public static void Pause(object owner)
    {
        if (owner == null) return;
        if (pauseOwners.Add(owner)) Apply();
    }

    /// <summary>Release this owner's pause. Time resumes once no owner holds one.</summary>
    public static void Unpause(object owner)
    {
        if (owner == null) return;
        if (pauseOwners.Remove(owner)) Apply();
    }

    // ================================================================
    //  SLOW MOTION
    // ================================================================

    /// <summary>Request slow motion. Scale is clamped to 0.01..1; the slowest request wins.</summary>
    public static void SetSlowMo(object owner, float scale)
    {
        if (owner == null) return;
        slowMoOwners[owner] = Mathf.Clamp(scale, 0.01f, 1f);
        Apply();
    }

    /// <summary>Drop this owner's slow-motion request.</summary>
    public static void ClearSlowMo(object owner)
    {
        if (owner == null) return;
        if (slowMoOwners.Remove(owner)) Apply();
    }

    // ================================================================
    //  RESET
    // ================================================================

    /// <summary>Drop every pause and slow-mo request — normal speed. Safe from any state.</summary>
    public static void ResetAll()
    {
        pauseOwners.Clear();
        slowMoOwners.Clear();
        Apply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        // Required when "Enter Play Mode Options" skips domain reload
        pauseOwners.Clear();
        slowMoOwners.Clear();
        SlowMoScale = 1f;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetAll();
    }

    // ================================================================
    //  INTERNAL
    // ================================================================

    private static void Apply()
    {
        PruneDestroyedOwners();

        float scale = 1f;
        foreach (float s in slowMoOwners.Values)
            if (s < scale) scale = s;
        SlowMoScale = scale;

        if (pauseOwners.Count > 0)
        {
            Time.timeScale = 0f;   // fixedDeltaTime left as-is: no physics steps run while paused
            return;
        }

        Time.timeScale = scale;
        Time.fixedDeltaTime = BaseFixedDelta * scale;
    }

    /// <summary>Owners that are Unity objects and got destroyed release their requests.</summary>
    private static bool PruneDestroyedOwners()
    {
        pruneBuffer.Clear();
        foreach (object o in pauseOwners)
            if (o is Object uo && uo == null) pruneBuffer.Add(o);
        foreach (object o in slowMoOwners.Keys)
            if (o is Object uo && uo == null) pruneBuffer.Add(o);

        if (pruneBuffer.Count == 0) return false;
        foreach (object o in pruneBuffer)
        {
            pauseOwners.Remove(o);
            slowMoOwners.Remove(o);
        }
        pruneBuffer.Clear();
        return true;
    }
}
