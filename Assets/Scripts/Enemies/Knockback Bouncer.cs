using UnityEngine;

/// <summary>
/// Static helper that handles the "move by a step, bounce off walls if you hit one"
/// routine shared by every enemy's knockback coroutine.
///
/// Previously this logic was copy-pasted into EnemyPatrolHorizontal,
/// EnemyPatrolVertical, EnemyShooter, and EnemyWallJumper — four near-identical
/// copies of the same ~30 lines. Now they all call StepWithWallBounce(...).
/// </summary>
public static class KnockbackBouncer
{
    /// <summary>
    /// Given an intended displacement <paramref name="step"/>, returns a possibly-
    /// modified displacement that stops at walls and bounces off with damping.
    /// If the path is clear, returns step unchanged.
    /// </summary>
    /// <param name="step">Requested per-frame displacement.</param>
    /// <param name="origin">Current world position (usually transform.position).</param>
    /// <param name="halfExtents">Half-extents of the BoxCast used for wall detection.</param>
    /// <param name="bounceDamping">0..1 — how much of the remaining velocity survives the bounce.</param>
    /// <param name="mask">Layers considered solid walls/ground.</param>
    /// <param name="selfColliders">Colliders on this enemy that should be ignored.</param>
    public static Vector3 StepWithWallBounce(
        Vector3 step,
        Vector3 origin,
        Vector3 halfExtents,
        float bounceDamping,
        LayerMask mask,
        Collider[] selfColliders)
    {
        if (step.sqrMagnitude < 0.00001f) return step;

        float dist = step.magnitude;
        Vector3 dir = step / dist;

        bool found = Physics.BoxCast(
            origin, halfExtents, dir,
            out RaycastHit hit, Quaternion.identity, dist,
            mask, QueryTriggerInteraction.Ignore);

        if (!found) return step;

        // Ignore our own colliders
        if (selfColliders != null)
        {
            foreach (var sc in selfColliders)
                if (sc == hit.collider) return step;
        }

        float safe = Mathf.Max(0f, hit.distance - 0.05f);
        Vector3 safeStep = dir * safe;
        Vector3 remaining = step - safeStep;
        Vector3 normal = hit.normal; normal.z = 0f;

        if (normal.sqrMagnitude > 0.001f)
        {
            Vector3 reflected = Vector3.Reflect(remaining, normal.normalized) * bounceDamping;
            reflected.z = 0f;
            return safeStep + reflected;
        }

        return safeStep;
    }
}
