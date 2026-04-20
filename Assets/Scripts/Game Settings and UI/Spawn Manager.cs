using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sequential spawn queue. Processes SpawnAreas one-by-one in spawnOrder.
/// Each area receives and adds to the global SpawnRecord registry so no
/// object ever overlaps another regardless of which spawner placed it.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    private readonly List<SpawnArea> areas = new List<SpawnArea>();

    private static SpawnManager instance;
    private static SpawnManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SpawnManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SpawnManager>();
            }
            return instance;
        }
    }

    public static void Register(SpawnArea area) => Instance.InternalRegister(area);
    public static void Unregister(SpawnArea area) => Instance.InternalUnregister(area);

    private void InternalRegister(SpawnArea area)
    {
        if (area != null && !areas.Contains(area))
            areas.Add(area);
    }

    private void InternalUnregister(SpawnArea area)
    {
        if (area != null) areas.Remove(area);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        areas.Clear();
        StartCoroutine(SpawnAllCoroutine());
    }

    private IEnumerator SpawnAllCoroutine()
    {
        // Wait two frames so every SpawnArea Awake + Start has run
        yield return null;
        yield return null;

        foreach (var area in FindObjectsByType<SpawnArea>(FindObjectsSortMode.None))
            InternalRegister(area);

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
}