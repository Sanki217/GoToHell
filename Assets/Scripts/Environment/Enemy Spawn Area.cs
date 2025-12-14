using UnityEngine;
using System.Collections.Generic;

public class EnemySpawnArea : SpawnArea
{
    [Header("Enemy Collision Settings")]
    public float collisionCheckRadius = 0.5f;
    public LayerMask collisionMask;
    // Assign environment, walls, floors, destructibles, other enemies, etc.

    public override void SpawnObjects()
    {
        // Prevent enemies from spawning in phase 1
        // They will be spawned manually by SpawnManager Phase 2
    }

    public void SpawnEnemiesAvoidingCollisions()
    {
        if (prefabs.Count == 0) return;

        Debug.Log($"Spawning {spawnCount} ENEMIES (collision-safe) in: {name}");

        List<Vector3> spawnedPositions = new List<Vector3>();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 point = GetValidEnemyPoint(spawnedPositions);

            Quaternion rotation = GetRandomRotation();
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];

            GameObject e = Instantiate(prefab, point, rotation);

            spawnedPositions.Add(point);
        }
    }

    private Vector3 GetValidEnemyPoint(List<Vector3> existing)
    {
        const int MAX_ATTEMPTS = 40;

        for (int i = 0; i < MAX_ATTEMPTS; i++)
        {
            Vector3 point = GetRandomPointInside();

            // distance check (reuse your logic)
            bool tooClose = false;
            foreach (var p in existing)
            {
                if (Vector3.Distance(point, p) < minSeparationDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // PHYSICS collision check
            Collider[] hits = Physics.OverlapSphere(point, collisionCheckRadius, collisionMask);
            if (hits.Length > 0)
                continue; // something already occupies space

            return point;
        }

        // fallback
        return GetRandomPointInside();
    }
}
