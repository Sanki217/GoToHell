using UnityEngine;

/// <summary>
/// Arrow Pierce — arrows pass through enemies without killing them.
/// Each pass deals bonus damage: 5 + X% Ability Power.
/// Level 1: 40%  Level 2: 60%  Level 3: 80%  Level 4: 100%  Level 5: 120%
///
/// Arrow.cs checks arrowPierceCount to decide whether to stop on a non-kill.
/// The bonus damage per pierce is read from playerStats via piercePassDamageBase + piercePassDmgScaling.
///
/// Configure base values in PlayerUpgradeData behaviourSettings or via Inspector below.
/// </summary>
public class UpgradeArrowPierce : PlayerUpgrade
{
    public override string Id => "Arrow_Pierce";

    // ── Level-specific values ─────────────────────────────────────────────
    [Header("Pierce Damage — base flat bonus per pass")]
    public float pierceBaseDamage = 5f;

    [Header("Pierce Damage Scaling — % of Ability Power per level")]
    [Tooltip("Index 0 = level 1, index 4 = level 5")]
    public float[] abilityPowerScaling = { 0.40f, 0.60f, 0.80f, 1.00f, 1.20f };

    // ── Private ──────────────────────────────────────────────────────────
    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;
    private int currentLevel = 0;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        upgradeManager = mgr;

        // Read Inspector overrides from behaviourSettings if present
        var data = mgr.GetUpgradeData(Id);
        if (data != null)
        {
            float v = data.GetSetting("baseDamage", -1f);
            if (v >= 0f) pierceBaseDamage = v;
        }

        currentLevel = 1;
        Apply();
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        currentLevel = newLevel;
        Apply();
    }

    private void Apply()
    {
        if (playerStats == null) return;

        // Grant +1 pierce per level (total = level)
        playerStats.arrowPierceCount = currentLevel;

        // Store scaling so Arrow.cs can read it
        int idx = Mathf.Clamp(currentLevel - 1, 0, abilityPowerScaling.Length - 1);
        playerStats.pierceDamageBase = pierceBaseDamage;
        playerStats.pierceDmgAPScaling = abilityPowerScaling[idx];
    }
}