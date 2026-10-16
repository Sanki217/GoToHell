using UnityEngine;

/// <summary>
/// Holy: after a delay, detonates for a burst of damage.
/// Both the delay AND the damage scale with PlayerStats.holyStrength.
/// Higher strength = faster detonation AND more damage.
///
/// Base values:
///   baseDamage = 20
///   baseDelay  = 2.5s
///
/// At HolyStrength 1.0 → 20 damage after 2.5s
/// At HolyStrength 2.0 → 40 damage after 1.25s (faster AND harder)
///
/// Reapplying while the countdown runs does NOT restart it — otherwise
/// sustained fire would postpone the detonation forever. Reapplying after the
/// detonation arms a fresh countdown.
/// </summary>
public class HolyEffect : StatusEffect
{
    [Header("Holy Base Values")]
    public float baseDamage = 20f;
    public float baseDelay = 2.5f;

    private float detonateTimer;
    private bool hasDetonated;

    public override StatusType Type => StatusType.Holy;
    protected override Color TintColor => new Color(1f, 0.95f, 0.6f);   // golden glow
    protected override ActionFX ApplyFX => ActionFX.StatusHolyApply;

    private float Strength => playerStats != null ? Mathf.Max(0.01f, playerStats.holyStrength) : 1f;

    protected override void OnApplied() => Arm();

    protected override void OnRefreshed()
    {
        if (hasDetonated) Arm();
        else timeRemaining = detonateTimer + 0.1f;   // keep the running countdown
    }

    private void Arm()
    {
        detonateTimer = baseDelay / Strength;
        duration = detonateTimer + 0.1f;   // status expires just after detonation
        timeRemaining = duration;
        hasDetonated = false;
    }

    protected override void OnTick(float dt)
    {
        if (hasDetonated) return;

        detonateTimer -= dt;
        if (detonateTimer <= 0f)
            Detonate();
    }

    private void Detonate()
    {
        hasDetonated = true;

        float finalDmg = baseDamage * Strength;
        int roundedDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmg));

        FXManager.Play(ActionFX.StatusHolyDetonate, transform.position);
        if (upgradeManager != null) upgradeManager.HolyDetonated(gameObject, finalDmg);
        if (playerStats != null) playerStats.RecordDamageDealt(finalDmg, DamageSource.Status, gameObject);

        // Damage last — it may kill the enemy and destroy this component
        if (enemy != null)
        {
            enemy.TakeDamage(roundedDmg, transform.position, Vector3.zero, 0f, false,
                             FloatingTextManager.HitType.HolyDetonate);
        }
    }
}
