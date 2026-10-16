using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// A permanent upgrade bought between runs with meta currency (the
/// "mirror / house" layer of a roguelite). Ranked: each rank applies
/// <see cref="perRank"/> once more and costs more than the last.
///
///   "Thick Skin"   maxRank 5, perRank.bonusMaxHP = 5      → up to +25 max HP
///   "Soul Hunger"  maxRank 3, perRank.soulGainPercent = 10 → up to +30% souls
///   "Second Wind"  maxRank 1, effects: ExtraLifeEffect
///
/// DATA-ONLY: create the asset, it appears in the meta shop.
///
/// Create via: Assets → Create → GoToHell → Meta Upgrade
/// Store in:   Assets/Resources/Meta/
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Meta Upgrade")]
public class MetaUpgradeDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string metaId;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    [Tooltip("Shop grouping, e.g. Body / Soul / Arsenal.")]
    public string category = "General";
    [Tooltip("Lower = earlier in the shop.")]
    public int sortOrder;

    [Header("Ranks & Cost")]
    [Min(1)] public int maxRank = 5;
    [Min(0)] public int baseCost = 50;
    [Tooltip("Each rank costs this much more than the previous (1.5 = +50%).")]
    [Min(1f)] public float costGrowth = 1.5f;
    [Tooltip("Optional exact cost per rank (index 0 = rank 1). Overrides base/growth where set.")]
    public int[] costOverrides;

    [Header("Requirements (optional)")]
    [Tooltip("Hidden until this achievement is completed.")]
    public AchievementDefinition requiredAchievement;
    [Tooltip("Locked until another meta upgrade reaches a rank (skill-tree style).")]
    public MetaUpgradeDefinition requiredUpgrade;
    [Min(1)] public int requiredUpgradeRank = 1;

    [Header("Effect — applied once per owned rank")]
    public RunModifierSet perRank = new RunModifierSet();

    [Header("Custom Effects (stacks = rank)")]
    [SerializeReference, SubclassSelector]
    public List<RunEffect> effects = new List<RunEffect>();

    /// <summary>Cost to go from rank (nextRank − 1) to nextRank.</summary>
    public int CostForRank(int nextRank)
    {
        int i = nextRank - 1;
        if (costOverrides != null && i >= 0 && i < costOverrides.Length && costOverrides[i] > 0)
            return costOverrides[i];
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, Mathf.Max(0, i)));
    }

    /// <summary>Total cost of every rank (balance sheet).</summary>
    public int TotalCost()
    {
        int sum = 0;
        for (int r = 1; r <= maxRank; r++) sum += CostForRank(r);
        return sum;
    }

    public string Describe(int rank)
    {
        var sb = new StringBuilder();
        int stacks = Mathf.Max(1, rank);
        string mods = perRank != null ? perRank.Describe(stacks) : "";
        sb.Append(mods);
        if (effects != null)
            foreach (RunEffect e in effects)
                if (e != null) { if (sb.Length > 0) sb.Append('\n'); sb.Append(e.Describe(stacks)); }
        return sb.ToString();
    }
}
