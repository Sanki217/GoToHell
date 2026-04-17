using UnityEngine;

/// <summary>
/// Training Dummy Spawner — place this in your merchant zone.
///
/// Spawns a training dummy prefab at its own world position and automatically
/// re-spawns it 2 seconds after it dies. The dummy is a normal Enemy prefab
/// (set maxHealth to 1000 in its inspector). It does not move, does not attack,
/// and drops no souls (configure on the prefab).
///
/// SETUP:
///   1. Create a "Training Dummy" prefab with an Enemy component.
///      Set maxHealth = 1000, set soulPrefab = null and minimumSouls = 0.
///      Give it an appropriate sprite/mesh.
///   2. Place a new empty GameObject in the merchant zone scene.
///   3. Add this TrainingDummySpawner component to it.
///   4. Assign the Training Dummy prefab to the Dummy Prefab field.
///   5. Done — it will auto-spawn on Start and respawn 2 seconds after each death.
/// </summary>
public class TrainingDummySpawner : MonoBehaviour
{
    [Header("Training Dummy")]
    [Tooltip("Enemy prefab to spawn. Use a prefab with Enemy component, maxHealth 1000, no souls.")]
    public GameObject dummyPrefab;

    [Tooltip("Seconds to wait before respawning after the dummy dies.")]
    public float respawnDelay = 2f;

    // ================================================================
    //  STATE
    // ================================================================

    private GameObject currentDummy;
    private float respawnTimer = -1f;   // negative = not counting down

    // ================================================================
    //  LIFECYCLE
    // ================================================================

    private void Start()
    {
        if (dummyPrefab == null)
        {
            Debug.LogWarning("[TrainingDummySpawner] Dummy Prefab is not assigned!", this);
            return;
        }
        SpawnDummy();
    }

    private void Update()
    {
        // Detect death — Unity sets the reference to null after Destroy
        if (currentDummy == null && respawnTimer < 0f)
        {
            // Dummy was just destroyed — start the countdown
            respawnTimer = respawnDelay;
        }

        // Countdown
        if (respawnTimer >= 0f)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0f)
                SpawnDummy();
        }
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private void SpawnDummy()
    {
        if (dummyPrefab == null) return;

        currentDummy = Instantiate(
            dummyPrefab,
            new Vector3(transform.position.x, transform.position.y, 0f),
            Quaternion.identity);

        // Training dummies should not deal contact damage.
        // IMPORTANT: disabling the component does NOT stop OnTriggerEnter
        // in Unity — we must actually destroy the component.
        foreach (var dmg in currentDummy.GetComponentsInChildren<EnemyDamage>(true))
            Destroy(dmg);

        respawnTimer = -1f;
    }

    // ================================================================
    //  GIZMOS — shows spawn position in Scene view
    // ================================================================

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1.8f, 0.1f));

        UnityEditor.Handles.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.2f,
            $"Training Dummy\nRespawn: {respawnDelay}s");
    }
#endif
}
