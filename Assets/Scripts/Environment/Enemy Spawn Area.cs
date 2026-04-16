using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EnemySpawnArea — extends SpawnArea but defers spawning to SpawnManager's
/// sequential queue. Wall/collision checking is inherited from SpawnArea
/// (wallCheckRadius + wallLayers). Set those in Inspector.
///
/// The old collisionMask/collisionCheckRadius fields are kept for backward
/// compatibility but now feed into the base class wallLayers/wallCheckRadius
/// if those haven't been set.
/// </summary>
public class EnemySpawnArea : SpawnArea
{
    [Header("Enemy Collision (legacy — prefer wallLayers on base)")]
    public float collisionCheckRadius = 0.5f;
    public LayerMask collisionMask;

    private void Start()
    {
        // Migrate legacy fields if base class fields aren't set
        if (wallLayers == 0 && collisionMask != 0) wallLayers = collisionMask;
        if (wallCheckRadius <= 0f && collisionCheckRadius > 0f) wallCheckRadius = collisionCheckRadius;
    }

    /// <summary>Block the no-arg legacy overload — SpawnManager calls SpawnObjects(globalPositions).</summary>
    public override void SpawnObjects() { }

    /// <summary>Enemies use the same validated spawn as everything else now.</summary>
    public override void SpawnObjects(List<Vector3> globalPositions)
    {
        if (prefabs.Count == 0) return;

        Debug.Log($"[SpawnManager] Spawning {spawnCount} ENEMIES (collision-safe) in: {name} (order {spawnOrder})");

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 point = GetValidPoint(globalPositions);
            Quaternion rotation = GetRandomRotation();
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            Instantiate(prefab, point, rotation);
            globalPositions.Add(point);
        }
    }
}
