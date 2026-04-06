using UnityEngine;

/// <summary>
/// A single primary stat bonus on an upgrade card.
/// Upgrades now grant primary stats (Agility, AttackDamage, etc.) only.
/// The derived stats (arrowDamage, moveSpeed, etc.) update automatically via RecalculateDerived().
///
/// VALUE: always positive float (e.g. 0.3, 1.2, 2.5).
///        Rolled in 0.1 increments from the global pool ranges, scaled by luck.
/// </summary>
[System.Serializable]
public class UpgradeStatBonus
{
    public PrimaryStat stat;
    public float value;  // always positive, 0.1 float precision

    /// <summary>Apply this bonus to PlayerStats and trigger recalculation.</summary>
    public void Apply(PlayerStats s)
    {
        s.AddPrimary(stat, value);
    }

    /// <summary>Human-readable card description: "+1.2 Agility"</summary>
    public string GetDescription()
    {
        return $"+{value:F1} {GetName(stat)}";
    }

    /// <summary>
    /// Shows current → new value with green arrow for upgrade card preview.
    /// Returns a string like "Agility  3.0 → 3.5"
    /// </summary>
    public string GetPreviewLine(PlayerStats s)
    {
        float current = s.GetPrimary(stat);
        float next = current + value;
        return $"{GetName(stat)}  {current:F1} → <color=#00FF88>{next:F1}</color>";
    }

    public static string GetName(PrimaryStat s) => s switch
    {
        PrimaryStat.Agility => "Agility",
        PrimaryStat.AttackDamage => "Attack Damage",
        PrimaryStat.AbilityPower => "Ability Power",
        PrimaryStat.Luck => "Luck",
        PrimaryStat.Psyche => "Psyche",
        PrimaryStat.Health => "Health",
        PrimaryStat.Size => "Size",
        PrimaryStat.Cooldown => "Cooldown",
        _ => s.ToString()
    };
}