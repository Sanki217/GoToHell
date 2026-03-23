using UnityEngine;

public class UpgradeBowExtraArrow : PlayerUpgrade
{
    public override string Id => "Bow_ExtraArrow";

    private int level;
    private PlayerShooting shooting;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        shooting = mgr.GetComponent<PlayerShooting>();

        mgr.OnWeakArrowFired += OnArrowFired;
        mgr.OnMediumArrowFired += OnArrowFired;
        mgr.OnChargedArrowFired += OnArrowFired;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void OnArrowFired(Vector3 dir, float speedMultiplier)
    {
        if (shooting == null)
            return;

        int arrowsToSpawn = level;

        for (int i = 0; i < arrowsToSpawn; i++)
        {
            float angle = GetSpreadAngle(i + 1);
            Vector3 newDir = Quaternion.Euler(0, 0, angle) * dir;

            shooting.SpawnExtraArrow(newDir, speedMultiplier);
        }
    }


    private float GetSpreadAngle(int index)
    {
        int step = (index + 1) / 2;   // 1,1,2,2,3,3...
        float angle = step * 5f;

        if (index % 2 == 0)
            angle = -angle;

        return angle;
    }
}
