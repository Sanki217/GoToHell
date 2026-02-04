using UnityEngine;

public enum UpgradeCategory
{
    Bow,
    Dash,
    WallSlide,
    Hover,
    Misc
}

[CreateAssetMenu(menuName = "Upgrades/Player Upgrade")]
public class PlayerUpgradeData : ScriptableObject
{
    public string upgradeId;
    public string displayName;
    [TextArea] public string description;

    public UpgradeCategory category;

    public int maxLevel = 5;

    public bool unlockedByDefault = false;

    public GameObject upgradePrefab; // contains PlayerUpgradeBase
}
