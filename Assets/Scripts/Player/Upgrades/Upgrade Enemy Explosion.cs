using UnityEngine;

public class Upgrade_Enemy_Explosion : PlayerUpgrade
{
    public override string Id => "Enemy_Explosion";

    private int level;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        mgr.OnEnemyKilled += OnKill;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void OnKill(GameObject enemy)
    {
        Debug.Log($"[Enemy Explosion] Level {level}");
    }
}
