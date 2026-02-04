using UnityEngine;

public class Upgrade_WallSlide_Damage : PlayerUpgrade
{
    public override string Id => "WallSlide_Damage";

    private int level;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        mgr.OnWallSlideTick += Tick;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void Tick(float dt)
    {
        Debug.Log($"[Wall Slide Damage] Tick x{level}");
    }
}

