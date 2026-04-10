using UnityEngine;

/// <summary>
/// Base class for curse upgrades — upgrades with a two-phase lifecycle:
///
///   Phase 1 (curse active, lasts minCurseLevels–maxCurseLevels player level-ups):
///     Both the penalty (negative effect) AND the bonus (positive effect) are active.
///
///   Phase 2 (curse lifted, permanent):
///     The penalty ends. The bonus stays forever.
///
/// HOW TO USE:
///   1. Inherit from PlayerUpgradeCurse instead of PlayerUpgrade.
///   2. Implement OnCurseActivated(mgr) — set up both the penalty and bonus here.
///   3. Implement OnCurseLifted()       — tear down only the penalty here.
///
/// The curse level counter decrements on every OnPlayerLevelUp event.
/// </summary>
public abstract class PlayerUpgradeCurse : PlayerUpgrade
{
    [Header("Curse Duration")]
    [Tooltip("Penalty lasts this many player level-ups (minimum, inclusive).")]
    public int minCurseLevels = 3;
    [Tooltip("Penalty lasts this many player level-ups (maximum, inclusive).")]
    public int maxCurseLevels = 5;

    // ================================================================
    //  PROTECTED STATE — readable by subclasses
    // ================================================================

    protected PlayerUpgradeManager curseMgr;
    protected bool curseActive = true;
    protected int curseLevelsRemaining;

    // ================================================================
    //  LIFECYCLE
    // ================================================================

    public override sealed void OnAdded(PlayerUpgradeManager mgr)
    {
        curseMgr = mgr;
        curseLevelsRemaining = Random.Range(minCurseLevels, maxCurseLevels + 1);
        mgr.OnPlayerLevelUp += HandleLevelUp;
        OnCurseActivated(mgr);
    }

    private void HandleLevelUp(int newLevel)
    {
        if (!curseActive) return;

        curseLevelsRemaining--;
        if (curseLevelsRemaining <= 0)
        {
            curseActive = false;
            curseMgr.OnPlayerLevelUp -= HandleLevelUp;
            OnCurseLifted();
        }
    }

    protected virtual void OnDestroy()
    {
        // Clean up event subscription if the object is ever removed mid-run
        if (curseActive && curseMgr != null)
            curseMgr.OnPlayerLevelUp -= HandleLevelUp;
    }

    // ================================================================
    //  ABSTRACT INTERFACE
    // ================================================================

    /// <summary>
    /// Called once when the upgrade orb is collected.
    /// Subscribe to events for both the penalty AND the bonus here.
    /// </summary>
    protected abstract void OnCurseActivated(PlayerUpgradeManager mgr);

    /// <summary>
    /// Called once after the required number of level-ups have passed.
    /// Unsubscribe from penalty events here. Leave bonus events intact.
    /// </summary>
    protected abstract void OnCurseLifted();
}
