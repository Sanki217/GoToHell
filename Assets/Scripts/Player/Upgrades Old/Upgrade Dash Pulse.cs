using UnityEngine;
public class UpgradeDashPulse : PlayerUpgrade
{
    public override string Id => "Dash_Pulse";

    private int level;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        mgr.OnDashEnded += OnDashEnd;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void OnDashEnd()
    {
        Debug.Log($"[Dash Pulse] Damage x{level}");
    }
}
