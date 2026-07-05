using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prefab-keyed object pool. Replaces Instantiate/Destroy for high-volume
/// short-lived objects (floating text, souls, projectiles) to eliminate
/// GC allocation spikes during combat.
///
/// Usage:
///   GameObject go = Pool.Spawn(prefab, pos, rot);   // instead of Instantiate
///   Pool.Despawn(go);                               // instead of Destroy
///
/// Rules for pooled prefabs:
///   • Reset per-use state in OnEnable (it runs on every reuse; Awake/Start run once).
///   • Never call Destroy on a pooled instance — always Pool.Despawn.
///   • Despawn on a non-pooled instance safely falls back to Destroy.
///
/// Pools clear automatically on scene unload (instances are scene objects
/// and die with the scene).
/// </summary>
public static class Pool
{
    private static readonly Dictionary<GameObject, Stack<GameObject>> pools =
        new Dictionary<GameObject, Stack<GameObject>>();

    private static bool hooked;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        EnsureHooked();

        if (!pools.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pools[prefab] = stack;
        }

        // Pop until we find a live instance (scene switches can kill pooled objects)
        GameObject go = null;
        while (stack.Count > 0 && go == null)
            go = stack.Pop();

        if (go == null)
        {
            go = Object.Instantiate(prefab, position, rotation);
            PooledInstance marker = go.AddComponent<PooledInstance>();
            marker.sourcePrefab = prefab;
        }
        else
        {
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
        }

        return go;
    }

    public static void Despawn(GameObject instance)
    {
        if (instance == null) return;

        PooledInstance marker = instance.GetComponent<PooledInstance>();
        if (marker == null || marker.sourcePrefab == null)
        {
            // Not pooled (e.g. hand-placed in the scene) — plain destroy
            Object.Destroy(instance);
            return;
        }

        instance.SetActive(false);

        if (!pools.TryGetValue(marker.sourcePrefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pools[marker.sourcePrefab] = stack;
        }
        stack.Push(instance);
    }

    private static void EnsureHooked()
    {
        if (hooked) return;
        hooked = true;
        SceneManager.sceneUnloaded += _ => pools.Clear();
    }
}
