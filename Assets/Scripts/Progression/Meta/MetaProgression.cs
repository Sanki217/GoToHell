using System;
using UnityEngine;

/// <summary>
/// Meta progression service: the persistent currency, owned ranks of
/// MetaUpgradeDefinitions, buying, refunding and the end-of-run payout.
/// All state lives in SaveData (metaCurrency, metaRanks) — this is logic only.
///
///   MetaProgression.Currency                       current balance
///   MetaProgression.GetRank(def)                   owned rank (0 = none)
///   MetaProgression.CanBuy(def, out reason)        for button states
///   MetaProgression.TryBuy(def)                    spends + saves
///   MetaProgression.ComputePayout(run)             preview a run's reward
///   MetaProgression.RefundAll()                    full respec
///   MetaProgression.OnChanged                      UI refresh hook
/// </summary>
public static class MetaProgression
{
    public static event Action OnChanged;

    private static MetaProgressionConfig config;

    public static MetaProgressionConfig Config
    {
        get
        {
            if (config == null)
            {
                config = Resources.Load<MetaProgressionConfig>("MetaProgressionConfig");
                if (config == null) config = ScriptableObject.CreateInstance<MetaProgressionConfig>();
            }
            return config;
        }
    }

    public static string CurrencyName => Config.currencyName;
    public static int Currency => SaveManager.Data.metaCurrency;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        config = null;
        OnChanged = null;
    }

    // ================================================================
    //  RANKS
    // ================================================================

    public static int GetRank(MetaUpgradeDefinition def)
    {
        if (def == null) return 0;
        foreach (MetaRank r in SaveManager.Data.metaRanks)
            if (r.id == def.metaId) return Mathf.Clamp(r.rank, 0, def.maxRank);
        return 0;
    }

    public static bool IsMaxed(MetaUpgradeDefinition def) => def != null && GetRank(def) >= def.maxRank;

    /// <summary>Hidden from the shop entirely (achievement not earned yet).</summary>
    public static bool IsHidden(MetaUpgradeDefinition def) =>
        def != null && def.requiredAchievement != null
        && !SaveManager.IsAchievementComplete(def.requiredAchievement.id);

    public static int NextCost(MetaUpgradeDefinition def) =>
        def == null || IsMaxed(def) ? 0 : def.CostForRank(GetRank(def) + 1);

    public static bool CanBuy(MetaUpgradeDefinition def, out string reason)
    {
        reason = "";
        if (def == null) { reason = "Missing"; return false; }
        if (IsHidden(def)) { reason = "Locked"; return false; }
        if (IsMaxed(def)) { reason = "Maxed"; return false; }
        if (def.requiredUpgrade != null && GetRank(def.requiredUpgrade) < def.requiredUpgradeRank)
        {
            reason = $"Requires {def.requiredUpgrade.displayName} {def.requiredUpgradeRank}";
            return false;
        }
        if (Currency < NextCost(def)) { reason = $"Need {NextCost(def)} {CurrencyName}"; return false; }
        return true;
    }

    public static bool TryBuy(MetaUpgradeDefinition def)
    {
        if (!CanBuy(def, out _)) return false;

        SaveData d = SaveManager.Data;
        d.metaCurrency -= NextCost(def);
        SetRank(d, def.metaId, GetRank(def) + 1);

        SaveManager.Save();
        Changed();
        return true;
    }

    /// <summary>Gives back everything spent on ranks (respec).</summary>
    public static int RefundAll()
    {
        SaveData d = SaveManager.Data;
        int refunded = 0;
        foreach (MetaUpgradeDefinition def in Content.MetaUpgrades.All)
        {
            int rank = GetRank(def);
            for (int r = 1; r <= rank; r++) refunded += def.CostForRank(r);
        }
        d.metaRanks.Clear();
        d.metaCurrency += refunded;

        SaveManager.Save();
        Changed();
        return refunded;
    }

    private static void SetRank(SaveData d, string id, int rank)
    {
        foreach (MetaRank r in d.metaRanks)
            if (r.id == id) { r.rank = rank; return; }
        d.metaRanks.Add(new MetaRank { id = id, rank = rank });
    }

    // ================================================================
    //  CURRENCY
    // ================================================================

    /// <summary>What a finished run pays, including meta-currency modifiers.</summary>
    public static int ComputePayout(RunStats run)
    {
        if (run == null) return 0;
        return Mathf.Max(0, Mathf.RoundToInt(Config.BasePayout(run) * RunModifiers.MetaCurrencyMult));
    }

    /// <summary>Adds currency. Pass save:false when the caller saves right after (RunManager).</summary>
    public static void AddCurrency(int amount, bool save = true)
    {
        if (amount == 0) return;
        SaveData d = SaveManager.Data;
        d.metaCurrency = Mathf.Max(0, d.metaCurrency + amount);
        if (amount > 0) d.metaCurrencyLifetime += amount;
        if (save) SaveManager.Save();
        Changed();
    }

    private static void Changed()
    {
        RunModifiers.MarkDirty();
        OnChanged?.Invoke();
    }
}
