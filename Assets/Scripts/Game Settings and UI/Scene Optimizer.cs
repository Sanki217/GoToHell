using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Scene Optimizer — improves performance by disabling objects outside
/// the visible and relevant area around the player.
///
/// Two systems:
///
/// 1. ENEMY SLEEP
///    Enemies far from the player have their movement scripts (EnemyPatrol,
///    EnemyPatrolVertical, EnemyShooter) disabled. They wake up when the
///    player gets close enough.
///
/// 2. RENDERER CULLING
///    Disables Renderer components on non-enemy objects outside the camera
///    frustum. Unity culls draw calls automatically, but this also stops
///    shadow casting and other renderer overhead.
///
/// Toggle "Enable Optimizer" on/off at runtime to compare performance.
/// </summary>
public class SceneOptimizer : MonoBehaviour
{
    [Header("Master Toggle")]
    public bool enableOptimizer = true;

    [Header("Enemy Sleep")]
    public bool enableEnemySleep = true;
    [Tooltip("Enemies within this distance are fully active.")]
    public float enemyWakeRadius = 40f;
    [Tooltip("Enemies beyond this distance are put to sleep.")]
    public float enemySleepRadius = 55f;

    [Header("Renderer Culling")]
    public bool enableRendererCulling = true;
    [Tooltip("Extra margin beyond camera frustum before culling (world units).")]
    public float rendererCullMargin = 5f;

    [Header("Update Rate")]
    [Range(1, 30)]
    [Tooltip("How many times per second distances are checked.")]
    public int checksPerSecond = 10;

    [Header("Debug")]
    public bool showGizmos = true;

    [Header("References")]
    public Transform playerTransform;

    // ================================================================
    //  PRIVATE
    // ================================================================

    private Camera mainCamera;

    private readonly HashSet<GameObject> sleepingEnemies = new HashSet<GameObject>();
    private readonly HashSet<Renderer> culledRenderers = new HashSet<Renderer>();

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        mainCamera = Camera.main;

        if (playerTransform == null)
            playerTransform = PlayerRefs.I?.T;

        StartCoroutine(OptimizeLoop());
    }

    // ================================================================
    //  MAIN LOOP
    // ================================================================

    private IEnumerator OptimizeLoop()
    {
        float interval = 1f / Mathf.Max(1, checksPerSecond);

        while (true)
        {
            yield return new WaitForSeconds(interval);

            if (!enableOptimizer || playerTransform == null) continue;

            if (enableEnemySleep) UpdateEnemySleep();
            if (enableRendererCulling) UpdateRendererCulling();
        }
    }

    // ================================================================
    //  ENEMY SLEEP
    // ================================================================

    private void UpdateEnemySleep()
    {
        Vector3 playerPos = playerTransform.position;

        // Find all enemies by tag — cheaper than FindObjectsByType
        GameObject[] allEnemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var go in allEnemyObjects)
        {
            if (go == null) continue;

            float dist = Vector3.Distance(go.transform.position, playerPos);
            bool sleeping = sleepingEnemies.Contains(go);

            if (!sleeping && dist > enemySleepRadius)
                Sleep(go);
            else if (sleeping && dist < enemyWakeRadius)
                Wake(go);
        }

        sleepingEnemies.RemoveWhere(go => go == null);
    }

    private void Sleep(GameObject go)
    {
        if (sleepingEnemies.Contains(go)) return;
        sleepingEnemies.Add(go);

        SetEnemyScriptsEnabled(go, false);
    }

    private void Wake(GameObject go)
    {
        if (!sleepingEnemies.Contains(go)) return;
        sleepingEnemies.Remove(go);

        SetEnemyScriptsEnabled(go, true);
    }

    private void SetEnemyScriptsEnabled(GameObject go, bool enabled)
    {
        var hPatrol = go.GetComponent<EnemyPatrolHorizontal>();
        var vPatrol = go.GetComponent<EnemyPatrolVertical>();
        var shooter = go.GetComponent<EnemyShooter>();

        if (hPatrol != null) hPatrol.enabled = enabled;
        if (vPatrol != null) vPatrol.enabled = enabled;
        if (shooter != null) shooter.enabled = enabled;
    }

    // ================================================================
    //  RENDERER CULLING
    // ================================================================

    private void UpdateRendererCulling()
    {
        if (mainCamera == null) return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            if (r.GetComponentInParent<Enemy>() != null) continue;  // enemies handled by sleep
            if (r is SpriteRenderer) continue;

            Bounds bounds = r.bounds;
            bounds.Expand(rendererCullMargin * 2f);

            bool visible = GeometryUtility.TestPlanesAABB(planes, bounds);

            if (visible && !r.enabled)
            {
                r.enabled = true;
                culledRenderers.Remove(r);
            }
            else if (!visible && r.enabled)
            {
                r.enabled = false;
                culledRenderers.Add(r);
            }
        }

        culledRenderers.RemoveWhere(r => r == null);
    }

    // ================================================================
    //  WAKE ALL
    // ================================================================

    public void WakeAll()
    {
        foreach (var go in sleepingEnemies)
            if (go != null) SetEnemyScriptsEnabled(go, true);
        sleepingEnemies.Clear();

        foreach (var r in culledRenderers)
            if (r != null) r.enabled = true;
        culledRenderers.Clear();
    }

    // ================================================================
    //  GIZMOS
    // ================================================================

    private void OnDrawGizmos()
    {
        if (!showGizmos || playerTransform == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(playerTransform.position, enemyWakeRadius);

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.10f);
        Gizmos.DrawWireSphere(playerTransform.position, enemySleepRadius);
    }
}