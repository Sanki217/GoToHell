using UnityEngine;
using System.Collections.Generic;

public class SpawnArea : MonoBehaviour
{
    public enum SpawnType
    {
        Floors,
        Walls,
        Enemies,
        Magma,
        ArrowsOnWalls,
        Souls,
        Destructibles
    }

    [Header("Spawner Settings")]
    public SpawnType spawnType;
    public List<GameObject> prefabs;
    public int spawnCount = 5;
    public float minSeparationDistance = 1.5f;

    [Header("Spawn Order")]
    [Tooltip("Lower values spawn first. Spawners with the same order spawn in scene order.")]
    public int spawnOrder = 0;

    [Header("Collision Validation")]
    [Tooltip("Radius used to check for walls/solids at spawn point. 0 = skip wall check.")]
    public float wallCheckRadius = 0.5f;
    [Tooltip("Layers considered solid. Spawn points overlapping these are rejected.")]
    public LayerMask wallLayers;

    [Header("Rotation Settings (Euler Angles)")]
    public Vector3 minRotation = new Vector3(0f, 0f, 0f);
    public Vector3 maxRotation = new Vector3(0f, 360f, 0f);

    [Header("Scale Settings")]
    public Vector3 minScale = Vector3.one;
    public Vector3 maxScale = Vector3.one;

    private Collider area;

    private static readonly Collider[] wallCheckBuffer = new Collider[8];

    private void Awake()
    {
        area = GetComponent<Collider>();
        area.isTrigger = true;
        SpawnManager.Register(this);
    }

    private void OnDestroy()
    {
        SpawnManager.Unregister(this);
    }

    /// <summary>
    /// Spawns objects using the global position registry so every spawner
    /// respects every other spawner's placements.
    /// </summary>
    public virtual void SpawnObjects(List<Vector3> globalPositions)
    {
        if (prefabs.Count == 0) return;

        Debug.Log($"[SpawnManager] Spawning {spawnCount} {spawnType} in area: {name} (order {spawnOrder})");

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 point = GetValidPoint(globalPositions);
            Quaternion rotation = GetRandomRotation();

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            Instantiate(prefab, point, rotation);

            globalPositions.Add(point);
        }
    }

    /// <summary>Legacy overload — creates a temporary list (for backward compatibility).</summary>
    public virtual void SpawnObjects()
    {
        SpawnObjects(new List<Vector3>());
    }

    public Vector3 GetRandomPointInside()
    {
        Bounds b = area.bounds;

        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }

    public Vector3 GetValidPoint(List<Vector3> existingPoints)
    {
        const int MAX_ATTEMPTS = 40;

        for (int i = 0; i < MAX_ATTEMPTS; i++)
        {
            Vector3 point = GetRandomPointInside();

            // Distance check against all previously spawned objects (global)
            bool tooClose = false;
            foreach (var p in existingPoints)
            {
                if (Vector3.Distance(point, p) < minSeparationDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Wall/solid overlap check
            if (wallCheckRadius > 0f && wallLayers != 0)
            {
                int hits = Physics.OverlapSphereNonAlloc(
                    point, wallCheckRadius, wallCheckBuffer,
                    wallLayers, QueryTriggerInteraction.Ignore);
                if (hits > 0) continue;
            }

            return point;
        }

        // Fallback — still use a random point but warn
        Debug.LogWarning($"[SpawnArea] {name}: all {MAX_ATTEMPTS} attempts failed, using fallback position.");
        return GetRandomPointInside();
    }

    public Quaternion GetRandomRotation()
    {
        float xRotation = Random.Range(minRotation.x, maxRotation.x);
        float yRotation = Random.Range(minRotation.y, maxRotation.y);
        float zRotation = Random.Range(minRotation.z, maxRotation.z);

        return Quaternion.Euler(xRotation, yRotation, zRotation);
    }
}
