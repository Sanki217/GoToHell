using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Soul Bonus upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
///
/// Effect: every soul collected grants extra souls.
/// Extra souls = collected × (bonusPercent + bonusPerLuck × Luck)
/// Scales with Luck.
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    [Header("Soul Bonus — Tuning")]
    [Tooltip("Base fraction of extra souls granted per collection. 1.0 = +100%.")]
    public float bonusPercent = 1.0f;

    [Tooltip("Additional fraction per 1 point of Luck. 0.10 = +10% per Luck.")]
    public float bonusPerLuck = 0.10f;

    private PlayerStats playerStats;
    private PlayerInventory inventory;
    private PlayerUpgradeManager upgradeManager;
    private bool granting = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        upgradeManager = mgr;
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();

        mgr.OnSoulCollected += OnSoulCollected;
    }

    private void OnDestroy()
    {
        if (upgradeManager != null) upgradeManager.OnSoulCollected -= OnSoulCollected;
    }

    public override string GetDynamicDescription(PlayerStats stats, List<UpgradeStatBonus> simulatedBonuses)
    {
        var s = Simulate(stats, simulatedBonuses);
        float bonus = bonusPercent + bonusPerLuck * s.luck;
        int pct = Mathf.RoundToInt(bonus * 100f);
        return $"Each soul collected grants {LK(pct, "F0")}% bonus souls.\n" +
               $"Scales with <color=#AAFF44>Luck</color>.";
    }

    private float GetBonus() =>
        bonusPercent + bonusPerLuck * (playerStats != null ? playerStats.luck : 0f);

    private void OnSoulCollected(int amount)
    {
        if (granting || inventory == null) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * GetBonus()));
        granting = true;
        // Updates UI + stats, grants XP once (unmultiplied — see PlayerLevelSystem),
        // and does not re-fire OnSoulCollected
        inventory.AddBonusSouls(bonus);
        granting = false;
    }
}