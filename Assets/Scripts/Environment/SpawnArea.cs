using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawned object record — position + exclusion radius stored together cleanly.
/// Passed through the global registry so all spawners share awareness.
/// </summary>
public struct SpawnRecord
{
    public Vector3 position;
    public float exclusionRadius;   // half-size of the object + separation buffer

    public SpawnRecord(Vector3 pos, float radius)
    {
        position = pos;
        exclusionRadius = radius;
    }
}

/// <summary>
/// Base spawner. Handles all spawn types.
///
/// SEPARATION:
///   Uses the prefab's actual collider extents × its baked scale to get the
///   real world-space half-size. Two objects can never be placed closer than
///   (radiusA + radiusB). minSeparationDistance adds extra breathing room on top.
///
/// SCALE:
///   minScale/maxScale are multipliers on the prefab's baked scale.
///   Both at (1,1,1) = exact prefab scale, untouched.
///
/// FALLBACK:
///   If no valid point is found after MAX_ATTEMPTS, the slot is skipped.
///   spawnCount is a maximum, not a guarantee.
/// </summary>
public class SpawnArea : MonoBehaviour
{
    public enum SpawnType
    {
        Floors, Walls, Enemies, Magma, ArrowsOnWalls, Souls, Destructibles
    }

    [Header("Spawner Settings")]
    public SpawnType spawnType;
    public List<GameObject> prefabs;
    public int spawnCount = 5;

    [Tooltip("Extra gap between object edges on top of their sizes. 0 = objects touch.")]
    public float minSeparationDistance = 0.5f;

    [Header("Spawn Order")]
    [Tooltip("Lower = spawns first.")]
    public int spawnOrder = 0;

    [Header("Wall / Solid Check")]
    [Tooltip("Sphere radius to check for solid geometry at spawn point. ~half prefab width. 0 = skip.")]
    public float wallCheckRadius = 0.5f;
    [Tooltip("Layers considered solid.")]
    public LayerMask wallLayers;

    [Header("Rotation")]
    public Vector3 minRotation = Vector3.zero;
    public Vector3 maxRotation = new Vector3(0f, 360f, 0f);

    [Header("Scale — multipliers on prefab's baked scale. (1,1,1) = no change.")]
    public Vector3 minScale = Vector3.one;
    public Vector3 maxScale = Vector3.one;

    // ── internal ────────────────────────────────────────────────────
    protected Collider areaCollider;
    private static readonly Collider[] wallHitBuffer = new Collider[16];
    private readonly Dictionary<GameObject, float> prefabRadiusCache = new Dictionary<GameObject, float>();

    protected virtual void Awake()
    {
        areaCollider = GetComponent<Collider>();
        if (areaCollider != null) areaCollider.isTrigger = true;
        SpawnManager.Register(this);
    }

    private void OnDestroy() => SpawnManager.Unregister(this);

    // ================================================================
    //  MAIN SPAWN  — receives and adds to the global registry
    // ================================================================

    public virtual void SpawnObjects(List<SpawnRecord> registry)
    {
        if (prefabs == null || prefabs.Count == 0) return;

        Debug.Log($"[SpawnArea] '{name}' spawning up to {spawnCount} {spawnType} (order {spawnOrder})");

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            Vector3 prefabScale = prefab.transform.localScale;
            float prefabRadius = GetPrefabRadius(prefab, prefabScale);

            if (!TryGetValidPoint(registry, prefabRadius, out Vector3 point))
            {
                Debug.LogWarning($"[SpawnArea] '{name}': slot {i + 1}/{spawnCount} skipped — no valid position.");
                continue;
            }

            Quaternion rot = GetRandomRotation();
            GameObject obj = Instantiate(prefab, point, rot);

            // Apply scale multiplier only if inspector values differ from (1,1,1)
            if (minScale != Vector3.one || maxScale != Vector3.one)
            {
                Vector3 mult = GetRandomScale();
                obj.transform.localScale = new Vector3(
                    prefabScale.x * mult.x,
                    prefabScale.y * mult.y,
                    prefabScale.z * mult.z);
            }

            // Register in global registry with correct exclusion radius
            registry.Add(new SpawnRecord(point, prefabRadius + minSeparationDistance));
            spawned++;
        }

