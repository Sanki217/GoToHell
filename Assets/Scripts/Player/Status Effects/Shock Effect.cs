using UnityEngine;

/// <summary>
/// Shock: the enemy takes bonus damage on their NEXT hit only, then Shock is consumed.
/// Bonus damage scales with PlayerStats.shockStrength.
///
/// Base values:
///   bonusDamageMultiplier = 0.5 (50% bonus = 1.5× damage at strength 1.0)
///   duration = 5s (how long shock lasts before expiring unused)
///
/// At ShockStrength 1.0 → +50% bonus on next hit
/// At ShockStrength 2.0 → +100% bonus on next hit (double damage)
///
/// Usage: Enemy.TakeDamage() checks for ShockEffect and calls ConsumeShock().
/// </summary>
public class ShockEffect : StatusEffect
{
    [Header("Shock Base Values")]
    public float bonusDamageMultiplier = 0.5f;  // multiplied by ShockStrength
    public float baseDuration = 5f;

    private Color shockColor = new Color(1f, 0.95f, 0.2f); // yellow

    protected override void OnApplied()
    {
        duration = baseDuration;
        timeRemaining = baseDuration;

        SetTint(shockColor);
        FXManager.Play(ActionFX.StatusShockApply, transform.position);
        upgradeManager?.StatusApplied(gameObject, StatusType.Shock);
        playerStats?.RecordStatusApplied(StatusType.Shock);
    }

    protected override void OnExpired()
    {
        RestoreTint();
        Destroy(this);
    }

    /// <summary>
    /// Called by Enemy.TakeDamage when shock is present.
    /// Returns the bonus damage to add, then destroys itself.
    /// </summary>
    public float ConsumeShock(float incomingDamage)
    {
        float strength = playerStats != null ? playerStats.shockStrength : 1f;
        float bonusDmg = incomingDamage * bonusDamageMultiplier * strength;

        FXManager.Play(ActionFX.StatusShockConsume, transform.position);
        upgradeManager?.ShockConsumed(gameObject, bonusDmg);
        playerStats?.RecordDamageDealt(bonusDmg, DamageSource.Status, gameObject);

        RestoreTint();
        Destroy(this);
        return bonusDmg;
    }
}