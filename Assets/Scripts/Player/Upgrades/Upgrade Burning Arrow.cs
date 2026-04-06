using UnityEngine;

/// <summary>
/// Burning Arrow — arrows apply [Burn] to enemies they damage.
/// Each level increases the [Burn] stat by 25%/50%/75%/100%/125% of its base value.
///
/// Burn stat (burnStrength) is a multiplier: 1.0 = base 100% (5 dmg/s).
/// Adding 25% of base means burnStrength += 0.25 (i.e. +25% to the base 1.0).
///
/// Configure base burn bonus values per level in Inspector or behaviourSettings.
/// </summary>
public class UpgradeBurningArrow : PlayerUpgrade
{
    public override string Id => "Arrow_Burn";

    [Header("Burn Strength Added per Level (fraction of base 1.0)")]
    [Tooltip("Level 1=+0.25, Level 2=+0.50... Each value is the TOTAL burn bonus at that level.")]
    public float[] burnBonusPerLevel = { 0.25f, 0.50f, 0.75f, 1.00f, 1.25f };

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;
    private float appliedBurnBonus = 0f;
    private int currentLevel = 0;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;
        currentLevel = 1;

        // Subscribe: apply burn on every arrow hit
        mgr.OnArrowHitEnemy += OnArrowHit;

        ApplyBurnBonus();
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        currentLevel = newLevel;
        ApplyBurnBonus();
    }

    private void ApplyBurnBonus()
    {
        if (playerStats == null) return;

        int idx = Mathf.Clamp(currentLevel - 1, 0, burnBonusPerLevel.Length - 1);
        float newBonus = burnBonusPerLevel[idx];
        float delta = newBonus - appliedBurnBonus;

        // burnStrength is a derived stat — we add to the base via primary system
        // Since burnStrength = baseBurnStrength + burnPerAP * abilityPower,
        // we directly adjust baseBurnStrength to add a flat bonus
        playerStats.baseBurnStrength += delta;
        appliedBurnBonus = newBonus;
        playerStats.RecalculateDerived();
    }

    private void OnArrowHit(GameObject enemy, float chargeLevel, bool wasCrit)
    {
        Enemy e = enemy?.GetComponent<Enemy>();
        if (e == null) return;
        e.ApplyStatus(StatusType.Burn, playerStats, upgradeManager);
    }
}