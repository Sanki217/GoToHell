using UnityEngine;

/// <summary>
/// A single stat bonus on an upgrade card.
///
/// VALUE NORMALIZATION — all StatRangeEntry values use human-readable display units.
///
///   PERCENT STATS  (CritChance, LifeSteal, BurnStrength etc.)
///     Enter 5 for +5%, 20 for +20%. Stored internally as 0.05, 0.20.
///
///   INVERTED STATS  (DashCost, HoverDrainRate, ChargeDrainRate, ChargeDuration)
///     Lower internal value = better. Enter positive improvement.
///     DashCost 3 = dash costs 3 less energy (applied as -3 internally).
///     ChargeDuration 10 = 10% faster charge.
///
///   FLAT STATS  (ArrowDamage, MaxHP, MoveSpeed etc.)
///     Value used directly. 0.5 = +0.5 damage. 10 = +10 HP.
///
/// MAX HP SPECIAL CASE:
///   Also calls PlayerHealth.IncreaseMaxHP() to keep the HP slider max in sync.
/// </summary>
[System.Serializable]
public class UpgradeStatBonus
{
    public StatType statType;
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
                // Update PlayerStats
                s.maxHP += delta;
                // Update PlayerHealth so the slider max updates too
                PlayerHealth ph = s.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.IncreaseMaxHP(delta);
                else
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
            case StatType.DashCost: s.dashCost = Mathf.Max(5f, s.dashCost + v); break;
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

    public static float ToInternal(StatType t, float displayValue)
    {
        if (IsPercent(t)) return displayValue / 100f;
        if (IsInverted(t)) return -Mathf.Abs(displayValue);
        return displayValue;
    }

    // ================================================================
    //  DESCRIPTION
    // ================================================================

    public string GetDescription()
    {
        float shown = Mathf.Abs(value);
        string sign = "+";
        string amount = IsPercent(statType)
            ? $"{sign}{shown:F0}%"
            : $"{sign}{shown:F1}";
        return $"{amount} {GetName(statType)}";
    }

    // ================================================================
    //  STAT METADATA
    // ================================================================

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