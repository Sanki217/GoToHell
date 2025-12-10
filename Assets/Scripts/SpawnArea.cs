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
        Orbs,
        Destructibles
    }

    [Header("Spawner Settings")]
    public SpawnType spawnType;
    public List<GameObject> prefabs;
    public int spawnCount = 5;
    public float minSeparationDistance = 1.5f;

    [Header("Rotation Settings (Euler Angles)")]
    public Vector3 minRotation = new Vector3(0f, 0f, 0f);
    public Vector3 maxRotation = new Vector3(0f, 360f, 0f);

    [Header("Scale Settings")]
    public Vector3 minScale = Vector3.one;
    public Vector3 maxScale = Vector3.one;

    private Collider area;

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

    public void SpawnObjects()
    {
        if (prefabs.Count == 0) return;

        Debug.Log($"Spawning {spawnCount} {spawnType} in area: {name}");

        List<Vector3> spawnedPositions = new List<Vector3>();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPoint = GetValidPoint(spawnedPositions);
            Quaternion randomRotation = GetRandomRotation();

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject spawned = Instantiate(prefab, randomPoint, randomRotation);

            spawnedPositions.Add(randomPoint);
        }
    }

    private Vector3 GetRandomPointInside()
    {
        Bounds b = area.bounds;

        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }

    private Vector3 GetValidPoint(List<Vector3> existingPoints)
    {
        const int MAX_ATTEMPTS = 30;

        for (int i = 0; i < MAX_ATTEMPTS; i++)
        {
            Vector3 point = GetRandomPointInside();

            bool tooClose = false;

            foreach (var p in existingPoints)
            {
                if (Vector3.Distance(point, p) < minSeparationDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return point;
        }

        // If all attempts failed, return a random point anyway
        return GetRandomPointInside();
    }

    private Quaternion GetRandomRotation()
    {
        float xRotation = Random.Range(minRotation.x, maxRotation.x);
        float yRotation = Random.Range(minRotation.y, maxRotation.y);
        float zRotation = Random.Range(minRotation.z, maxRotation.z);

        return Quaternion.Euler(xRotation, yRotation, zRotation);
    }
}
