using UnityEngine;

/// <summary>
/// Burn: deals damage over time in ticks.
/// Duration is fixed. Damage per tick scales with PlayerStats.burnStrength.
///
/// Base values (before strength scaling):
///   tickInterval = 0.5s
///   damagePerTick = 5
///   duration      = 3s
///
/// At BurnStrength 1.0 (100%) → 5 damage every 0.5s for 3s = 30 total
/// At BurnStrength 1.5 (150%) → 7.5 damage every 0.5s for 3s = 45 total
///
/// Reapplying refreshes the duration but NOT the tick timer — under sustained
/// fire (several hits per second) the burn keeps ticking instead of being
/// reset to 0 before its first tick.
/// </summary>
public class BurnEffect : StatusEffect
{
    [Header("Burn Base Values")]
    public float baseDamagePerTick = 5f;
    public float tickInterval = 0.5f;
    public float baseDuration = 3f;

    private float tickTimer;

    public override StatusType Type => StatusType.Burn;
    protected override Color TintColor => new Color(1f, 0.4f, 0f);   // orange
    protected override ActionFX ApplyFX => ActionFX.StatusBurnApply;

    protected override void OnApplied()
    {
        duration = baseDuration;
        timeRemaining = baseDuration;
        tickTimer = 0f;
    }

    protected override void OnTick(float dt)
    {
        tickTimer += dt;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;

        // Damage scales with BurnStrength
        float strength = playerStats != null ? playerStats.burnStrength : 1f;
        float dmg = baseDamagePerTick * strength;
        int roundedDmg = Mathf.Max(1, Mathf.RoundToInt(dmg));

        FXManager.Play(ActionFX.StatusBurnTick, transform.position);
        if (upgradeManager != null) upgradeManager.BurnTick(gameObject, dmg);
        if (playerStats != null) playerStats.RecordDamageDealt(dmg, DamageSource.Status, gameObject);

        // Damage last — it may kill the enemy and destroy this component
        if (enemy != null)
        {
            enemy.TakeDamage(roundedDmg, transform.position, Vector3.zero, 0f, false,
                             FloatingTextManager.HitType.BurnTick);
        }
    }
}
