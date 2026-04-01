using UnityEngine;

/// <summary>
/// Freeze: slows enemy movement and attack speed.
/// Works with the new EnemyPatrol, EnemyPatrolVertical, and EnemyShooter scripts.
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

    private EnemyPatrolHorizontal hPatrol;
    private EnemyPatrolVertical vPatrol;
    private EnemyShooter shooter;

    private float originalHSpeed;
    private float originalVSpeed;
    private float originalShooterCooldown;
    private bool wasApplied = false;

    private Color freezeColor = new Color(0.5f, 0.85f, 1f);

    protected override void OnApplied()
    {
        duration = baseDuration;
        timeRemaining = baseDuration;

        hPatrol = GetComponent<EnemyPatrolHorizontal>();
        vPatrol = GetComponent<EnemyPatrolVertical>();
        shooter = GetComponent<EnemyShooter>();

        if (hPatrol != null) originalHSpeed = hPatrol.speed;
        if (vPatrol != null) originalVSpeed = vPatrol.speed;
        if (shooter != null) originalShooterCooldown = shooter.attackCooldown;

        ApplySlow();
        SetTint(freezeColor);
        FXManager.Play(ActionFX.StatusFreezeApply, transform.position);
        upgradeManager?.StatusApplied(gameObject, StatusType.Freeze);
        playerStats?.RecordStatusApplied(StatusType.Freeze);
    }

    protected override void OnRefreshed()
    {
        RestoreSlow();
        ApplySlow();
    }

    protected override void OnTick(float dt)
    {
        FXManager.Play(ActionFX.StatusFreezeTick, transform.position);
        upgradeManager?.FreezeTick(gameObject);
    }

    protected override void OnExpired()
    {
        RestoreSlow();
        RestoreTint();
        Destroy(this);
    }

    private void ApplySlow()
    {
        float strength = playerStats != null ? playerStats.freezeStrength : 1f;
        float slow = Mathf.Clamp(baseSlowPercent * strength, 0f, maxSlowPercent);
        float mult = 1f - slow;

        // Apply via speedMultiplier field so original speed is preserved
        if (hPatrol != null) hPatrol.speedMultiplier = mult;
        if (vPatrol != null) vPatrol.speedMultiplier = mult;
        if (shooter != null) shooter.attackCooldown = originalShooterCooldown / mult;

        wasApplied = true;
    }

    private void RestoreSlow()
    {
        if (!wasApplied) return;
        if (hPatrol != null) hPatrol.speedMultiplier = 1f;
        if (vPatrol != null) vPatrol.speedMultiplier = 1f;
        if (shooter != null) shooter.attackCooldown = originalShooterCooldown;
    }

    private void OnDestroy()
    {
        RestoreSlow();
        RestoreTint();
    }
}