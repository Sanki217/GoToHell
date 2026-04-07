using UnityEngine;

/// <summary>
/// Base class for all upgrade behaviours. Attach a subclass to an upgrade orb prefab.
/// All tuning values are exposed as serialized fields in the Inspector on the prefab.
///
/// The upgrade is identified by its C# type — no ID string required.
/// PlayerUpgradeManager tracks ownership using upgrade.GetType().Name.
///
/// OnAdded() is called once when the orb is collected by the player.
/// </summary>
public abstract class PlayerUpgrade : MonoBehaviour
{
    [Header("Upgrade Display")]
    public string displayName;
    [TextArea(2, 5)]
    public string description;
    public Sprite icon;
    public UpgradeCategory category;
    public UpgradeRarity rarity;   // set per-prefab; used by UI for colour

    [Header("Stat Bonus Count per Rarity")]
    [Tooltip("How many primary stat bonuses are rolled when offered at each rarity.")]
    public int statCountCommon = 1;
    public int statCountRare = 2;
    public int statCountEpic = 3;
    public int statCountLegendary = 3;

    /// <summary>Unique ownership key — derived from class name, no manual setup needed.</summary>
    public string UpgradeId => GetType().Name;

    public int GetStatCount(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => statCountCommon,
        UpgradeRarity.Rare => statCountRare,
        UpgradeRarity.Epic => statCountEpic,
        UpgradeRarity.Legendary => statCountLegendary,
        _ => statCountCommon
    };

    /// <summary>Called once when the orb arrives at the player. Wire up events here.</summary>
    public virtual void OnAdded(PlayerUpgradeManager mgr) { }
}