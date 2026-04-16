using UnityEngine;

/// <summary>
/// Any component that wants to be notified when the Enemy it sits on gets
/// knocked back should implement this interface. Enemy.TakeDamage iterates
/// all IKnockbackReceivers on the GameObject and delegates to them.
///
/// Why an interface:
///   Previously Enemy.cs hard-coded GetComponent&lt;EnemyPatrolHorizontal&gt;(),
///   GetComponent&lt;EnemyPatrolVertical&gt;(), GetComponent&lt;EnemyShooter&gt;(),
///   GetComponent&lt;EnemyWallJumper&gt;() — adding any new enemy type required
///   editing Enemy.cs. Now enemy types are open-ended.
/// </summary>
public interface IKnockbackReceiver
{
    /// <summary>
    /// Apply a knockback impulse over the given duration. The receiver is
    /// responsible for suspending its own movement logic during this window
    /// and resuming gracefully afterward.
    /// </summary>
    /// <param name="impulse">Total world-space displacement to cover over duration (ease-out is typical).</param>
    /// <param name="duration">Seconds the knockback should take.</param>
    void ReceiveKnockback(Vector3 impulse, float duration);
}
