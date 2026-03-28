using UnityEngine;

/// <summary>
/// Arrow Pierce — arrows pass through enemies even if they don't kill them.
/// Each level adds +20% arrow damage.
///
/// Implementation: sets a flag on each Arrow that disables the pierce stop logic.
/// Hooks into OnArrowHitEnemy — but the real work is done by modifying Arrow behavior
/// via a flag checked in Arrow.OnTriggerEnter.
///
/// Add "arrowPierces" bool to PlayerStats (or read from upgrade manager).
/// </summary>
public class UpgradeArrowPierce : PlayerUpgrade
{
    public override string Id => "Arrow_Pierce";

    private PlayerStats playerStats;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        playerStats = mgr.GetComponent<PlayerStats>();
        // Signal globally that arrows pierce
        if (playerStats != null) playerStats.arrowPierces = true;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        // Each level: +20% arrow damage
        if (playerStats != null) playerStats.arrowDamage += playerStats.arrowDamage * 0.2f;
    }
}