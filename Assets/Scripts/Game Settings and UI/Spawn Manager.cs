using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sequential spawn queue. Processes SpawnAreas one-by-one in spawnOrder.
/// Each area receives and adds to the global SpawnRecord registry so no
/// object ever overlaps another regardless of which spawner placed it.
///
/// SCENE-SCOPED: auto-created when the first SpawnArea registers (Awake),
/// spawns once two frames later, and dies with the scene. Scenes without
/// SpawnAreas (menus, Collection, creator) never create one — no
/// DontDestroyOnLoad, no sceneLoaded hooks, no scene scans.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    private static SpawnManager instance;

    private readonly List<SpawnArea> areas = new List<SpawnArea>();
    private bool spawnScheduled;

    // ================================================================
    //  STATIC API (called by SpawnArea)
    // ================================================================

    public static void Register(SpawnArea area)
    {
        if (area == null) return;

        if (instance == null)
            instance = new GameObject("[SpawnManager]").AddComponent<SpawnManager>();

        if (!instance.areas.Contains(area))
            instance.areas.Add(area);

        instance.ScheduleSpawn();
    }

    public static void Unregister(SpawnArea area)
    {
        if (instance != null && area != null)
            instance.areas.Remove(area);
    }

    // ================================================================
    //  SPAWN
    // ================================================================

    private void ScheduleSpawn()
    {
        if (spawnScheduled) return;
        spawnScheduled = true;
        StartCoroutine(SpawnAllCoroutine());
    }

    private IEnumerator SpawnAllCoroutine()
    {
        // Wait two frames so every SpawnArea Awake + Start has run and registered
        yield return null;
        yield return null;

        var sorted = new List<SpawnArea>(areas);
        sorted.Sort((a, b) => a.spawnOrder.CompareTo(b.spawnOrder));

        // Single global registry — all spawners share it
        var registry = new List<SpawnRecord>();

        Debug.Log($"[SpawnManager] Starting sequential spawn ({sorted.Count} areas)");

        foreach (var area in sorted)
        {
            if (area == null) continue;
            area.SpawnObjects(registry);
            yield return new WaitForFixedUpdate();
        }

        Debug.Log($"[SpawnManager] Done. {registry.Count} objects registered.");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
