using UnityEngine;

/// <summary>
/// Freeze: slows enemy movement speed, attack speed, and projectile speed.
/// Enemy can still be damaged normally while frozen.
/// Slow % scales with PlayerStats.freezeStrength.
///
/// Base values:
///   slowPercent = 0.5 (50% slow at 1.0 strength)
///   duration    = 3s
///
/// At FreezeStrength 1.0 → 50% slow
/// At FreezeStrength 1.5 → 75% slow
/// Capped at 95% slow (enemy never fully stops)
/// </summary>
public class FreezeEffect : StatusEffect
{
    [Header("Freeze Base Values")]
    public float baseSlowPercent = 0.5f;   // 0.5 = 50% slow at strength 1.0
    public float baseDuration = 3f;
    public float maxSlowPercent = 0.95f;  // never fully stop the enemy

    private HorizontalMovement hMove;
    private VerticalMovement vMove;
    private Shooter shooter;

    private float originalHSpeed;
    private float originalVSpeed;
    private float originalShooterCooldown;
    private bool wasApplied = false;

    private Color freezeColor = new Color(0.5f, 0.85f, 1f); // light blue

    protected override void OnApplied()
    {
        duration = baseDuration;
        timeRemaining = baseDuration;

        hMove = GetComponent<HorizontalMovement>();
        vMove = GetComponent<VerticalMovement>();
        shooter = GetComponent<Shooter>();

        // Store original values before slowing
        if (hMove != null) originalHSpeed = hMove.maxSpeed;
        if (vMove != null) originalVSpeed = vMove.maxSpeed;
        if (shooter != null) originalShooterCooldown = shooter.attackCooldown;

        ApplySlow();
        SetTint(freezeColor);
        FXManager.Play(ActionFX.StatusFreezeApply, transform.position);
        upgradeManager?.StatusApplied(gameObject, StatusType.Freeze);
        playerStats?.RecordStatusApplied(StatusType.Freeze);
    }

    protected override void OnRefreshed()
    {
        // Re-apply slow in case strength changed
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
        float slowAmount = Mathf.Clamp(baseSlowPercent * strength, 0f, maxSlowPercent);
        float speedMult = 1f - slowAmount;

        if (hMove != null) hMove.maxSpeed = originalHSpeed * speedMult;
        if (vMove != null) vMove.maxSpeed = originalVSpeed * speedMult;
        if (shooter != null) shooter.attackCooldown = originalShooterCooldown / speedMult;

        wasApplied = true;
    }

    private void RestoreSlow()
    {
        if (!wasApplied) return;
        if (hMove != null) hMove.maxSpeed = originalHSpeed;
        if (vMove != null) vMove.maxSpeed = originalVSpeed;
        if (shooter != null) shooter.attackCooldown = originalShooterCooldown;
    }

    private void OnDestroy()
    {
        // Safety: always restore on component destruction
        RestoreSlow();
        RestoreTint();
    }
}