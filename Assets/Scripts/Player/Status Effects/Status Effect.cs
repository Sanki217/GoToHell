using UnityEngine;

/// <summary>
/// Abstract base class for all status effects.
/// Each status effect is a component added to an enemy GameObject.
///
/// Rules (from GDD):
///   - Duration refresh on reapplication — no stacking
///   - Quality scales via PlayerStats percentage bonuses
///   - Applied only by the player, never by enemies to the player
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

    // ================================================================
    //  INIT  (called by Enemy.ApplyStatus)
    // ================================================================

    public virtual void Initialize(float dur, PlayerStats stats, PlayerUpgradeManager mgr)
    {
        duration = dur;
        timeRemaining = dur;
        playerStats = stats;
        upgradeManager = mgr;
        enemy = GetComponent<Enemy>();

        OnApplied();
    }

    /// <summary>Refresh the duration without restarting the effect.</summary>
    public void Refresh()
    {
        timeRemaining = duration;
        OnRefreshed();
    }

    // ================================================================
    //  TICK
    // ================================================================

    protected virtual void Update()
    {
        if (timeRemaining <= 0f) return;

        timeRemaining -= Time.deltaTime;
        OnTick(Time.deltaTime);

        if (timeRemaining <= 0f)
            OnExpired();
    }

    // ================================================================
    //  OVERRIDEABLE HOOKS
    // ================================================================

    protected virtual void OnApplied() { }
    protected virtual void OnRefreshed() { }
    protected virtual void OnTick(float dt) { }
    protected virtual void OnExpired() { Destroy(this); }

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>
    /// Tint the enemy renderer to show the status effect visually.
    /// Call with the status color from OnApplied, restore in OnExpired.
    /// </summary>
    protected void SetTint(Color color)
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = color;
    }

    protected void RestoreTint()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = Color.white;
    }
}