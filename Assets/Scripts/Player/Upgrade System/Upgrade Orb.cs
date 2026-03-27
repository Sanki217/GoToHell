using UnityEngine;

/// <summary>
/// Dev testing tool — drop in scene to apply a specific upgrade without going through level-up UI.
/// Add new entries here as you implement and want to test each upgrade.
/// </summary>
public class UpgradeOrb : MonoBehaviour
{
    public enum UpgradeType
    {
        BurningArrow
        // Add more entries here as upgrades are implemented
    }

    public UpgradeType type;

    public void Apply(PlayerUpgradeManager mgr)
    {
        PlayerUpgrade upgrade = type switch
        {
            UpgradeType.BurningArrow => new UpgradeBurningArrow(),
            _ => null
        };

        if (upgrade != null)
            mgr.ApplyUpgrade(upgrade);

        Destroy(gameObject);
    }
}