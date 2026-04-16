using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sequential spawn queue manager.
///
/// FLOW:
///   1. On scene load, collects all SpawnArea instances.
///   2. Sorts them by SpawnArea.spawnOrder (lower first, scene order as tiebreaker).
///   3. Processes them one-by-one in a coroutine — each spawner gets the full
///      global list of already-placed positions, so nothing overlaps.
///   4. Waits one fixed-update frame between spawners to let physics settle.
///
/// All spawners now share collision validation (SpawnArea base class).
/// EnemySpawnArea no longer needs its own special path — it uses the same
/// SpawnObjects(globalPositions) method as everything else.
///
/// SETUP:
///   - Set SpawnArea.spawnOrder in Inspector (lower = earlier).
///   - Set SpawnArea.wallCheckRadius + wallLayers to avoid spawning inside walls.
///   - Set SpawnArea.minSeparationDistance to control spacing.
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

    // ---------- Static API ----------
    public static void Register(SpawnArea area) => Instance.InternalRegister(area);
    public static void Unregister(SpawnArea area) => Instance.InternalUnregister(area);

    private void InternalRegister(SpawnArea area)
    {
        if (area == null) return;
        if (!areas.Contains(area))
            areas.Add(area);
    }

    private void InternalUnregister(SpawnArea area)
    {
        if (area == null) return;
        areas.Remove(area);
    }

    // ---------- Scene lifecycle ----------
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        areas.Clear();

        foreach (var area in FindObjectsByType<SpawnArea>(FindObjectsSortMode.None))
            InternalRegister(area);

        StartCoroutine(SpawnAllCoroutine());
    }

    // ---------- Sequential spawning ----------
    private IEnumerator SpawnAllCoroutine()
    {
        // Sort by spawnOrder (stable — preserves scene order as tiebreaker)
        var sorted = new List<SpawnArea>(areas);
        sorted.Sort((a, b) => a.spawnOrder.CompareTo(b.spawnOrder));

        // Global position registry — every spawner contributes to and reads from this
        var globalPositions = new List<Vector3>();

        Debug.Log($"[SpawnManager] Starting sequential spawn queue ({sorted.Count} areas)");

        foreach (var area in sorted)
        {
            if (area == null) continue;

            area.SpawnObjects(globalPositions);

            // Let physics settle between spawners
            yield return new WaitForFixedUpdate();
        }

        Debug.Log($"[SpawnManager] SpawnAll completed. {globalPositions.Count} total objects placed.");
    }
}
