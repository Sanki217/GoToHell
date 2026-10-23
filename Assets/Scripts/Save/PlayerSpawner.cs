using UnityEngine;

/// <summary>
/// Spawns the class-specific player prefab at this object's position when the
/// gameplay scene loads. Place at the level's start point.
///
/// Prefab resolution:
///   1. RunConfig.selectedClassId → that ClassDefinition's playerPrefab.
///   2. Otherwise (testing the scene directly): defaultPlayerPrefab.
///   3. If a player already exists in the scene (hand-placed for testing),
///      nothing is spawned at all.
///
/// Runs at execution order -200 so the player (and PlayerRefs) exist before
/// any scene Start() runs — CameraFollow, GameStartSequence, HUD bindings etc.
/// all resolve via PlayerRefs afterwards.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Prefab used when no run/class is selected (testing the scene directly).")]
    public GameObject defaultPlayerPrefab;

    private void Awake()
    {
        // A hand-placed player wins — nothing to spawn
        if (Object.FindFirstObjectByType<PlayerStats>() != null) return;

        GameObject prefab = ResolvePrefab();
        if (prefab == null)
        {
            Debug.LogError("[PlayerSpawner] No player prefab to spawn — assign defaultPlayerPrefab " +
                           "or set playerPrefab on the ClassDefinitions.");
            return;
        }

        Instantiate(prefab, transform.position, Quaternion.identity);
    }

    private GameObject ResolvePrefab() => PrefabFor(RunConfig.I != null ? RunConfig.I.selectedClassId : "", defaultPlayerPrefab);

    /// <summary>The class's player prefab, or <paramref name="fallback"/>.</summary>
    public static GameObject PrefabFor(string classId, GameObject fallback)
    {
        if (!string.IsNullOrEmpty(classId))
        {
            ClassDefinition def = Content.Classes.Get(classId);
            if (def != null)
            {
                if (def.playerPrefab != null) return def.playerPrefab;
                Debug.LogWarning($"[PlayerSpawner] Class '{classId}' has no playerPrefab — using default.");
            }
        }
        return fallback;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
    }
}
