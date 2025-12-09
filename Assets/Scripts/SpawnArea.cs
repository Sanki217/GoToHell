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

    [Header("Rotation Settings (Euler Angles)")]
    public Vector3 minRotation = new Vector3(0f, 0f, 0f);
    public Vector3 maxRotation = new Vector3(0f, 360f, 0f);

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

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPoint = GetRandomPointInside();
            Quaternion randomRotation = GetRandomRotation();

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject spawned = Instantiate(prefab, randomPoint, randomRotation, null);


            
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

   

    private Quaternion GetRandomRotation()
    {
        float xRotation = Random.Range(minRotation.x, maxRotation.x);
        float yRotation = Random.Range(minRotation.y, maxRotation.y);
        float zRotation = Random.Range(minRotation.z, maxRotation.z);

        // Convert the random Euler angles into a Quaternion
        return Quaternion.Euler(xRotation, yRotation, zRotation);
    }
}