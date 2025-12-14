using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // ---------- Spawning ----------
    private IEnumerator SpawnAllCoroutine()
    {
        var snapshot = new List<SpawnArea>(areas);

        Debug.Log($"[SpawnManager] Phase 1: Spawning non-enemies ({snapshot.Count} areas)");

        foreach (var area in snapshot)
        {
            if (area == null) continue;
            if (area.spawnType != SpawnArea.SpawnType.Enemies)
                area.SpawnObjects();
        }

        yield return new WaitForFixedUpdate();

        snapshot = new List<SpawnArea>(areas);

        Debug.Log("[SpawnManager] Phase 2: Spawning enemies");

        foreach (var area in snapshot)
        {
            if (area == null) continue;
            if (area.spawnType == SpawnArea.SpawnType.Enemies)
            {
                if (area is EnemySpawnArea enemyArea)
                    enemyArea.SpawnEnemiesAvoidingCollisions();
                else
                    area.SpawnObjects();
            }
        }

        Debug.Log("[SpawnManager] SpawnAll completed.");
    }
}
