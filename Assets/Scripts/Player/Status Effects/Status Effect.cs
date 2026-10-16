using UnityEngine;

/// <summary>
/// Abstract base class for all status effects.
/// Each status effect is a component added to an enemy GameObject by
/// Enemy.ApplyStatus.
///
/// Rules (from GDD):
///   - Reapplication refreshes the duration — no stacking
///   - Quality scales via PlayerStats percentage bonuses
///   - Applied only by the player, never by enemies to the player
///
/// The base class owns everything shared, so a new status is just its own
/// behaviour:
///   • Type / TintColor / ApplyFX — identity, colour and sound/particle.
///   • Initialize (first application) vs Reapply (refresh) — subclasses get
///     OnApplied once and OnRefreshed on every reapplication. Reapplying no
///     longer re-runs setup (Freeze used to re-capture an already-slowed
///     cooldown and leave shooters permanently slow).
///   • Stats recording + the upgrade-bus StatusApplied event fire on every
///     application, exactly as before.
///   • The enemy tint is a TintController layer owned by this effect, removed
///     automatically when the effect ends — overlapping statuses no longer
///     reset each other's colour.
/// </summary>
public abstract class StatusEffect : MonoBehaviour
{
    // ================================================================
    //  STATE
    // ================================================================

    protected float duration;
    protected float timeRemaining;
    protected PlayerStats playerStats;
    protected PlayerUpgradeManager upgradeManager;
    protected Enemy enemy;

    private bool ended;

    // ================================================================
    //  IDENTITY — implemented per status
    // ================================================================

    public abstract StatusType Type { get; }
    protected abstract Color TintColor { get; }
    protected abstract ActionFX ApplyFX { get; }

    // ================================================================
    //  APPLY / REAPPLY  (called by Enemy.ApplyStatus)
    // ================================================================

    /// <summary>First application. Subclasses set duration/timeRemaining in OnApplied.</summary>
    public void Initialize(PlayerStats stats, PlayerUpgradeManager mgr)
    {
        playerStats = stats;
        upgradeManager = mgr;
        enemy = GetComponent<Enemy>();

        OnApplied();

        if (enemy != null) enemy.Tint.SetTint(this, TintColor);
        NotifyApplied();
    }

    /// <summary>Reapplication: refresh the duration, keep running state.</summary>
    public void Reapply(PlayerStats stats, PlayerUpgradeManager mgr)
    {
        if (ended) return;
        if (stats != null) playerStats = stats;
        if (mgr != null) upgradeManager = mgr;

        timeRemaining = duration;
        OnRefreshed();
        NotifyApplied();
    }

    private void NotifyApplied()
    {
        FXManager.Play(ApplyFX, transform.position);
        if (upgradeManager != null) upgradeManager.StatusApplied(gameObject, Type);
        if (playerStats != null) playerStats.RecordStatusApplied(Type);
    }

    // ================================================================
    //  TICK
    // ================================================================

    protected virtual void Update()
    {
        if (ended) return;

        float dt = Time.deltaTime;
        timeRemaining -= dt;
        OnTick(dt);

        if (!ended && timeRemaining <= 0f)
            End();
    }

    /// <summary>Ends the effect now: OnExpired, then the component is removed.</summary>
    protected void End()
    {
        if (ended) return;
        ended = true;
        OnExpired();
        Destroy(this);
    }

    protected virtual void OnDestroy()
    {
        if (enemy != null) enemy.Tint.ClearTint(this);
        OnRemoved();
    }

    // ================================================================
    //  OVERRIDEABLE HOOKS
    // ================================================================

    /// <summary>First application — set duration/timeRemaining here.</summary>
    protected virtual void OnApplied() { }
    /// <summary>Reapplied while active — timeRemaining was already reset to duration.</summary>
    protected virtual void OnRefreshed() { }
    protected virtual void OnTick(float dt) { }
    /// <summary>Natural expiry (not called when the enemy dies).</summary>
    protected virtual void OnExpired() { }
    /// <summary>Always called when the component goes away (expiry, consume, enemy death). Undo side effects here.</summary>
    protected virtual void OnRemoved() { }
}
