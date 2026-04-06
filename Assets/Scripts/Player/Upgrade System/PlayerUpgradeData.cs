using UnityEngine;

/// <summary>
/// One ScriptableObject per upgrade instance.
/// Choose the upgrade type from the dropdown — the upgradeId is set automatically.
/// Upgrades have ONE level, disappear from pool when owned.
/// Description uses colour-coded stat references computed from live values.
///
/// COLOUR CODES (match StatsUI):
///   Attack Damage  = #FF4444 (red)
///   Ability Power  = #4488FF (blue)
///   Luck           = #FFD700 (yellow)
///   Psyche         = #CC88FF (purple)
///   Health         = #44FF88 (green)
///   Size           = #FF8800 (orange)
///   Cooldown       = #FFFFFF (white)
///   Agility        = #00FFFF (cyan)
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/Player Upgrade Data")]
public class PlayerUpgradeData : ScriptableObject
{
    [Header("Upgrade Type — choose from dropdown")]
    [Tooltip("Selecting this automatically sets the upgradeId used to link to the C# upgrade class.")]
    public UpgradeType upgradeType = UpgradeType.None;

    // Auto-synced from upgradeType in OnValidate
    [HideInInspector] public string upgradeId;

    [Header("Display")]
    public string displayName;
    public Sprite icon;
    public UpgradeCategory category;

    [Header("Stat Bonus Count per Rarity")]
    public int statCountCommon = 1;
    public int statCountRare = 2;
    public int statCountEpic = 3;
    public int statCountLegendary = 4;

    [Header("Curse")]
    [Tooltip("Curses skip stat bonuses and cannot be re-offered once owned.")]
    public bool isCurse = false;

    // ================================================================
    //  BEHAVIOUR SETTINGS
    //  Key/value pairs read by the upgrade C# class.
    //  Shown in Inspector — all tunable without touching code.
    // ================================================================

    [Header("Behaviour Settings — all values tunable here")]
    public BehaviourSetting[] behaviourSettings;

    // ================================================================
    //  DESCRIPTION
    //  Written here in plain text. Use [KEY] tokens that get replaced
    //  with coloured computed values when the card is shown.
    //
    //  Available tokens (automatically coloured):
    //    [damage]      — computed damage (red)
    //    [radius]      — computed radius (orange)
    //    [duration]    — duration in seconds (white)
    //    [cooldown]    — computed cooldown (white)
    //    [burn]        — burn strength (blue)
    //    [bonus]       — soul/xp bonus % (yellow)
    //    [pierceCount] — pierce count (cyan)
    //    [pierceDmg]   — pierce bonus damage (blue)
    //    [chance]      — proc chance % (yellow)
    // ================================================================

    [Header("Description Template")]
    [Tooltip("Use tokens like [damage], [radius], [cooldown] etc. They are replaced with coloured computed values.")]
    [TextArea(3, 6)]
    public string descriptionTemplate;

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>
    /// Returns the description with all [tokens] replaced by computed coloured values.
    /// Pass playerStats to compute live values; pass null for a generic display.
    /// </summary>
    public string GetDescription(PlayerStats s = null)
    {
        if (string.IsNullOrEmpty(descriptionTemplate)) return "";

        string desc = descriptionTemplate;

        // Colours
        const string cAttack = "#FF4444";
        const string cAbility = "#4488FF";
        const string cSize = "#FF8800";
        const string cLuck = "#FFD700";
        const string cCool = "#FFFFFF";
        const string cAgility = "#00FFFF";

        // Pull behaviour values
        float dmgBase = GetSetting("damage", 0f);
        float dmgAPScale = GetSetting("damageAP", 0f);
        float radBase = GetSetting("radius", 0f);
        float radSizeScale = GetSetting("radiusSize", 0f);
        float dur = GetSetting("duration", 0f);
        float cdBase = GetSetting("cooldown", 0f);
        float cdReduction = GetSetting("cooldownStat", 0f);
        float bonusPct = GetSetting("bonusPercent", 0f);
        float chancePct = GetSetting("chance", 0f);
        float pierceCnt = GetSetting("pierceCount", 0f);
        float pierceDmg = GetSetting("pierceDamage", 0f);
        float pierceDmgAP = GetSetting("pierceDmgAP", 0f);

        // Compute live values if stats available
        float ap = s != null ? s.abilityPower : 0f;
        float sz = s != null ? s.size : 0f;
        float lk = s != null ? s.luck : 0f;
        float cd = s != null ? s.cooldown : 0f;

        float computedDmg = dmgBase + dmgAPScale * ap;
        float computedRad = radBase + radSizeScale * sz;
        float computedCd = cdBase > 0f
            ? Mathf.Max(GetSetting("cooldownMin", 1f), cdBase * (1f - cdReduction * cd))
            : 0f;
        float computedBonus = bonusPct + GetSetting("bonusLuck", 0f) * lk;
        float computedPDmg = pierceDmg + pierceDmgAP * ap;

        // Replace tokens
        desc = desc.Replace("[damage]", Colored($"{computedDmg:F0}", cAttack));
        desc = desc.Replace("[radius]", Colored($"{computedRad:F1}", cSize));
        desc = desc.Replace("[duration]", Colored($"{dur:F1}s", cCool));
        desc = desc.Replace("[cooldown]", Colored($"{computedCd:F1}s", cCool));
        desc = desc.Replace("[burn]", Colored($"{GetSetting("burnBonus", 0f) * 100f:F0}%", cAbility));
        desc = desc.Replace("[bonus]", Colored($"{computedBonus * 100f:F0}%", cLuck));
        desc = desc.Replace("[pierceCount]", Colored($"{(int)pierceCnt}", cAgility));
        desc = desc.Replace("[pierceDmg]", Colored($"{computedPDmg:F0}", cAbility));
        desc = desc.Replace("[chance]", Colored($"{chancePct * 100f:F0}%", cLuck));

        return desc;
    }

