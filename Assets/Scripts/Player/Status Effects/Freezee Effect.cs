using UnityEngine;

/// <summary>
/// Freeze: slows enemy movement and attack speed.
///
/// Works on EVERY enemy type through Enemy.SpeedMultiplier — patrols, wall
/// jumpers, shooters and stalkers all scale their movement (and shooters
/// their fire rate) by it. No per-type special cases here.
///
/// At FreezeStrength 1.0 → 50% slow
/// At FreezeStrength 1.5 → 75% slow
/// Capped at 95% (enemy never fully stops)
/// </summary>
public class FreezeEffect : StatusEffect
{
    [Header("Freeze Values")]
    public float baseSlowPercent = 0.5f;
    public float baseDuration = 3f;
    public float maxSlowPercent = 0.95f;

    [Tooltip("Seconds between freeze 'ticks' (tick FX + upgrade-bus FreezeTick event).")]
    public float tickInterval = 0.5f;

    private float tickTimer;

    public override StatusType Type => StatusType.Freeze;
    protected override Color TintColor => new Color(0.5f, 0.85f, 1f);
    protected override ActionFX ApplyFX => ActionFX.StatusFreezeApply;

    protected override void OnApplied()
    {
        duration = baseDuration;
        timeRemaining = baseDuration;
        tickTimer = 0f;
        ApplySlow();
    }

    protected override void OnRefreshed()
    {
        ApplySlow();   // re-evaluate with the latest freezeStrength
    }

    protected override void OnTick(float dt)
    {
        tickTimer += dt;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;

        FXManager.Play(ActionFX.StatusFreezeTick, transform.position);
        if (upgradeManager != null) upgradeManager.FreezeTick(gameObject);
    }

    protected override void OnRemoved()
    {
        if (enemy != null) enemy.ClearSlow(this);
    }

    private void ApplySlow()
    {
        if (enemy == null) return;
        float strength = playerStats != null ? playerStats.freezeStrength : 1f;
        float slow = Mathf.Clamp(baseSlowPercent * strength, 0f, maxSlowPercent);
        enemy.SetSlow(this, 1f - slow);
    }
}
