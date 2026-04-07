using UnityEngine;

/// <summary>
/// Soul Bonus upgrade. Attach to an upgrade orb prefab alongside UpgradeOrb.
/// All tuning values are exposed in the Inspector on the prefab.
///
/// Effect: every soul collected grants extra souls.
/// Extra souls = collected × (bonusPercent + bonusPerLuck × Luck)
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
    private PlayerLevelSystem levelSystem;
    private bool granting = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();
        levelSystem = mgr.GetComponent<PlayerLevelSystem>();

        mgr.OnSoulCollected += OnSoulCollected;
    }

    private float GetBonus() =>
        bonusPercent + bonusPerLuck * (playerStats != null ? playerStats.luck : 0f);

    private void OnSoulCollected(int amount)
    {
        if (granting || inventory == null) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * GetBonus()));
        granting = true;
        inventory.currentSouls += bonus;
        levelSystem?.AddXPDirect(bonus);
        granting = false;
    }
}