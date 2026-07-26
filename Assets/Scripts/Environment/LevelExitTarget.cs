using UnityEngine;

/// <summary>
/// End-of-level target: the FIRST point of damage from the player ends the
/// layer — exactly like walking into a LevelExit trigger (victory summary on
/// the final/last built layer, next layer otherwise). Put it in the merchant
/// zone in place of the walk-in exit.
///
/// Extends Enemy so every weapon (arrow, slash, dash, explosions, statuses)
/// hits it through the standard damage path.
///
/// SETUP:
///   1. Create the target object, TAG IT "Enemy", add a solid collider.
///   2. Add this component.
///   3. Remove (or disable) the old LevelExit trigger volume in the scene.
/// </summary>
public class LevelExitTarget : Enemy
{
    [Header("Feedback")]
    public float hitShakeMagnitude = 0.25f;

    private bool used;

    public override void TakeDamage(int amount, Vector3 hitPosition,
                                    Vector3 knockbackDir, float knockbackForce,
                                    bool isCrit, FloatingTextManager.HitType hitType)
    {
        if (used) return;
        used = true;

        PlayerRefs.CamFollow?.Shake(hitShakeMagnitude, 0.2f);
        LevelExit.CompleteLevel();
    }
}
