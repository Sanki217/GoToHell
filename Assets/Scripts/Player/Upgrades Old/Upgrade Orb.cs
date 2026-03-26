using UnityEngine;

public class UpgradeOrb : MonoBehaviour
{
    public enum UpgradeType
    {
        BowExtraArrow,
        DashPulse,
        WallSlideDamage,
        HoverRegen,
        EnemyExplosion,
        BurningArrow
    }

    public UpgradeType type;

    public void Apply(PlayerUpgradeManager mgr)
    {
        PlayerUpgrade upgrade = type switch
        {
            UpgradeType.BowExtraArrow => new UpgradeBowExtraArrow(),
            UpgradeType.DashPulse => new UpgradeDashPulse(),
            UpgradeType.WallSlideDamage => new Upgrade_WallSlide_Damage(),
            UpgradeType.HoverRegen => new Upgrade_Hover_Regen(),
            UpgradeType.EnemyExplosion => new Upgrade_Enemy_Explosion(),
            UpgradeType.BurningArrow => new UpgradeBurningArrow(),
            _ => null
        };

        if (upgrade != null)
            mgr.ApplyUpgrade(upgrade);

        Destroy(gameObject);
    }
}