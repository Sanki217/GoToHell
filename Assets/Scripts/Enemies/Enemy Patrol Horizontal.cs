using UnityEngine;

/// <summary>Patrols left/right along the floor or a ledge. All logic lives in EnemyPatrolBase.</summary>
public class EnemyPatrolHorizontal : EnemyPatrolBase
{
    protected override Vector3 Axis => Vector3.right;

    private void Reset()
    {
        turnBoxHalfExtents = new Vector3(0.35f, 0.45f, 0.1f);
        knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    }
}
