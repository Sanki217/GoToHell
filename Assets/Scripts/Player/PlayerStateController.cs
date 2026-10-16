using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides whether the player may act this frame.
///
/// Two independent layers:
///   • BASE CONTROL (EnableControl / DisableControl) — the run-flow gate:
///     intro kick, name prompt, run summary. Off until the start sequence
///     hands it over.
///   • BLOCKERS (AddBlocker / RemoveBlocker, keyed by owner) — temporary
///     overlays: pause menu, level-up picker, chest roll, resurrection.
///
/// HasControl() = base control AND no blockers. Because blockers are keyed by
/// owner, closing one menu can never hand control back while another menu
/// (or the intro) still wants the player frozen — the same composition rule
/// GameTime uses for pauses.
/// </summary>
public class PlayerStateController : MonoBehaviour
{
    [Tooltip("Base control at spawn. Normally off — GameStartSequence enables it after the intro kick.")]
    public bool hasControl = false;

    [Tooltip("While base control is off, let external forces (the intro kick) move the player.")]
    public bool allowKickMovement = false;

    private readonly HashSet<object> blockers = new HashSet<object>();
    private readonly List<object> pruneBuffer = new List<object>();
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // ================================================================
    //  QUERY
    // ================================================================

    public bool HasControl()
    {
        if (!hasControl) return false;
        if (blockers.Count == 0) return true;
        PruneDestroyedBlockers();
        return blockers.Count == 0;
    }

    public bool AllowKickMovement() => allowKickMovement;

    /// <summary>True if anything other than base control is currently blocking input.</summary>
    public bool IsBlocked => blockers.Count > 0;

    // ================================================================
    //  BASE CONTROL — run flow
    // ================================================================

    public void EnableControl()
    {
        hasControl = true;
        allowKickMovement = false;   // stop allowing external kick forces
    }

    public void DisableControl()
    {
        hasControl = false;
        StopMotion();
    }

    // ================================================================
    //  BLOCKERS — menus / overlays
    // ================================================================

    /// <summary>
    /// Freeze player input until the same owner calls RemoveBlocker.
    /// Momentum is kept — menus pause time via GameTime, so the player resumes
    /// mid-arc instead of stopping dead in the air.
    /// </summary>
    public void AddBlocker(object owner)
    {
        if (owner == null) return;
        blockers.Add(owner);
    }

    public void RemoveBlocker(object owner)
    {
        if (owner == null) return;
        blockers.Remove(owner);
    }

    // ================================================================
    //  INTERNAL
    // ================================================================

    private void StopMotion()
    {
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }

    private void PruneDestroyedBlockers()
    {
        pruneBuffer.Clear();
        foreach (object o in blockers)
            if (o is Object uo && uo == null) pruneBuffer.Add(o);
        foreach (object o in pruneBuffer) blockers.Remove(o);
        pruneBuffer.Clear();
    }
}
