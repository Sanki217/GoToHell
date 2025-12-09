using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    // A static list to hold all active SpawnArea instances.
    // Static makes it accessible from anywhere without a direct reference.
    private static List<SpawnArea> allSpawnAreas = new List<SpawnArea>();

    // Public static method for SpawnArea scripts to register themselves.
    public static void Register(SpawnArea area)
    {
        if (!allSpawnAreas.Contains(area))
        {
            allSpawnAreas.Add(area);
        }
    }

    // Public static method for SpawnArea scripts to unregister themselves (optional but good practice).
    public static void Unregister(SpawnArea area)
    {
        allSpawnAreas.Remove(area);
    }

    // This method is now safe and efficient as it iterates over a pre-built list.
    private void Start()
    {
        Debug.Log($"SpawnManager: Initializing spawn for {allSpawnAreas.Count} areas.");

        // Use a standard 'for' loop for safe iteration, or a temporary copy if you 
        // anticipate the list changing during iteration (less common here).
        foreach (var area in allSpawnAreas)
        {
            if (area != null)
            {
                area.SpawnObjects();
            }
        }

        // Optional: Clear the list if the SpawnManager is responsible for the object lifecycle.
        // For a dungeon/level generator, you might want to keep the list for later use.
        // allSpawnAreas.Clear();
    }
}