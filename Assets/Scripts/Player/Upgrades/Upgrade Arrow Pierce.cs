using UnityEngine;

/// <summary>
/// Arrow Pierce — arrows pass through enemies, dealing bonus damage on each pass-through.
///
/// PlayerUpgradeData behaviourSettings needed:
///   "pierceCount"   — how many enemies the arrow passes through (default 1)
///   "pierceDamage"  — flat bonus damage per pass-through (default 5)
///   "pierceDmgAP"   — fraction of Ability Power added to pierce damage (default 0.40)
///
/// Example description template:
///   "Arrows pierce through [pierceCount] enemies, dealing [pierceDmg] bonus damage per pass."
/// </summary>
public class UpgradeArrowPierce : PlayerUpgrade
{
    public override string Id => "Arrow_Pierce";

    private PlayerStats playerStats;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();

        var data = mgr.GetUpgradeData(Id);
        if (playerStats != null && data != null)
        {
            playerStats.arrowPierceCount = Mathf.RoundToInt(data.GetSetting("pierceCount", 1f));
            playerStats.pierceDamageBase = data.GetSetting("pierceDamage", 5f);
            playerStats.pierceDmgAPScaling = data.GetSetting("pierceDmgAP", 0.40f);
        }
    }

    // Single level — OnLevelUp never called (pool filters owned upgrades)
    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel) { }
}