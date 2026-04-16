using UnityEngine;

/// <summary>
/// Cached singleton reference to the Player and its most-used components.
///
/// WHY:
///   Many systems (enemies, projectiles, UI, pickups) need the Player and
///   its sub-components. Calling GameObject.FindWithTag("Player") +
///   GetComponent<X>() repeatedly is wasteful — 20+ call sites in this
///   project were doing it every frame or on every event.
///
///   PlayerRefs caches everything in Awake and exposes a static accessor.
///
/// USAGE:
///     PlayerRefs.I?.Stats?.RecordDamageDealt(dmg, DamageSource.Arrow);
///     PlayerRefs.I?.Energy?.RestoreEnergy(5f);
///     Transform target = PlayerRefs.I?.T;
///
/// SETUP:
///   1. Add this component to your Player GameObject (same object that has
///      PlayerStats, PlayerHealth, etc. — tagged "Player").
///   2. That's it. Nothing else to configure.
///
/// NOTES:
///   • I is null before Awake runs and after the Player is destroyed —
///     always null-check with I?.Field at call sites.
///   • If you load a new scene, the old Player is destroyed first
///     (OnDestroy clears I) before the new one's Awake runs.
///   • Uses ExecutionOrder -100 to initialize before anything that
///     might reference it.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerRefs : MonoBehaviour
{
    // ================================================================
    //  STATIC ACCESS
    // ================================================================

    /// <summary>The one and only PlayerRefs. Null if no Player is in the scene.</summary>
    public static PlayerRefs I { get; private set; }

    // ================================================================
    //  CACHED COMPONENTS
    // ================================================================

    [Header("Auto-cached (read-only in Inspector)")]
    public Transform             T;
    public PlayerStats           Stats;
    public PlayerHealth          Health;
    public PlayerEnergy          Energy;
    public PlayerShooting        Shooting;
    public PlayerUpgradeManager  Upgrades;
    public KillStreak            Killstreak;
    public DashAbility           Dash;
    public PlayerInventory       Inventory;
    public PlayerLevelSystem     LevelSystem;
    public PlayerStateController StateCtrl;

    // ================================================================
    //  LIFECYCLE
    // ================================================================

    private void Awake()
    {
        if (I != null && I != this)
        {
            Debug.LogWarning("[PlayerRefs] A second PlayerRefs was created — destroying the duplicate.", this);
            Destroy(this);
            return;
        }

        I          = this;
        T          = transform;
        Stats      = GetComponent<PlayerStats>();
        Health     = GetComponent<PlayerHealth>();
        Energy     = GetComponent<PlayerEnergy>();
        Shooting   = GetComponent<PlayerShooting>();
        Upgrades   = GetComponent<PlayerUpgradeManager>();
        Killstreak  = GetComponent<KillStreak>();
        Dash        = GetComponent<DashAbility>();
        Inventory   = GetComponent<PlayerInventory>();
        LevelSystem = GetComponent<PlayerLevelSystem>();
        StateCtrl   = GetComponent<PlayerStateController>();
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    // ================================================================
    //  CONVENIENCE
    // ================================================================

    /// <summary>Shortcut for the common "is there a Player in the scene" check.</summary>
    public static bool Exists => I != null;

    /// <summary>World position of the player, or Vector3.zero if no player exists.</summary>
    public static Vector3 Position => I != null ? I.T.position : Vector3.zero;
}
