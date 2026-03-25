using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized FX manager. Handles all audio and particle effects in the game.
/// Place this on an empty GameObject in the scene called "FXManager".
///
/// Usage from any script:
///   FXManager.Play(ActionFX.PlayerDash, transform.position);
///   FXManager.Play(ActionFX.EnemyDeath, transform.position);
/// </summary>
public class FXManager : MonoBehaviour
{
    // ================================================================
    //  SINGLETON
    // ================================================================

    public static FXManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ================================================================
    //  DATA TYPES
    // ================================================================

    [System.Serializable]
    public class FXEntry
    {
        public ActionFX id;
        public AudioClip audioClip;
        public GameObject particlePrefab;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitchMin = 0.9f;

        [Range(0.5f, 2f)]
        public float pitchMax = 1.1f;

        public float particleScale = 1f;
    }

    // ================================================================
    //  INSPECTOR
    // ================================================================

    [Header("Audio Source Pool")]
    public int audioSourcePoolSize = 10;   // how many sounds can play at once

    [Header("FX Library — fill in Unity Inspector")]
    public List<FXEntry> fxLibrary = new List<FXEntry>();

    // ================================================================
    //  PRIVATE
    // ================================================================

    private Dictionary<ActionFX, FXEntry> fxLookup = new();
    private AudioSource[] audioPool;
    private int poolIndex;

    private void Start()
    {
        // Build fast lookup dictionary
        fxLookup.Clear();
        foreach (var entry in fxLibrary)
        {
            if (!fxLookup.ContainsKey(entry.id))
                fxLookup[entry.id] = entry;
        }

        // Create audio source pool on this GameObject
        audioPool = new AudioSource[audioSourcePoolSize];
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            audioPool[i] = gameObject.AddComponent<AudioSource>();
            audioPool[i].playOnAwake = false;
        }
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Play both the sound and particle effect for an action at a world position.
    /// </summary>
    public static void Play(ActionFX action, Vector3 worldPosition)
    {
        if (Instance == null) return;
        Instance.PlayInternal(action, worldPosition);
    }

    /// <summary>
    /// Play only the sound for an action (no particle).
    /// </summary>
    public static void PlaySound(ActionFX action)
    {
        if (Instance == null) return;
        Instance.PlaySoundInternal(action);
    }

    /// <summary>
    /// Play only the particle for an action (no sound).
    /// </summary>
    public static void PlayParticle(ActionFX action, Vector3 worldPosition)
    {
        if (Instance == null) return;
        Instance.PlayParticleInternal(action, worldPosition);
    }

    // ================================================================
    //  PRIVATE IMPLEMENTATION
    // ================================================================

    private void PlayInternal(ActionFX action, Vector3 position)
    {
        if (!fxLookup.TryGetValue(action, out FXEntry entry)) return;
        PlayAudio(entry);
        SpawnParticle(entry, position);
    }

    private void PlaySoundInternal(ActionFX action)
    {
        if (!fxLookup.TryGetValue(action, out FXEntry entry)) return;
        PlayAudio(entry);
    }

    private void PlayParticleInternal(ActionFX action, Vector3 position)
    {
        if (!fxLookup.TryGetValue(action, out FXEntry entry)) return;
        SpawnParticle(entry, position);
    }

    private void PlayAudio(FXEntry entry)
    {
        if (entry.audioClip == null) return;

        // Grab next available AudioSource from pool
        AudioSource source = audioPool[poolIndex % audioSourcePoolSize];
        poolIndex++;

        source.clip = entry.audioClip;
        source.volume = entry.volume;
        source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        source.Play();
    }

    private void SpawnParticle(FXEntry entry, Vector3 position)
    {
        if (entry.particlePrefab == null) return;

        GameObject fx = Instantiate(entry.particlePrefab, position, Quaternion.identity);
        fx.transform.localScale = Vector3.one * entry.particleScale;

        // Auto-destroy after particle system finishes
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(fx, 3f); // fallback
    }
}

// ================================================================
//  ACTION FX ENUM — every action in the game
// ================================================================

public enum ActionFX
{
    // Player — Movement
    PlayerJump,
    PlayerDoubleJump,
    PlayerLand,
    PlayerWallSlideStart,
    PlayerWallSlideTick,
    PlayerWallSlideEnd,
    PlayerHoverStart,
    PlayerHoverTick,
    PlayerHoverEnd,

    // Player — Combat
    PlayerDashStart,
    PlayerDashEnd,
    PlayerDashHitEnemy,
    PlayerDashHitWall,
    PlayerArrowFireWeak,
    PlayerArrowFireMedium,
    PlayerArrowFireCharged,
    PlayerArrowFireExtra,
    PlayerArrowHitEnemy,
    PlayerArrowHitWall,
    PlayerArrowHitDestructible,
    PlayerArrowPickup,
    PlayerCriticalHit,
    PlayerArrowChargeStart,
    PlayerArrowChargeCancelled,

    // Player — Damage and Death
    PlayerHit,
    PlayerDeath,
    PlayerSpikesTouched,
    PlayerLavaTick,

    // Player — Resources
    PlayerSoulCollect,
    PlayerLevelUp,
    PlayerEnergyGain,
    PlayerBonusEnergyStart,
    PlayerBonusEnergyEmpty,

    // Status Effects
    StatusBurnApply,
    StatusBurnTick,
    StatusFreezeApply,
    StatusFreezeTick,
    StatusHolyApply,
    StatusHolyDetonate,
    StatusShockApply,
    StatusShockConsume,

    // Enemies
    EnemyHit,
    EnemyDeath,
    EnemyShoot,
    EnemyProjectileHit,

    // Environment
    ChestOpenCommon,
    ChestOpenRare,
    ChestOpenLegendary,
    LavaZoneDrain,
    PortalOpen,
    ShootingTargetHit,

    // Merchant
    MerchantGreet,
    MerchantPurchase,
    MerchantShopOpen,
    MerchantShopClose,

    // Forge Items
    InfernalForgeUse,
    SoulCrucibleUse,
    ChaosForgeUse,

    // Upgrades
    UpgradePickup,
    UpgradeMythicTransform,
    LevelUpFanfare,
    CursedUpgradePickup,

    // UI
    MenuOpen,
    MenuClose
}