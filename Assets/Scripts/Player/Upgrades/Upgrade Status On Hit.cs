using UnityEngine;

/// <summary>
/// Base class for "chance to apply X status on arrow or dash hit" upgrades.
/// Subclasses (one per status type) are attached to their own orb prefabs.
/// All tuning is in the Inspector on each prefab.
/// </summary>
public abstract class UpgradeStatusOnHit : PlayerUpgrade
{
    protected abstract StatusType StatusType { get; }

    [Header("Status On Hit — Tuning")]
    [Tooltip("Probability (0–1) to apply the status on each hit. 0.15 = 15%.")]
    public float applyChance = 0.15f;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;

        mgr.OnArrowHitEnemy += OnArrowHit;
        mgr.OnDashHitEnemy += OnDashHit;
    }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit) => TryApply(enemy);
    private void OnDashHit(GameObject enemy) => TryApply(enemy);

    private void TryApply(GameObject enemyGO)
    {
        if (Random.value > applyChance) return;
        Enemy enemy = enemyGO?.GetComponent<Enemy>();
        if (enemy == null) return;
        enemy.ApplyStatus(StatusType, playerStats, upgradeManager);
    }
}

// ── Concrete subclasses — one prefab each ──────────────────────────────────

public class UpgradeStatusOnHitBurn : UpgradeStatusOnHit
{
    protected override StatusType StatusType => StatusType.Burn;
}

public class UpgradeStatusOnHitFreeze : UpgradeStatusOnHit
{
    protected override StatusType StatusType => StatusType.Freeze;
}

public class UpgradeStatusOnHitHoly : UpgradeStatusOnHit
{
    protected override StatusType StatusType => StatusType.Holy;
}

public class UpgradeStatusOnHitShock : UpgradeStatusOnHit
{
    protected override StatusType StatusType => StatusType.Shock;
}