using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpawnManager (MonoBehaviour) — automatically starts spawning on Start(),
/// but retains static Register/Unregister API. Runs Phase 1 (non-enemies),
/// yields one FixedUpdate to let physics register new colliders, then Phase 2.
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

    // Static API used by SpawnArea
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

    // Auto-start on scene Start
    private void Start()
    {
        // Wait a single frame before starting to allow all Awake/Start registrations to complete.
        // This helps if some SpawnArea register in Start().
        StartCoroutine(AutoStartRoutine());
    }

    private IEnumerator AutoStartRoutine()
    {
        // Give one frame for all SpawnAreas to register (Awake/Start sequence).
        yield return null;

        // Now start the two-phase spawn coroutine
        StartCoroutine(SpawnAllCoroutine());
    }

    // Public explicit entry if you prefer manual control
    public static void SpawnAll() => Instance.StartCoroutine(Instance.SpawnAllCoroutine());

    private IEnumerator SpawnAllCoroutine()
    {
        var snapshot = new List<SpawnArea>(areas);

        Debug.Log($"[SpawnManager] Phase 1: Spawning non-enemies ({snapshot.Count} areas snapshot)");
        foreach (var area in snapshot)
        {
            if (area == null) continue;
            if (area.spawnType != SpawnArea.SpawnType.Enemies)
            {
                try { area.SpawnObjects(); }
                catch (System.Exception ex) { Debug.LogError($"[SpawnManager] Exception while spawning area {area.name}: {ex}"); }
            }
        }

        // Wait for physics to register newly created colliders.
        yield return new WaitForFixedUpdate();

        // Re-snapshot in case registration changed the list
        snapshot = new List<SpawnArea>(areas);

        Debug.Log("[SpawnManager] Phase 2: Spawning enemies (collision-aware)");
        foreach (var area in snapshot)
        {
            if (area == null) continue;
            if (area.spawnType == SpawnArea.SpawnType.Enemies)
            {
                if (area is EnemySpawnArea enemyArea)
                {
                    try { enemyArea.SpawnEnemiesAvoidingCollisions(); }
                    catch (System.Exception ex) { Debug.LogError($"[SpawnManager] Exception while spawning enemies in {area.name}: {ex}"); }
                }
                else
                {
                    try { area.SpawnObjects(); }
                    catch (System.Exception ex) { Debug.LogError($"[SpawnManager] Exception while spawning enemies (fallback) in {area.name}: {ex}"); }
                }
            }
        }

        Debug.Log("[SpawnManager] SpawnAll completed.");
    }
}
