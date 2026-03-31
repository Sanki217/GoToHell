using UnityEngine;

/// <summary>
/// A single stat bonus on an upgrade card.
///
/// VALUE NORMALIZATION
/// All StatRangeEntry values in PlayerUpgradeData are in human-readable units.
/// Apply() converts them before writing to PlayerStats:
///
///   PERCENT STATS  (CritChance, LifeSteal, BurnStrength etc.)
///     Inspector: 5   means +5%   → internally added as +0.05
///     Inspector: 20  means +20%  → internally added as +0.20
///
///   INVERTED STATS  (DashCost, HoverDrainRate, ChargeDrainRate, ChargeDuration)
///     These are stats where lower = better for the player.
///     Inspector: 5   means "5 improvement" → internally added as -5
///     You ALWAYS enter a positive number. The system negates it.
///     e.g. DashCost 3 = dash costs 3 less energy
///          ChargeDuration 10 = 10% faster charge (0.1s less)
///
///   FLAT STATS  (ArrowDamage, MaxHP, MoveSpeed etc.)
///     Inspector value is used directly. 0.5 = +0.5 damage. 10 = +10 HP.
/// </summary>
[System.Serializable]
public class UpgradeStatBonus
{
    public StatType statType;

    [Tooltip("Value in DISPLAY units. See UpgradeStatBonus documentation. " +
             "Percent stats: enter 5 for +5%. Inverted stats: enter positive improvement.")]
    public float value;

    // ================================================================
    //  APPLY
    // ================================================================

    public void Apply(PlayerStats s)
    {
        float v = ToInternal(statType, value);

        switch (statType)
        {
            case StatType.MaxHP:
                int delta = Mathf.RoundToInt(v);
                s.maxHP += delta;
                s.CurrentHP = Mathf.Min(s.CurrentHP + delta, s.maxHP);
                break;
            case StatType.ArrowDamage: s.arrowDamage += v; break;
            case StatType.DashDamage: s.dashDamage += v; break;
            case StatType.CritChance: s.critChance += v; break;
            case StatType.CritMultiplier: s.critMultiplier += v; break;
            case StatType.KnockbackForce: s.knockbackForce += v; break;
            case StatType.BurnStrength: s.burnStrength += v; break;
            case StatType.FreezeStrength: s.freezeStrength += v; break;
            case StatType.HolyStrength: s.holyStrength += v; break;
            case StatType.ShockStrength: s.shockStrength += v; break;
            case StatType.MoveSpeed: s.moveSpeed += v; break;
            case StatType.DashDistance: s.dashDistance += v; break;
            case StatType.DashCost: s.dashCost += v; break;
            case StatType.DashInvincibility: s.dashInvincibilityWindow += v; break;
            case StatType.MaxEnergy: s.maxEnergy += v; break;
            case StatType.HoverDrainRate: s.hoverDrainRate += v; break;
            case StatType.ChargeDrainRate: s.arrowChargeDrainRate += v; break;
            case StatType.ChargeDuration: s.arrowChargeDuration += v; break;
            case StatType.LifeSteal: s.lifeSteal += v; break;
            case StatType.LootRange: s.lootRange += v; break;
            case StatType.Luck: s.luck += v; break;
        }
    }

    // ================================================================
    //  CONVERSION
    // ================================================================

    /// <summary>
    /// Convert a display-unit value to the internal value added to PlayerStats.
    ///   Percent stats:  divide by 100 (5 → 0.05)
    ///   Inverted stats: negate      (+5 → -5)
    ///   Flat stats:     unchanged
    /// </summary>
    public static float ToInternal(StatType t, float displayValue)
    {
        if (IsPercent(t)) return displayValue / 100f;
        if (IsInverted(t)) return -Mathf.Abs(displayValue);
        return displayValue;
    }

    // ================================================================
    //  DESCRIPTION  (always human-readable)
    // ================================================================

    public string GetDescription()
    {
        // For inverted stats show as positive improvement even though it's a decrease
        float shown = Mathf.Abs(value);
        bool improving = IsInverted(statType) ? value > 0f : value > 0f;
        string sign = improving ? "+" : "-";
        string amount = IsPercent(statType)
            ? $"{sign}{shown:F0}%"
            : $"{sign}{shown:F1}";
        return $"{amount} {GetName(statType)}";
    }

    // ================================================================
    //  STAT METADATA
    // ================================================================

    /// <summary>
    /// Percent stats are stored as fractions (0–1) in PlayerStats.
    /// Display value of 5 means 5% → stored as 0.05.
    /// </summary>
    public static bool IsPercent(StatType t) => t switch
    {
        StatType.CritChance => true,
        StatType.CritMultiplier => true,
        StatType.BurnStrength => true,
        StatType.FreezeStrength => true,
        StatType.HolyStrength => true,
        StatType.ShockStrength => true,
        StatType.LifeSteal => true,
        _ => false
    };

    /// <summary>
    /// Inverted stats: lower value = better for the player.
    /// Display value is positive improvement; applied as negative.
    /// </summary>
    public static bool IsInverted(StatType t) => t switch
    {
        StatType.DashCost => true,
        StatType.HoverDrainRate => true,
        StatType.ChargeDrainRate => true,
        StatType.ChargeDuration => true,
        _ => false
    };

    public static string GetName(StatType t) => t switch
    {
        StatType.MaxHP => "Max HP",
        StatType.ArrowDamage => "Arrow Damage",
        StatType.DashDamage => "Dash Damage",
        StatType.CritChance => "Crit Chance",
        StatType.CritMultiplier => "Crit Multiplier",
        StatType.KnockbackForce => "Knockback",
        StatType.BurnStrength => "Burn Strength",
        StatType.FreezeStrength => "Freeze Strength",
        StatType.HolyStrength => "Holy Strength",
        StatType.ShockStrength => "Shock Strength",
        StatType.MoveSpeed => "Move Speed",
        StatType.DashDistance => "Dash Distance",
        StatType.DashCost => "Dash Cost",
        StatType.DashInvincibility => "Dash Invincibility",
        StatType.MaxEnergy => "Max Energy",
        StatType.HoverDrainRate => "Hover Drain",
        StatType.ChargeDrainRate => "Charge Drain",
        StatType.ChargeDuration => "Charge Speed",
        StatType.LifeSteal => "Lifesteal",
        StatType.LootRange => "Loot Range",
        StatType.Luck => "Luck",
        _ => t.ToString()
    };
}

public enum StatType
{
    MaxHP, ArrowDamage, DashDamage, CritChance, CritMultiplier,
    KnockbackForce, BurnStrength, FreezeStrength, HolyStrength, ShockStrength,
    MoveSpeed, DashDistance, DashCost, DashInvincibility, MaxEnergy,
    HoverDrainRate, ChargeDrainRate, ChargeDuration, LifeSteal, LootRange, Luck
}