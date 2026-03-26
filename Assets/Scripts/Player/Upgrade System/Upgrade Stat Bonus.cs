using UnityEngine;

/// <summary>
/// A single stat bonus attached to an upgrade at a given rarity.
/// Value is a delta added to the PlayerStats field.
/// Negative values decrease stats (e.g. DashCost -5 = costs 5 less energy).
/// </summary>
[System.Serializable]
public class UpgradeStatBonus
{
    public StatType statType;
    public float value;

    public void Apply(PlayerStats s)
    {
        switch (statType)
        {
            case StatType.MaxHP:
                s.maxHP += Mathf.RoundToInt(value);
                s.CurrentHP = Mathf.Min(s.CurrentHP + Mathf.RoundToInt(value), s.maxHP);
                break;
            case StatType.ArrowDamage: s.arrowDamage += value; break;
            case StatType.DashDamage: s.dashDamage += value; break;
            case StatType.CritChance: s.critChance += value; break;
            case StatType.CritMultiplier: s.critMultiplier += value; break;
            case StatType.KnockbackForce: s.knockbackForce += value; break;
            case StatType.BurnStrength: s.burnStrength += value; break;
            case StatType.FreezeStrength: s.freezeStrength += value; break;
            case StatType.HolyStrength: s.holyStrength += value; break;
            case StatType.ShockStrength: s.shockStrength += value; break;
            case StatType.MoveSpeed: s.moveSpeed += value; break;
            case StatType.DashDistance: s.dashDistance += value; break;
            case StatType.DashCost: s.dashCost += value; break;
            case StatType.DashInvincibility: s.dashInvincibilityWindow += value; break;
            case StatType.MaxEnergy: s.maxEnergy += value; break;
            case StatType.HoverDrainRate: s.hoverDrainRate += value; break;
            case StatType.ChargeDrainRate: s.arrowChargeDrainRate += value; break;
            case StatType.ChargeDuration: s.arrowChargeDuration += value; break;
            case StatType.LifeSteal: s.lifeSteal += value; break;
            case StatType.LootRange: s.lootRange += value; break;
            case StatType.Luck: s.luck += value; break;
        }
    }

    /// <summary>Short human-readable string for upgrade card UI.</summary>
    public string GetDescription()
    {
        string sign = value >= 0f ? "+" : "";
        string amount = IsPercent(statType)
            ? $"{sign}{value * 100f:F0}%"
            : $"{sign}{value:F1}";
        return $"{amount} {GetName(statType)}";
    }

    private static bool IsPercent(StatType t) => t switch
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