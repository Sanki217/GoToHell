using UnityEngine;

public class UpgradeBowExtraArrow : PlayerUpgrade
{
    public override string Id => "Bow_ExtraArrow";

    private int level;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        mgr.OnArrowFired += OnArrowFired;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void OnArrowFired(Vector3 dir)
    {
        Debug.Log($"[Bow Extra Arrow] Level {level}");
        // Later: spawn arrows here
    }
}
