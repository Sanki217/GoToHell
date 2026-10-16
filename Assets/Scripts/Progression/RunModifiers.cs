using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// The run's combined modifiers from every source — active pacts (RunConfig)
/// and owned meta-upgrade ranks (SaveData) — flattened into plain numbers
/// that gameplay reads:
///
///   PlayerInventory.AddSouls    × SoulGainMult
///   PlayerLevelSystem.AddXP     × XpMult
///   PlayerHealth.TakeDamage     × DamageTakenMult
///   ShopItem price              × ShopPriceMult
///   Enemy health/damage/speed   × EnemyHealthMult / EnemyDamageMult / EnemySpeedMult
///   Level-up + chest rarity     + RarityLuck
///   PlayerUpgradePool           FavouredTags / BannedTags
///   RunConfigApplier            ApplyToPlayer (stats, max HP, souls, RunEffects)
///
/// Rebuilt lazily whenever a source changes (pact picked, meta rank bought,
/// run started). Gameplay never needs to know WHERE a modifier came from —
/// a new source (relics, curses, difficulty modes) is one more loop in
/// Rebuild().
/// </summary>
public static class RunModifiers
{
    // ---- Aggregated values ---------------------------------------------
    private static bool dirty = true;

    private static float soulGain, xpGain, shopPrice, metaCurrency, damageTaken;
    private static float enemyHealth, enemyDamage, enemySpeed;
    private static float rarityLuck, favouredWeight, bonusMaxHP;
    private static int startingSouls, heat;
    private static UpgradeTag favouredTags, bannedTags;
    private static readonly float[] primary = new float[System.Enum.GetValues(typeof(PrimaryStat)).Length];

    // A source = one pact or one owned meta upgrade, with its stack count
    private struct Source
    {
        public RunModifierSet set;
        public List<RunEffect> effects;
        public int stacks;
    }
    private static readonly List<Source> sources = new List<Source>();

    // ---- Public read API -------------------------------------------------

    public static float SoulGainMult     { get { Ensure(); return Mult(soulGain); } }
    public static float XpMult           { get { Ensure(); return Mult(xpGain); } }
    public static float ShopPriceMult    { get { Ensure(); return Mult(shopPrice); } }
    public static float MetaCurrencyMult { get { Ensure(); return Mult(metaCurrency); } }
    public static float DamageTakenMult  { get { Ensure(); return Mult(damageTaken); } }
    public static float EnemyHealthMult  { get { Ensure(); return Mult(enemyHealth); } }
    public static float EnemyDamageMult  { get { Ensure(); return Mult(enemyDamage); } }
    public static float EnemySpeedMult   { get { Ensure(); return Mult(enemySpeed); } }
    public static float RarityLuck       { get { Ensure(); return rarityLuck; } }
    public static UpgradeTag FavouredTags { get { Ensure(); return favouredTags; } }
    public static UpgradeTag BannedTags   { get { Ensure(); return bannedTags; } }
    /// <summary>Weight multiplier for favoured-tag upgrades (2 = twice as likely).</summary>
    public static float FavouredTagMult  { get { Ensure(); return Mult(favouredWeight); } }
    public static int StartingSouls      { get { Ensure(); return startingSouls; } }
    public static float BonusMaxHP       { get { Ensure(); return bonusMaxHP; } }
    /// <summary>Summed heat of the active pacts.</summary>
    public static int TotalHeat          { get { Ensure(); return heat; } }

    public static float PrimaryBonus(PrimaryStat stat)
    {
        Ensure();
        int i = (int)stat;
        return i >= 0 && i < primary.Length ? primary[i] : 0f;
    }

    /// <summary>Call when a source changes. Values rebuild on next read.</summary>
    public static void MarkDirty() => dirty = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => dirty = true;

    // ---- Active pacts ----------------------------------------------------

    /// <summary>Active pact definitions (resolved from RunConfig.selectedPactIds).</summary>
    public static IEnumerable<PactDefinition> ActivePacts()
    {
        RunConfig cfg = RunConfig.I;
        if (cfg == null || cfg.selectedPactIds == null) yield break;
        foreach (string id in cfg.selectedPactIds)
        {
            PactDefinition p = Content.Pacts.Get(id);
            if (p != null) yield return p;
        }
    }

