using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Spawns stalker enemies around the player at random intervals — an
/// occasional danger, not a constant one. Put ONE on a manager object in the
/// gameplay scene; all tuning in the Inspector.
///
/// SPAWN POSITION: always far BELOW the player — he keeps falling toward
/// them, so they end up all around him instead of piling up above (and the
/// camera shows more below than above anyway).
///   • Y: spawnDistance + up to spawnDistanceExtra below him, pushed further
///     down by his fall speed × fallLeadTime so a fast fall can't outrun it.
///   • X: random within ±spawnSpreadX around him.
/// Z comes from the stalker itself (far = deep toward the camera), so it
/// rises in from depth.
///
/// TIMING: every checkInterval seconds one roll happens; spawnChance decides
/// if ONE stalker appears (shooterChance picks the variant). A successful
/// spawn is followed by spawnCooldown seconds of guaranteed quiet. Never more
/// than maxAlive at once — dead stalkers free their slot automatically.
/// </summary>
public class EnemyStalkerSpawner : MonoBehaviour, IDebugDrawable
{
    [Header("Prefabs")]
    public GameObject meleeStalkerPrefab;
    public GameObject shooterStalkerPrefab;
    [Range(0f, 1f)]
    [Tooltip("Chance a spawn is the shooter variant instead of melee.")]
    public float shooterChance = 0.4f;

    [Header("Timing")]
    [Tooltip("Seconds between spawn rolls.")]
    public float checkInterval = 4f;
    [Range(0f, 1f)]
    [Tooltip("Chance that a roll actually spawns a stalker.")]
    public float spawnChance = 0.25f;
    [Tooltip("Guaranteed quiet seconds after a successful spawn.")]
    public float spawnCooldown = 12f;
    [Tooltip("Hard cap of stalkers alive at once.")]
    public int maxAlive = 3;

    [Header("Spawn Position (relative to player)")]
    [FormerlySerializedAs("noSpawnXHalfWidth")]
    [Tooltip("Minimum distance below the player.")]
    public float spawnDistance = 18f;
    [FormerlySerializedAs("spawnXExtra")]
    [Tooltip("Extra random distance below spawnDistance.")]
    public float spawnDistanceExtra = 5f;
    [Tooltip("Random horizontal offset (±) around the player.")]
    public float spawnSpreadX = 12f;
    [Tooltip("Seconds of the player's fall added below the spawn point (0 = ignore fall speed).")]
    public float fallLeadTime = 0.75f;

    // ── state ───────────────────────────────────────────────────────
    private readonly List<GameObject> alive = new List<GameObject>();
    private float rollTimer;
    private float cooldownRemaining;
    private Vector3 lastPlayerPos;
    private float playerFallSpeed;   // units/s downward, smoothed
    private bool hasLastPos;

    private void Update()
    {
        TrackPlayerFall();

        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;

        rollTimer += Time.deltaTime;
        if (rollTimer < checkInterval) return;
        rollTimer = 0f;

        alive.RemoveAll(g => g == null);   // dead stalkers free their slot

        if (cooldownRemaining > 0f) return;
        if (alive.Count >= maxAlive) return;
        if (PlayerRefs.I == null) return;
        if (Random.value > spawnChance) return;

        Spawn();
        cooldownRemaining = spawnCooldown;
    }

    private void Spawn()
    {
        GameObject prefab = Random.value < shooterChance ? shooterStalkerPrefab : meleeStalkerPrefab;
        if (prefab == null) prefab = meleeStalkerPrefab != null ? meleeStalkerPrefab : shooterStalkerPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[EnemyStalkerSpawner] No stalker prefabs assigned.", this);
            return;
        }

        Vector3 p = PlayerRefs.I.T.position;
        float below = spawnDistance + Random.Range(0f, spawnDistanceExtra) + playerFallSpeed * fallLeadTime;
        Vector3 pos = new Vector3(p.x + Random.Range(-spawnSpreadX, spawnSpreadX), p.y - below, 0f);

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        if (go.TryGetComponent(out EnemyStalker stalker)) stalker.SnapDepth();   // starts deep, flies in
        alive.Add(go);
    }

    private void TrackPlayerFall()
    {
        if (PlayerRefs.I == null || Time.deltaTime <= 0f) { hasLastPos = false; return; }
        Vector3 p = PlayerRefs.I.T.position;
        if (hasLastPos)
        {
            float fall = Mathf.Max(0f, (lastPlayerPos.y - p.y) / Time.deltaTime);
            playerFallSpeed = Mathf.Lerp(playerFallSpeed, fall, 1f - Mathf.Exp(-5f * Time.deltaTime));
        }
        lastPlayerPos = p;
        hasLastPos = true;
    }

    public void DrawDebug()
    {
        Transform t = PlayerRefs.PlayerTransform;
        if (t == null) return;
        Vector3 c = t.position;
        float below = playerFallSpeed * fallLeadTime;
        float top = c.y - spawnDistance - below, bottom = top - spawnDistanceExtra;
        DebugDraw.Box(new Vector3(c.x, (top + bottom) * 0.5f, 0f),
                      new Vector3(spawnSpreadX * 2f, Mathf.Max(0.2f, top - bottom), 0f), DebugColors.Target);
        DebugDraw.Label(new Vector3(c.x - spawnSpreadX, top, 0f),
                        $"stalker spawn · alive {alive.Count}/{maxAlive} · roll {Mathf.Max(0f, checkInterval - rollTimer):0.0}s" +
                        (cooldownRemaining > 0f ? $" · quiet {cooldownRemaining:0.0}s" : "") +
                        $" · fall {playerFallSpeed:0.0} m/s", DebugColors.Target);
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = Application.isPlaying && PlayerRefs.I != null ? PlayerRefs.I.T : null;
        Vector3 c = t != null ? t.position : transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
        // spawn box below the player
        float top = c.y - spawnDistance, bottom = c.y - spawnDistance - spawnDistanceExtra;
        Vector3 center = new Vector3(c.x, (top + bottom) * 0.5f, 0f);
        Gizmos.DrawWireCube(center, new Vector3(spawnSpreadX * 2f, Mathf.Max(0.1f, top - bottom), 0.1f));
    }
}
