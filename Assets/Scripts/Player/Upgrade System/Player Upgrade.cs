using UnityEngine;

/// <summary>
/// Base class for all upgrade behaviours. Attach a subclass to an upgrade orb prefab.
/// All tuning values are exposed as serialized fields in the Inspector on the prefab.
///
/// The upgrade is identified by its C# type — no ID string required.
/// PlayerUpgradeManager tracks ownership using upgrade.GetType().Name.
///
/// OnAdded() is called once when the orb is collected by the player.
///
/// GetDynamicDescription(stats, simulatedBonuses) — override in each upgrade subclass
/// to return a rich-text string showing exact calculated values in stat colours.
/// The UI calls this instead of the static description field.
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

    // ================================================================
    //  DYNAMIC DESCRIPTION  — override in every upgrade subclass
    // ================================================================

    /// <summary>
    /// Returns rich-text description with live-calculated numbers coloured by their primary stat.
    /// stats = current player stats.
    /// simulatedBonuses = stat bonuses that would be granted IF this card is picked —
    ///                    apply them temporarily to the calculation so numbers reflect post-pick values.
    ///
    /// Default: returns the static description string.
    /// Each subclass overrides to show specific numbers.
    /// </summary>
    public virtual string GetDynamicDescription(PlayerStats stats,
                                                 System.Collections.Generic.List<UpgradeStatBonus> simulatedBonuses)
        => description;

    // ================================================================
    //  SHARED COLOUR HELPERS — use these in every subclass override
    // ================================================================

    protected static string AP(float value, string fmt = "F0")   // Psyche (Ability Power merged into Psyche) → pink
        => $"<color=#FF66CC>{value.ToString(fmt)}</color>";

    protected static string AD(float value, string fmt = "F0")   // Attack Damage → red
        => $"<color=#FF4444>{value.ToString(fmt)}</color>";

    protected static string SZ(float value, string fmt = "F1")   // Size → grey
        => $"<color=#AAAAAA>{value.ToString(fmt)}</color>";

    protected static string CD(float value, string fmt = "F1")   // Cooldown → cyan
        => $"<color=#44FFEE>{value.ToString(fmt)}</color>";

    protected static string LK(float value, string fmt = "F1")   // Luck → yellow-green
        => $"<color=#AAFF44>{value.ToString(fmt)}</color>";

    protected static string HP(float value, string fmt = "F0")   // Health → green
        => $"<color=#44FF88>{value.ToString(fmt)}</color>";

    protected static string AG(float value, string fmt = "F1")   // Agility → orange
        => $"<color=#FF9933>{value.ToString(fmt)}</color>";

    protected static string PSY(float value, string fmt = "F1")  // Psyche → pink
        => $"<color=#FF66CC>{value.ToString(fmt)}</color>";

    /// <summary>
    /// Given current stats and the simulated stat bonuses for this card,
    /// returns a PlayerStats-like snapshot with the bonuses applied so damage
    /// numbers reflect post-pick values. Does NOT modify the real PlayerStats.
    /// </summary>
    protected static SimulatedStats Simulate(PlayerStats real,
                                              System.Collections.Generic.List<UpgradeStatBonus> bonuses)
    {
        var s = new SimulatedStats(real);
        if (bonuses != null)
        {
            foreach (var b in bonuses)
            {
                switch (b.stat)
                {
                    case PrimaryStat.AttackDamage: s.attackDamage += b.value; break;
                    case PrimaryStat.Size: s.size += b.value; break;
                    case PrimaryStat.Cooldown: s.cooldown += b.value; break;
                    case PrimaryStat.Luck: s.luck += b.value; break;
                    case PrimaryStat.Health: s.health += b.value; break;
                    case PrimaryStat.Agility: s.agility += b.value; break;
                    case PrimaryStat.Psyche: s.psyche += b.value; break;
                }
            }
        }
        return s;
    }
}

// ================================================================
//  SimulatedStats — lightweight snapshot used only for description preview
// ================================================================

public class SimulatedStats
{
    public float attackDamage;
    public float size;
    public float cooldown;
    public float luck;
    public float health;
    public float agility;
    public float psyche;

    // Derived fields mirroring PlayerStats formulas — only the ones upgrades need
    public float burnStrength;
    public float freezeStrength;
    public float holyStrength;
    public float shockStrength;
    public float arrowDamage;

    public SimulatedStats(PlayerStats real)
    {
        if (real == null) return;
        attackDamage = real.attackDamage;
        size = real.size;
        cooldown = real.cooldown;
        luck = real.luck;
        health = real.health_stat;
        agility = real.agility;
        psyche = real.psyche;

        // Copy derived
        burnStrength = real.burnStrength;
        freezeStrength = real.freezeStrength;
        holyStrength = real.holyStrength;
        shockStrength = real.shockStrength;
        arrowDamage = real.arrowDamage;
    }
}