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
/// </summary>
public class HolyEffect : StatusEffect
{
    [Header("Holy Base Values")]
    public float baseDamage = 20f;
    public float baseDelay = 2.5f;

    private float detonateTimer;
    private bool hasDetonated = false;

    private Color holyColor = new Color(1f, 0.95f, 0.6f); // golden glow

    protected override void OnApplied()
    {
        float strength = playerStats != null ? playerStats.holyStrength : 1f;
        // Higher strength = shorter delay
        detonateTimer = baseDelay / strength;
        duration = detonateTimer + 0.1f; // status expires just after detonation
        timeRemaining = duration;
        hasDetonated = false;

        SetTint(holyColor);
        FXManager.Play(ActionFX.StatusHolyApply, transform.position);
        upgradeManager?.StatusApplied(gameObject, StatusType.Holy);
        playerStats?.RecordStatusApplied(StatusType.Holy);
    }

    protected override void OnRefreshed()
    {
        // Restarting the countdown
        float strength = playerStats != null ? playerStats.holyStrength : 1f;
        detonateTimer = baseDelay / strength;
        duration = detonateTimer + 0.1f;
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

        float strength = playerStats != null ? playerStats.holyStrength : 1f;
        float finalDmg = baseDamage * strength;
        int roundedDmg = Mathf.Max(1, Mathf.RoundToInt(finalDmg));

        if (enemy != null)
        {
            enemy.TakeDamage(
                roundedDmg,
                transform.position,
                Vector3.zero,
                0f,
                false,
                FloatingTextManager.HitType.HolyDetonate
            );
        }

        FXManager.Play(ActionFX.StatusHolyDetonate, transform.position);
        upgradeManager?.HolyDetonated(gameObject, finalDmg);
        playerStats?.RecordDamageDealt(finalDmg, DamageSource.Status);
    }

    protected override void OnExpired()
    {
        RestoreTint();
        Destroy(this);
    }
}