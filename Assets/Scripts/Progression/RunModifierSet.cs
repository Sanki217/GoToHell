using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>A flat amount of one primary stat (e.g. +2 Luck).</summary>
[Serializable]
public struct PrimaryStatAmount
{
    public PrimaryStat stat;
    public float amount;
}

/// <summary>
/// A bundle of NUMERIC run modifiers, used by meta upgrades (and any future
/// source: relics, difficulty modes…). Filled in the Inspector; no code.
///
///   Meta "Thick Skin" ×3:  bonusMaxHP 5 per rank → +15 max HP
///
/// Percent fields are ADDITIVE across all sources and turned into one
/// multiplier by RunModifiers: +50 and −20 → ×1.30. Anything that needs real
/// logic (grant an upgrade, start with an extra life…) is a RunEffect instead.
///
/// ADDING A MODIFIER: add a field here, sum it in RunModifiers.Rebuild, read
/// it where it applies, and add a line to Describe(). That's all.
/// </summary>
[Serializable]
public class RunModifierSet
{
    [Header("Player — flat, applied when the player spawns")]
    public List<PrimaryStatAmount> primaryStats = new List<PrimaryStatAmount>();
    public float bonusMaxHP;
    [Tooltip("Souls in the wallet at the start of a run (Layer 1 only).")]
    public int startingSouls;

    [Header("Economy — percent (+25 = 25% more)")]
    public float soulGainPercent;
    public float xpGainPercent;
    [Tooltip("Shop prices. −20 = 20% cheaper.")]
    public float shopPricePercent;
    [Tooltip("Meta currency earned at the end of the run.")]
    public float metaCurrencyPercent;

    [Header("Survival — percent")]
    [Tooltip("Damage the player takes. +50 = 50% more, −25 = 25% less.")]
    public float damageTakenPercent;

    [Header("Enemies — percent")]
    public float enemyHealthPercent;
    public float enemyDamagePercent;
    public float enemySpeedPercent;

    [Header("Upgrades")]
    [Tooltip("Extra Luck used ONLY for rarity rolls (level-ups and chests).")]
    public float rarityLuck;
    [Tooltip("Upgrades with any of these tags are offered more often.")]
    public UpgradeTag favouredTags;
    [Tooltip("How much more often favoured upgrades appear (+100 = twice as likely).")]
    public float favouredTagWeightPercent = 100f;
    [Tooltip("Upgrades with any of these tags are never offered.")]
    public UpgradeTag bannedTags;

    /// <summary>Human-readable summary for menus, tooltips and the balance sheet.</summary>
    public string Describe(int stacks = 1)
    {
        var sb = new StringBuilder();
        if (primaryStats != null)
            foreach (PrimaryStatAmount p in primaryStats)
                if (p.amount != 0f) Line(sb, $"{Signed(p.amount * stacks)} {p.stat}");
        if (bonusMaxHP != 0f)     Line(sb, $"{Signed(bonusMaxHP * stacks)} Max HP");
        if (startingSouls != 0)   Line(sb, $"Start with {startingSouls * stacks} souls");
        Pct(sb, soulGainPercent * stacks, "souls gained");
        Pct(sb, xpGainPercent * stacks, "XP gained");
        Pct(sb, shopPricePercent * stacks, "shop prices");
        Pct(sb, metaCurrencyPercent * stacks, "meta currency");
        Pct(sb, damageTakenPercent * stacks, "damage taken");
        Pct(sb, enemyHealthPercent * stacks, "enemy health");
        Pct(sb, enemyDamagePercent * stacks, "enemy damage");
        Pct(sb, enemySpeedPercent * stacks, "enemy speed");
        if (rarityLuck != 0f) Line(sb, $"{Signed(rarityLuck * stacks)} rarity luck");
        if (favouredTags != UpgradeTag.None)
            Line(sb, $"{favouredTags} upgrades {Signed(favouredTagWeightPercent)}% more common");
        if (bannedTags != UpgradeTag.None) Line(sb, $"No {bannedTags} upgrades");
        return sb.ToString();
    }

    private static void Pct(StringBuilder sb, float v, string what)
    {
        if (v != 0f) Line(sb, $"{Signed(v)}% {what}");
    }

    private static string Signed(float v) => v > 0f ? $"+{v:0.##}" : v.ToString("0.##");

    private static void Line(StringBuilder sb, string s)
    {
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(s);
    }
}