        Debug.Log($"[SpawnArea] '{name}': placed {spawned}/{spawnCount}");
    }

    // Legacy no-arg overload
    public virtual void SpawnObjects() => SpawnObjects(new List<SpawnRecord>());

    // ================================================================
    //  VALID POINT SEARCH
    // ================================================================

    private bool TryGetValidPoint(List<SpawnRecord> registry, float selfRadius, out Vector3 result)
    {
        const int MAX_ATTEMPTS = 60;

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            Vector3 candidate = GetRandomPointInside();

            // Wall / solid geometry check
            if (wallCheckRadius > 0f && wallLayers != 0)
            {
                int hits = Physics.OverlapSphereNonAlloc(
                    candidate, wallCheckRadius, wallHitBuffer,
                    wallLayers, QueryTriggerInteraction.Ignore);
                if (hits > 0) continue;
            }

            // Separation check — candidate must not overlap any registered object
            bool tooClose = false;
            foreach (SpawnRecord rec in registry)
            {
                // Minimum distance = sum of both objects' radii (no double-counting)
                float minDist = selfRadius + rec.exclusionRadius;
                if (Vector3.Distance(candidate, rec.position) < minDist)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            result = candidate;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // ================================================================
    //  PREFAB RADIUS — collider extents × prefab scale = real world size
    // ================================================================

    private float GetPrefabRadius(GameObject prefab, Vector3 scale)
    {
        if (prefab == null) return 0.5f;

        // Cache key includes scale so different scale multipliers get correct radii
        // For now cache by prefab reference (scale is baked into prefab itself)
        if (prefabRadiusCache.TryGetValue(prefab, out float cached)) return cached;

        float radius = 0.5f;

        // Read from colliders on the prefab asset (no instantiation needed)
        Collider[] cols = prefab.GetComponentsInChildren<Collider>();
        if (cols.Length > 0)
        {
            float maxExtent = 0f;
            foreach (Collider col in cols)
            {
                // Get local-space extent, then multiply by the prefab's own scale
                float ext = GetColliderLocalExtent(col, scale);
                if (ext > maxExtent) maxExtent = ext;
            }
            if (maxExtent > 0f) radius = maxExtent;
        }
        else
        {
            // No colliders — fall back to renderer local bounds × scale
            Renderer[] rends = prefab.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                float maxExtent = 0f;
                foreach (Renderer rend in rends)
                {
                    // localBounds is in local space before scale is applied
                    Vector3 extents = rend.localBounds.extents;
                    float ext = Mathf.Max(
                        extents.x * Mathf.Abs(scale.x),
                        extents.y * Mathf.Abs(scale.y));
                    if (ext > maxExtent) maxExtent = ext;
                }
                if (maxExtent > 0f) radius = maxExtent;
            }
        }

        prefabRadiusCache[prefab] = radius;
        return radius;
    }

    private float GetColliderLocalExtent(Collider col, Vector3 scale)
    {
        if (col is BoxCollider box)
        {
            // Box size is in local space — multiply by scale to get world extent
            float wx = box.size.x * 0.5f * Mathf.Abs(scale.x);
            float wy = box.size.y * 0.5f * Mathf.Abs(scale.y);
            return Mathf.Max(wx, wy);
        }
        if (col is SphereCollider sphere)
        {
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return sphere.radius * maxScale;
        }
        if (col is CapsuleCollider capsule)
        {
            float r = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float h = capsule.height * 0.5f * Mathf.Abs(scale.y);
            return Mathf.Max(r, h);
        }
        // MeshCollider or unknown — safe default
        return 0.5f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    public Vector3 GetRandomPointInside()
    {
        if (areaCollider == null) return transform.position;
        Bounds b = areaCollider.bounds;
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z));
    }

    public Quaternion GetRandomRotation() => Quaternion.Euler(
        Random.Range(minRotation.x, maxRotation.x),
        Random.Range(minRotation.y, maxRotation.y),
        Random.Range(minRotation.z, maxRotation.z));

    public Vector3 GetRandomScale() => new Vector3(
        Random.Range(minScale.x, maxScale.x),
        Random.Range(minScale.y, maxScale.y),
        Random.Range(minScale.z, maxScale.z));
}