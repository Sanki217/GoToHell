using UnityEngine;

public class UpgradeBowExtraArrow : PlayerUpgrade
{
    public override string Id => "Bow_ExtraArrow";

    private int level;
    private PlayerShooting shooting;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        shooting = mgr.GetComponent<PlayerShooting>();
        mgr.OnArrowFired += OnArrowFired;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void OnArrowFired(Vector3 dir)
    {
        if (shooting == null) return;

        // Fire (level) extra arrows in a small cone
        float spread = 20f;

        for (int i = 0; i < level; i++)
        {
            float angle = Random.Range(-spread, spread);
            Vector3 newDir = Quaternion.Euler(0, 0, angle) * dir;

            shooting.ForceShoot(newDir); // we add this method
        }
    }
}
