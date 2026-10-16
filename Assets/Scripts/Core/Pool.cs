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
///   Pool.Prewarm(prefab, 32);                        // optional, at level start
///
/// Rules for pooled prefabs:
///   • Reset per-use state in OnEnable (it runs on every reuse; Awake/Start run once).
///   • Never call Destroy on a pooled instance — always Pool.Despawn.
///   • Despawn on a non-pooled instance safely falls back to Destroy.
///   • Despawning the same instance twice is ignored (two triggers in one
///     physics step used to push it into the pool twice, so the same object
///     was handed out to two spawners).
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

        Stack<GameObject> stack = GetStack(prefab);

        // Pop until we find a live instance (scene switches can kill pooled objects)
        GameObject go = null;
        while (stack.Count > 0 && go == null)
            go = stack.Pop();

        if (go == null)
            return CreateInstance(prefab, position, rotation);

        if (go.TryGetComponent(out PooledInstance marker)) marker.inPool = false;
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    public static void Despawn(GameObject instance)
    {
        if (instance == null) return;

        if (!instance.TryGetComponent(out PooledInstance marker) || marker.sourcePrefab == null)
        {
            // Not pooled (e.g. hand-placed in the scene) — plain destroy
            Object.Destroy(instance);
            return;
        }

        if (marker.inPool) return;   // already returned this frame
        marker.inPool = true;

        instance.SetActive(false);
        GetStack(marker.sourcePrefab).Push(instance);
    }

    /// <summary>Instantiate <paramref name="count"/> inactive instances up front (avoids first-use hitches).</summary>
    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        EnsureHooked();

        Stack<GameObject> stack = GetStack(prefab);
        for (int i = stack.Count; i < count; i++)
        {
            GameObject go = CreateInstance(prefab, Vector3.zero, Quaternion.identity);
            go.GetComponent<PooledInstance>().inPool = true;
            go.SetActive(false);
            stack.Push(go);
        }
    }

    private static GameObject CreateInstance(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject go = Object.Instantiate(prefab, position, rotation);
        PooledInstance marker = go.AddComponent<PooledInstance>();
        marker.sourcePrefab = prefab;
        return go;
    }

    private static Stack<GameObject> GetStack(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pools[prefab] = stack;
        }
        return stack;
    }

    private static void EnsureHooked()
    {
        if (hooked) return;
        hooked = true;
        SceneManager.sceneUnloaded += _ => pools.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        pools.Clear();
        hooked = false;
    }
}
