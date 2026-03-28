using UnityEngine;

/// <summary>
/// Base class for "Chance to apply X status on any damage" upgrades.
/// Each subclass specifies which status type and subscribes to arrow + dash hit events.
///
/// Common:  10% chance
/// Rare:    add to chance via OnLevelUp
/// Each level: +5% chance, +20% to that status's strength
/// </summary>
public abstract class UpgradeStatusOnHit : PlayerUpgrade
{
    protected abstract StatusType StatusType { get; }

    protected float applyChance = 0.10f;  // starts at 10%
    protected PlayerStats playerStats;
    protected PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;

        // Subscribe to all damage events
        mgr.OnArrowHitEnemy += OnArrowHit;
        mgr.OnDashHitEnemy += OnDashHit;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        applyChance += 0.05f;   // +5% chance per level
        // Also boost the relevant status strength
        if (playerStats == null) return;
        switch (StatusType)
        {
            case StatusType.Burn: playerStats.burnStrength += 0.2f; break;
            case StatusType.Freeze: playerStats.freezeStrength += 0.2f; break;
            case StatusType.Holy: playerStats.holyStrength += 0.2f; break;
            case StatusType.Shock: playerStats.shockStrength += 0.2f; break;
        }
    }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit)
        => TryApply(enemy);

    private void OnDashHit(GameObject enemy)
        => TryApply(enemy);

    private void TryApply(GameObject enemyGO)
    {
        if (Random.value > applyChance) return;
        Enemy enemy = enemyGO?.GetComponent<Enemy>();
        if (enemy == null) return;
        enemy.ApplyStatus(StatusType, playerStats, upgradeManager);
    }
}

// ── Four concrete subclasses ─────────────────────────────────────────────────

public class UpgradeStatusOnHitBurn : UpgradeStatusOnHit
{
    public override string Id => "Status_Burn";
    protected override StatusType StatusType => StatusType.Burn;
}

public class UpgradeStatusOnHitFreeze : UpgradeStatusOnHit
{
    public override string Id => "Status_Freeze";
    protected override StatusType StatusType => StatusType.Freeze;
}

public class UpgradeStatusOnHitHoly : UpgradeStatusOnHit
{
    public override string Id => "Status_Holy";
    protected override StatusType StatusType => StatusType.Holy;
}

public class UpgradeStatusOnHitShock : UpgradeStatusOnHit
{
    public override string Id => "Status_Shock";
    protected override StatusType StatusType => StatusType.Shock;
}