    private static string Colored(string value, string hex) =>
        $"<color={hex}>{value}</color>";

    public int GetStatCount(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => statCountCommon,
        UpgradeRarity.Rare => statCountRare,
        UpgradeRarity.Epic => statCountEpic,
        UpgradeRarity.Legendary => statCountLegendary,
        _ => statCountCommon
    };

    public float GetSetting(string key, float defaultValue = 0f)
    {
        if (behaviourSettings == null) return defaultValue;
        foreach (var s in behaviourSettings)
            if (s.key == key) return s.value;
        return defaultValue;
    }

    // ================================================================
    //  AUTO-SYNC upgradeId FROM upgradeType
    // ================================================================

#if UNITY_EDITOR
    private void OnValidate()
    {
        upgradeId = UpgradeTypeToId(upgradeType);
        if (string.IsNullOrEmpty(displayName) && upgradeType != UpgradeType.None)
            displayName = upgradeType.ToString().Replace("_", " ");
    }
#endif

    public static string UpgradeTypeToId(UpgradeType t) => t switch
    {
        UpgradeType.Arrow_Burn => "Arrow_Burn",
        UpgradeType.Arrow_Pierce => "Arrow_Pierce",
        UpgradeType.Status_Burn => "Status_Burn",
        UpgradeType.Status_Freeze => "Status_Freeze",
        UpgradeType.Status_Holy => "Status_Holy",
        UpgradeType.Status_Shock => "Status_Shock",
        UpgradeType.Enemy_Explosion => "Enemy_Explosion",
        UpgradeType.Soul_Bonus => "SoulBonus",
        UpgradeType.Immunity => "Immunity",
        UpgradeType.Bow_ExtraArrow => "Bow_ExtraArrow",
        UpgradeType.Dash_Pulse => "Dash_Pulse",
        UpgradeType.Hover_Regen => "Hover_Regen",
        UpgradeType.WallSlide_Damage => "WallSlide_Damage",
        _ => ""
    };
}

// ================================================================
//  ENUMS
// ================================================================

/// <summary>All registered upgrade types. Add new ones here + in UpgradeFactory.</summary>
public enum UpgradeType
{
    None,
    // Arrow
    Arrow_Burn,
    Arrow_Pierce,
    Bow_ExtraArrow,
    // Status
    Status_Burn,
    Status_Freeze,
    Status_Holy,
    Status_Shock,
    // Passive
    Enemy_Explosion,
    Soul_Bonus,
    Immunity,
    // Ability
    Dash_Pulse,
    Hover_Regen,
    WallSlide_Damage,
}

[System.Serializable]
public class BehaviourSetting
{
    [Tooltip("Key used by the upgrade C# class to read this value")]
    public string key;
    [Tooltip("Value returned when this key is requested")]
    public float value;
}

public enum UpgradeRarity { Common, Rare, Epic, Legendary }
public enum UpgradeCategory { Arrow, Dash, Passive, Conditional, Curse, Misc }