using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns stalker enemies around the player at random intervals — an
/// occasional danger, not a constant one. Put ONE on a manager object in the
/// gameplay scene; all tuning in the Inspector.
///
/// SPAWN POSITION (relative to the player):
///   • Z: random within spawnZMin..spawnZMax — negative, in front of the
///     level toward the camera, so the player never sees the spawn itself,
///     only the stalker flying in.
///   • Y: random within ±spawnYRange around the player.
///   • X: always OUTSIDE the noSpawnXHalfWidth band around the player (plus
///     up to spawnXExtra further out) — they enter from the sides.
///
/// TIMING: every checkInterval seconds one roll happens; spawnChance decides
/// if ONE stalker appears (shooterChance picks the variant). A successful
/// spawn is followed by spawnCooldown seconds of guaranteed quiet. Never more
/// than maxAlive at once — dead stalkers free their slot automatically.
/// </summary>
public class EnemyStalkerSpawner : MonoBehaviour
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
    [Tooltip("Depth range — negative Z is in front of the level, toward the camera.")]
    public float spawnZMin = -6f;
    public float spawnZMax = -12f;
    [Tooltip("Vertical spread around the player.")]
    public float spawnYRange = 4f;
    [Tooltip("No-spawn half-width on X around the player — stalkers always appear outside it.")]
    public float noSpawnXHalfWidth = 10f;
    [Tooltip("Extra random X distance beyond the no-spawn band.")]
    public float spawnXExtra = 5f;

    // ── state ───────────────────────────────────────────────────────
    private readonly List<GameObject> alive = new List<GameObject>();
    private float rollTimer;
    private float cooldownRemaining;

    private void Update()
    {
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
        float sideSign = Random.value < 0.5f ? -1f : 1f;
        Vector3 pos = new Vector3(
            p.x + sideSign * (noSpawnXHalfWidth + Random.Range(0f, spawnXExtra)),
            p.y + Random.Range(-spawnYRange, spawnYRange),
            Random.Range(spawnZMin, spawnZMax));

        alive.Add(Instantiate(prefab, pos, Quaternion.identity));
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = Application.isPlaying && PlayerRefs.I != null ? PlayerRefs.I.T : null;
        Vector3 c = t != null ? t.position : transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
        // no-spawn band edges
        Gizmos.DrawLine(c + new Vector3(-noSpawnXHalfWidth, -spawnYRange, 0), c + new Vector3(-noSpawnXHalfWidth, spawnYRange, 0));
        Gizmos.DrawLine(c + new Vector3(noSpawnXHalfWidth, -spawnYRange, 0), c + new Vector3(noSpawnXHalfWidth, spawnYRange, 0));
    }
}
