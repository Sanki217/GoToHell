using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EnemySpawnArea — extends SpawnArea.
/// Legacy field migration moved to Awake so fields are ready before SpawnManager calls us.
/// </summary>
public class EnemySpawnArea : SpawnArea
{
    [Header("Legacy fields — prefer wallLayers/wallCheckRadius on base class")]
    public float collisionCheckRadius = 0.5f;
    public LayerMask collisionMask;

    protected override void Awake()
    {
        // Migrate legacy fields before base.Awake() registers with SpawnManager
        if (wallLayers == 0 && collisionMask != 0)
            wallLayers = collisionMask;
        if (wallCheckRadius <= 0f && collisionCheckRadius > 0f)
            wallCheckRadius = collisionCheckRadius;

        base.Awake();
    }

    // No SpawnObjects override needed — base class handles everything.

    /// <summary>Block legacy no-arg call.</summary>
    public override void SpawnObjects() { }
}