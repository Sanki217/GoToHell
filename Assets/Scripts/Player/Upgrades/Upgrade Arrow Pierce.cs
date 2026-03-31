using UnityEngine;

/// <summary>
/// Arrow Pierce — arrows pass through enemies they don't kill.
///
/// Level 1: arrows can pierce through 1 non-kill enemy before stopping.
/// Level 2: pierce through 2 non-kill enemies.
/// Level N: pierce through N non-kill enemies.
///
/// Arrows always pass through enemies they kill for free — this upgrade only
/// adds the ability to pass through surviving enemies too.
/// </summary>
public class UpgradeArrowPierce : PlayerUpgrade
{
    public override string Id => "Arrow_Pierce";

    private PlayerStats playerStats;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        // Level 1: grant 1 pierce
        if (playerStats != null)
            playerStats.arrowPierceCount += 1;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        // Each additional level: +1 pierce
        if (playerStats != null)
            playerStats.arrowPierceCount += 1;
    }
}