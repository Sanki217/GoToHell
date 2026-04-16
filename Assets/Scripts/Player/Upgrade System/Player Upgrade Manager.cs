using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerUpgradeManager : MonoBehaviour
{
    // ============================================================
    //  DEBUG
    // ============================================================

    [SerializeField]
    private List<string> debugActiveUpgrades = new();

    private void Update()
    {
        debugActiveUpgrades.Clear();
        foreach (var id in activeUpgrades.Keys)
            debugActiveUpgrades.Add(id);
    }

    // ============================================================
    //  QUERY
    // ============================================================

    public bool HasUpgrade(string id) => activeUpgrades.ContainsKey(id);

    public IEnumerable<string> GetActiveUpgradeIds()
    {
        foreach (var id in activeUpgrades.Keys)
            yield return id;
    }

    // ============================================================
    //  EVENTS — Shooting
    // ============================================================

    public event Action<Vector3, float> OnWeakArrowFired;
    public event Action<Vector3, float> OnMediumArrowFired;
    public event Action<Vector3, float> OnChargedArrowFired;
    public event Action<Vector3, float> OnExtraArrowFired;

    public event Action<GameObject, float, bool> OnArrowHitEnemy;
    public event Action<Vector3> OnArrowHitWall;
    public event Action<GameObject> OnArrowHitDestructible;
    public event Action OnArrowPickedUp;
    public event Action<float> OnArrowChargeCancelledByEnergy;

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

    public event Action<int> OnJump;
    public event Action<float> OnLand;
    public event Action<float, float> OnFalling;

    // ============================================================
    //  EVENTS — Combat
    // ============================================================

    public event Action<GameObject> OnEnemyKilled;
    public event Action<GameObject, float> OnCriticalHit;
    public event Action<int> OnDamageTaken;
    public event Action OnPlayerDied;

    // ============================================================
    //  EVENTS — Status Effects
    // ============================================================

    public event Action<GameObject, StatusType> OnStatusApplied;
    public event Action<GameObject, float> OnBurnTick;
    public event Action<GameObject> OnFreezeTick;
    public event Action<GameObject, float> OnHolyDetonated;
    public event Action<GameObject, float> OnShockConsumed;

    // ============================================================
    //  EVENTS — Environment
    // ============================================================

    public event Action OnSpikesTouched;
    public event Action<float> OnLavaTick;
    public event Action OnLavaZoneDrained;

    // ============================================================
    //  EVENTS — Arrow Kills (for Soul Arrow chaining — chain copies excluded)
    // ============================================================

    public event Action<GameObject> OnArrowKill;

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

    private Dictionary<string, PlayerUpgrade> activeUpgrades = new();

    /// <summary>
    /// Stored info for each applied upgrade — used by StatsUI hover tooltip.
    /// Populated when ApplyUpgrade is called.
    /// </summary>
    private Dictionary<string, AppliedUpgradeInfo> appliedUpgradeInfo = new();

    public AppliedUpgradeInfo GetUpgradeInfo(string id)
    {
        appliedUpgradeInfo.TryGetValue(id, out var info);
        return info;
    }

    public PlayerUpgrade GetUpgrade(string id)
    {
        activeUpgrades.TryGetValue(id, out var upgrade);
        return upgrade;
    }

    // ============================================================
    //  EVENT TRIGGERS — Shooting
    // ============================================================

    public void FireWeakArrow(Vector3 dir, float speed) => OnWeakArrowFired?.Invoke(dir, speed);
    public void FireMediumArrow(Vector3 dir, float charge) => OnMediumArrowFired?.Invoke(dir, charge);
    public void FireChargedArrow(Vector3 dir, float speed) => OnChargedArrowFired?.Invoke(dir, speed);
    public void FireExtraArrow(Vector3 dir, float speed) => OnExtraArrowFired?.Invoke(dir, speed);

    public void ArrowHitEnemy(GameObject enemy, float chargeLevel = 0f, bool wasCrit = false)
        => OnArrowHitEnemy?.Invoke(enemy, chargeLevel, wasCrit);
    public void ArrowHitWall(Vector3 position) => OnArrowHitWall?.Invoke(position);
    public void ArrowHitDestructible(GameObject target) => OnArrowHitDestructible?.Invoke(target);
    public void ArrowPickedUp() => OnArrowPickedUp?.Invoke();
    public void ArrowChargeCancelledByEnergy(float cr) => OnArrowChargeCancelledByEnergy?.Invoke(cr);

    // ============================================================
    //  EVENT TRIGGERS — Dash
    // ============================================================

    public void DashStart() => OnDashStarted?.Invoke();
    public void DashEnd() => OnDashEnded?.Invoke();
    public void DashHitEnemy(GameObject e) => OnDashHitEnemy?.Invoke(e);
    public void DashHitWall() => OnDashHitWall?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Wall Slide
    // ============================================================

    public void WallSlideStart() => OnWallSlideStart?.Invoke();
    public void WallSlideTick(float dt) => OnWallSlideTick?.Invoke(dt);
    public void WallSlideEnd() => OnWallSlideEnd?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Hover
    // ============================================================

    public void HoverStart() => OnHoverStart?.Invoke();
    public void HoverTick(float dt) => OnHoverTick?.Invoke(dt);
    public void HoverEnd() => OnHoverEnd?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Movement
    // ============================================================

    public void Jump(int n) => OnJump?.Invoke(n);
    public void Land(float speed) => OnLand?.Invoke(speed);
    public void Falling(float dt, float s) => OnFalling?.Invoke(dt, s);

    // ============================================================
    //  EVENT TRIGGERS — Combat
    // ============================================================

    public void EnemyKilled(GameObject e) => OnEnemyKilled?.Invoke(e);
    public void CriticalHit(GameObject e, float dmg) => OnCriticalHit?.Invoke(e, dmg);
    public void DamageTaken(int amount) => OnDamageTaken?.Invoke(amount);
    public void PlayerDied() => OnPlayerDied?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Status Effects
    // ============================================================

    public void StatusApplied(GameObject e, StatusType t) => OnStatusApplied?.Invoke(e, t);
    public void BurnTick(GameObject e, float dmg) => OnBurnTick?.Invoke(e, dmg);
    public void FreezeTick(GameObject e) => OnFreezeTick?.Invoke(e);
    public void HolyDetonated(GameObject e, float dmg) => OnHolyDetonated?.Invoke(e, dmg);
    public void ShockConsumed(GameObject e, float bonus) => OnShockConsumed?.Invoke(e, bonus);

    // ============================================================
    //  EVENT TRIGGERS — Environment
    // ============================================================

    public void SpikesTouched() => OnSpikesTouched?.Invoke();
    public void LavaTick(float dt) => OnLavaTick?.Invoke(dt);
    public void LavaZoneDrained() => OnLavaZoneDrained?.Invoke();

    // ============================================================
    //  EVENT TRIGGERS — Resources
    // ============================================================

    public void ArrowKill(GameObject enemy) => OnArrowKill?.Invoke(enemy);

    public void SoulCollected(int amount) => OnSoulCollected?.Invoke(amount);
    public void EnergyGained(float amt, EnergySource s) => OnEnergyGained?.Invoke(amt, s);
    public void PlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
    public void ChestOpened(ChestRarity r) => OnChestOpened?.Invoke(r);
    public void MerchantPurchase(string id) => OnMerchantPurchase?.Invoke(id);

    // ============================================================
    //  APPLY UPGRADE
    // ============================================================

    public void ApplyUpgrade(PlayerUpgrade upgrade, UpgradeRarity rarity = UpgradeRarity.Common,
                             System.Collections.Generic.List<UpgradeStatBonus> statBonuses = null)
    {
        string id = upgrade.UpgradeId;

        if (activeUpgrades.ContainsKey(id))
        {
            Debug.LogWarning($"[UpgradeManager] '{id}' already owned — ignoring.");
            return;
        }

        activeUpgrades.Add(id, upgrade);

        // Store display info for the TAB hover tooltip
        appliedUpgradeInfo[id] = new AppliedUpgradeInfo
        {
            displayName = upgrade.displayName,
            icon = upgrade.icon,
            rarity = rarity,
            statBonuses = statBonuses,
            category = upgrade.category
        };

        upgrade.OnAdded(this);

        Debug.Log($"[Upgrade] {id} applied.");
    }
}

// ============================================================
//  SUPPORTING ENUMS
// ============================================================

public enum StatusType { Burn, Freeze, Holy, Shock }
public enum EnergySource { Kill, Falling, Lava, WallSlide }
public enum ChestRarity { Common, Rare, Epic, Legendary }

/// <summary>Snapshot of an upgrade's display info at the time it was applied.</summary>
public class AppliedUpgradeInfo
{
    public string displayName;
    public Sprite icon;
    public UpgradeRarity rarity;
    public System.Collections.Generic.List<UpgradeStatBonus> statBonuses;
    public UpgradeCategory category;
}