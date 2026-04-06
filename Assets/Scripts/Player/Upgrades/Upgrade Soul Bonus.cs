using UnityEngine;

/// <summary>
/// Soul Bonus — earn X% more souls.
/// Bonus: 50/100/150/200/250% + 10% Luck stat per level.
///
/// Bonus souls are added directly (no event re-fire, no XP double-scaling).
/// </summary>
public class UpgradeSoulBonus : PlayerUpgrade
{
    public override string Id => "SoulBonus";

    [Header("Base Bonus % per Level (0.5 = 50%, 1.0 = 100%)")]
    public float[] baseBonusPerLevel = { 0.50f, 1.00f, 1.50f, 2.00f, 2.50f };

    [Header("Luck Scaling (fraction per Luck point added to bonus %)")]
    [Tooltip("0.10 = +10% per Luck point. At Luck=5, adds +50% extra to the base bonus.")]
    public float luckScalingPerPoint = 0.10f;

    // ── Private ──────────────────────────────────────────────────────────
    private PlayerStats playerStats;
    private PlayerInventory inventory;
    private PlayerLevelSystem levelSystem;
    private int currentLevel = 0;
    private bool _isGranting = false;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        inventory = mgr.GetComponent<PlayerInventory>();
        levelSystem = mgr.GetComponent<PlayerLevelSystem>();
        currentLevel = 1;
        mgr.OnSoulCollected += OnSoulCollected;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        currentLevel = newLevel;
    }

    private float GetBonusFraction()
    {
        int idx = Mathf.Clamp(currentLevel - 1, 0, baseBonusPerLevel.Length - 1);
        float basePct = baseBonusPerLevel[idx];
        float luckBonus = playerStats != null ? playerStats.luck * luckScalingPerPoint : 0f;
        return basePct + luckBonus;
    }

    private void OnSoulCollected(int amount)
    {
        if (_isGranting || inventory == null) return;
        float fraction = GetBonusFraction();
        if (fraction <= 0f) return;

        int bonus = Mathf.Max(1, Mathf.RoundToInt(amount * fraction));
        _isGranting = true;
        inventory.currentSouls += bonus;
        levelSystem?.AddXPDirect(bonus);
        _isGranting = false;
    }
}