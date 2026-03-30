using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerUpgradeManager : MonoBehaviour
{
    // ============================================================
    //  DEBUG
    // ============================================================

    [Header("Upgrade Pool (assign your UpgradePool asset here)")]
    public PlayerUpgradePool upgradePool;

    [SerializeField]
    private List<string> debugActiveUpgrades = new();

    private void Update()
    {
        debugActiveUpgrades.Clear();
        foreach (var kv in activeUpgrades)
            debugActiveUpgrades.Add($"{kv.Key} (Lv {kv.Value.level})");
    }

    // ============================================================
    //  QUERY
    // ============================================================

    public bool HasUpgrade(string id)
        => activeUpgrades.ContainsKey(id);

    public int GetUpgradeLevel(string id)
        => activeUpgrades.TryGetValue(id, out var inst) ? inst.level : 0;

    /// <summary>
    /// Look up a PlayerUpgradeData asset by upgrade ID.
    /// Returns null if the pool is not assigned or the ID is not found.
    /// Used by upgrade behaviour classes to read their behaviourSettings.
    /// </summary>
    public PlayerUpgradeData GetUpgradeData(string id)
    {
        if (upgradePool == null) return null;
        foreach (var data in upgradePool.upgrades)
            if (data != null && data.upgradeId == id) return data;
        return null;
    }

    /// <summary>Returns a snapshot of all active upgrades as (id, level) pairs for the UI.</summary>
    public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, int>> GetActiveUpgrades()
    {
        foreach (var kv in activeUpgrades)
            yield return new System.Collections.Generic.KeyValuePair<string, int>(kv.Key, kv.Value.level);
    }

    // ============================================================
    //  EVENTS — Shooting
    // ============================================================

    public event Action<Vector3, float> OnWeakArrowFired;
    public event Action<Vector3, float> OnMediumArrowFired;
    public event Action<Vector3, float> OnChargedArrowFired;
    public event Action<Vector3, float> OnExtraArrowFired;

    // enemy = the enemy GameObject that was hit
    // chargeLevel = 0 (weak) to 1 (fully charged)
    // wasCrit = whether this hit was a critical strike
    public event Action<GameObject, float, bool> OnArrowHitEnemy;
    public event Action<Vector3> OnArrowHitWall;
    public event Action<GameObject> OnArrowHitDestructible;
    public event Action OnArrowPickedUp;
    public event Action<float> OnArrowChargeCancelledByEnergy;  // float = charge level reached (0-1)

    // ============================================================
    //  EVENTS — Dash
    // ============================================================

    public event Action OnDashStarted;
    public event Action OnDashEnded;
    public event Action<GameObject> OnDashHitEnemy;
    public event Action OnDashHitWall;

    // ============================================================
    //  EVENTS — Wall Slide
    // ============================================================

    public event Action OnWallSlideStart;
    public event Action<float> OnWallSlideTick;
    public event Action OnWallSlideEnd;

    // ============================================================
    //  EVENTS — Hover
    // ============================================================

    public event Action OnHoverStart;
    public event Action<float> OnHoverTick;
    public event Action OnHoverEnd;

    // ============================================================
    //  EVENTS — Movement
    // ============================================================

    // jumpNumber: 1 = first jump, 2 = double jump
    public event Action<int> OnJump;
    public event Action<float> OnLand;          // float = fall speed on landing
    public event Action<float, float> OnFalling;       // deltaTime, currentFallSpeed

    // ============================================================
    //  EVENTS — Combat
    // ============================================================

    public event Action<GameObject> OnEnemyKilled;
    public event Action<GameObject, float> OnCriticalHit;   // enemy, damageDealt
    public event Action<int> OnDamageTaken;
    public event Action OnPlayerDied;

    // ============================================================
    //  EVENTS — Status Effects
    // ============================================================

    public event Action<GameObject, StatusType> OnStatusApplied;
    public event Action<GameObject, float> OnBurnTick;      // enemy, damageDealt
    public event Action<GameObject> OnFreezeTick;
    public event Action<GameObject, float> OnHolyDetonated; // enemy, damageDealt
    public event Action<GameObject, float> OnShockConsumed; // enemy, bonusDamage

    // ============================================================
    //  EVENTS — Environment
    // ============================================================

    public event Action OnSpikesTouched;
    public event Action<float> OnLavaTick;
    public event Action OnLavaZoneDrained;

    // ============================================================
    //  EVENTS — Resources
    // ============================================================

    public event Action<int> OnSoulCollected;
    public event Action<float, EnergySource> OnEnergyGained;
    public event Action<int> OnPlayerLevelUp;
    public event Action<ChestRarity> OnChestOpened;
    public event Action<string> OnMerchantPurchase;

    // ============================================================
    //  UPGRADE STORAGE
    // ============================================================

    private Dictionary<string, PlayerUpgradeInstance> activeUpgrades =
        new Dictionary<string, PlayerUpgradeInstance>();

    // ============================================================
    //  EVENT TRIGGERS — Shooting
    // ============================================================

    public void FireWeakArrow(Vector3 dir, float speed)
        => OnWeakArrowFired?.Invoke(dir, speed);

    public void FireMediumArrow(Vector3 dir, float charge)
        => OnMediumArrowFired?.Invoke(dir, charge);

    public void FireChargedArrow(Vector3 dir, float speed)
        => OnChargedArrowFired?.Invoke(dir, speed);

    public void FireExtraArrow(Vector3 dir, float speed)
        => OnExtraArrowFired?.Invoke(dir, speed);

    public void ArrowHitEnemy(GameObject enemy, float chargeLevel = 0f, bool wasCrit = false)
        => OnArrowHitEnemy?.Invoke(enemy, chargeLevel, wasCrit);

    public void ArrowHitWall(Vector3 position)
        => OnArrowHitWall?.Invoke(position);

    public void ArrowHitDestructible(GameObject target)
        => OnArrowHitDestructible?.Invoke(target);

    public void ArrowPickedUp()
        => OnArrowPickedUp?.Invoke();

    public void ArrowChargeCancelledByEnergy(float chargeReached)
        => OnArrowChargeCancelledByEnergy?.Invoke(chargeReached);

    // ============================================================
    //  EVENT TRIGGERS — Dash
    // ============================================================

    public void DashStart() => OnDashStarted?.Invoke();
    public void DashEnd() => OnDashEnded?.Invoke();
    public void DashHitEnemy(GameObject enemy) => OnDashHitEnemy?.Invoke(enemy);
    public void DashHitWall() => OnDashHitWall?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Wall Slide
    // ============================================================

    public void WallSlideStart() => OnWallSlideStart?.Invoke();
    public void WallSlideTick(float deltaTime) => OnWallSlideTick?.Invoke(deltaTime);
    public void WallSlideEnd() => OnWallSlideEnd?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Hover
    // ============================================================

    public void HoverStart() => OnHoverStart?.Invoke();
    public void HoverTick(float deltaTime) => OnHoverTick?.Invoke(deltaTime);
    public void HoverEnd() => OnHoverEnd?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Movement
    // ============================================================

    public void Jump(int jumpNumber) => OnJump?.Invoke(jumpNumber);
    public void Land(float fallSpeed) => OnLand?.Invoke(fallSpeed);
    public void Falling(float dt, float speed) => OnFalling?.Invoke(dt, speed);

    // ============================================================
    //  EVENT TRIGGERS — Combat
    // ============================================================

    public void EnemyKilled(GameObject enemy) => OnEnemyKilled?.Invoke(enemy);
    public void CriticalHit(GameObject enemy, float dmg) => OnCriticalHit?.Invoke(enemy, dmg);
    public void DamageTaken(int amount) => OnDamageTaken?.Invoke(amount);
    public void PlayerDied() => OnPlayerDied?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Status Effects
    // ============================================================

    public void StatusApplied(GameObject enemy, StatusType type) => OnStatusApplied?.Invoke(enemy, type);
    public void BurnTick(GameObject enemy, float dmg) => OnBurnTick?.Invoke(enemy, dmg);
    public void FreezeTick(GameObject enemy) => OnFreezeTick?.Invoke(enemy);
    public void HolyDetonated(GameObject enemy, float dmg) => OnHolyDetonated?.Invoke(enemy, dmg);
    public void ShockConsumed(GameObject enemy, float bonusDmg) => OnShockConsumed?.Invoke(enemy, bonusDmg);

    // ============================================================
    //  EVENT TRIGGERS — Environment
    // ============================================================

    public void SpikesTouched() => OnSpikesTouched?.Invoke();
    public void LavaTick(float deltaTime) => OnLavaTick?.Invoke(deltaTime);
    public void LavaZoneDrained() => OnLavaZoneDrained?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Resources
    // ============================================================

    public void SoulCollected(int amount) => OnSoulCollected?.Invoke(amount);
    public void EnergyGained(float amount, EnergySource source) => OnEnergyGained?.Invoke(amount, source);
    public void PlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
    public void ChestOpened(ChestRarity rarity) => OnChestOpened?.Invoke(rarity);
    public void MerchantPurchase(string itemId) => OnMerchantPurchase?.Invoke(itemId);

    // ============================================================
    //  APPLY UPGRADE  (bug fix: OnLevelUp now called on the STORED
    //  instance's upgrade, not the incoming one)
    // ============================================================

    public void ApplyUpgrade(PlayerUpgrade upgrade)
    {
        if (!activeUpgrades.TryGetValue(upgrade.Id, out var instance))
        {
            // First time we see this upgrade — store it and call OnAdded
            instance = new PlayerUpgradeInstance(upgrade);
            activeUpgrades.Add(upgrade.Id, instance);
            instance.upgrade.OnAdded(this);
        }

        // Always increment and notify the STORED upgrade, not the incoming one
        instance.level++;
        instance.upgrade.OnLevelUp(this, instance.level);

        Debug.Log($"[Upgrade] {upgrade.Id} → Level {instance.level}");
    }
}

// ============================================================
//  SUPPORTING ENUMS
//  (kept here so all scripts can see them without extra files)
// ============================================================

public enum StatusType
{
    Burn,
    Freeze,
    Holy,
    Shock
}

public enum EnergySource
{
    Kill,
    Falling,
    Lava,
    WallSlide
}

public enum ChestRarity
{
    Common,
    Rare,
    Legendary
}