    // ---- Apply to the player -------------------------------------------

    /// <summary>
    /// Applies flat bonuses and RunEffects to a freshly spawned player.
    /// Called once per player instance by RunConfigApplier.
    /// </summary>
    public static void ApplyToPlayer(GameObject player, bool isRunStart)
    {
        if (player == null) return;
        Ensure();

        if (player.TryGetComponent(out PlayerStats stats))
        {
            for (int i = 0; i < primary.Length; i++)
                if (primary[i] != 0f) stats.AddPrimary((PrimaryStat)i, primary[i]);
            if (bonusMaxHP != 0f) stats.AddBonusMaxHP(bonusMaxHP);
        }

        if (isRunStart && startingSouls > 0 && player.TryGetComponent(out PlayerInventory inv))
            inv.AddBonusSouls(startingSouls);

        foreach (Source s in sources)
        {
            if (s.effects == null) continue;
            var ctx = new RunEffectContext(player, s.stacks, isRunStart);
            foreach (RunEffect e in s.effects)
            {
                if (e == null) continue;
                try { e.Apply(ctx); }
                catch (System.Exception ex) { Debug.LogException(ex); }   // one broken effect never blocks the run
            }
        }
    }

    /// <summary>Multi-line summary of everything active (pause menu / debug).</summary>
    public static string DescribeActive()
    {
        Ensure();
        var sb = new StringBuilder();
        foreach (Source s in sources)
        {
            string d = s.set != null ? s.set.Describe(s.stacks) : "";
            if (d.Length == 0) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(d);
        }
        return sb.ToString();
    }

    // ---- Rebuild ---------------------------------------------------------

    private static void Ensure()
    {
        if (dirty) Rebuild();
    }

    private static void Rebuild()
    {
        dirty = false;
        sources.Clear();
        heat = 0;

        foreach (PactDefinition p in ActivePacts())
        {
            sources.Add(new Source { set = p.modifiers, effects = p.effects, stacks = 1 });
            heat += p.heat;
        }

        foreach (MetaUpgradeDefinition m in Content.MetaUpgrades.All)
        {
            int rank = MetaProgression.GetRank(m);
            if (rank > 0) sources.Add(new Source { set = m.perRank, effects = m.effects, stacks = rank });
        }

        soulGain = xpGain = shopPrice = metaCurrency = damageTaken = 0f;
        enemyHealth = enemyDamage = enemySpeed = 0f;
        rarityLuck = favouredWeight = bonusMaxHP = 0f;
        startingSouls = 0;
        favouredTags = bannedTags = UpgradeTag.None;
        System.Array.Clear(primary, 0, primary.Length);

        foreach (Source s in sources)
        {
            RunModifierSet m = s.set;
            if (m == null) continue;
            int k = s.stacks;

            if (m.primaryStats != null)
                foreach (PrimaryStatAmount p in m.primaryStats)
                {
                    int i = (int)p.stat;
                    if (i >= 0 && i < primary.Length) primary[i] += p.amount * k;
                }
            bonusMaxHP    += m.bonusMaxHP * k;
            startingSouls += m.startingSouls * k;

            soulGain     += m.soulGainPercent * k;
            xpGain       += m.xpGainPercent * k;
            shopPrice    += m.shopPricePercent * k;
            metaCurrency += m.metaCurrencyPercent * k;
            damageTaken  += m.damageTakenPercent * k;
            enemyHealth  += m.enemyHealthPercent * k;
            enemyDamage  += m.enemyDamagePercent * k;
            enemySpeed   += m.enemySpeedPercent * k;
            rarityLuck   += m.rarityLuck * k;

            if (m.favouredTags != UpgradeTag.None)
            {
                favouredTags |= m.favouredTags;
                favouredWeight += m.favouredTagWeightPercent;   // not stacked by rank: it's a rule, not a number
            }
            bannedTags |= m.bannedTags;
        }
    }

    private static float Mult(float percent) => Mathf.Max(0f, 1f + percent / 100f);
}
