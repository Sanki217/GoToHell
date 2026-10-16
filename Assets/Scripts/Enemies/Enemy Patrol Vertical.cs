using UnityEngine;

/// <summary>Patrols up/down a shaft. All logic lives in EnemyPatrolBase.</summary>
public class EnemyPatrolVertical : EnemyPatrolBase
{
    protected override Vector3 Axis => Vector3.up;

    private void Reset()
    {
        turnBoxHalfExtents = new Vector3(0.45f, 0.35f, 0.1f);
        knockbackCastHalf = new Vector3(0.4f, 0.3f, 0.1f);
    }
}
