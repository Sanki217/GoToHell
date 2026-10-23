using System;
using System.Collections.Generic;

/// <summary>
/// One saved character (a slot on the Character Select screen). Lives in
/// SaveData.characters. The class is permanent; the weapon is equipped in the
/// town and can be swapped. Every creation stat starts at 1 and the allocated
/// points are what the player added at creation.
/// </summary>
[Serializable]
public class CharacterData
{
    public string id;               // stable GUID — runs reference it
    public string name;
    public string classId;
    public string createdUtc;

    // Free points spent at creation, on top of the base 1
    public int agility;
    public int attackDamage;
    public int luck;
    public int psyche;
    public int health;
    public int size;
    public int cooldown;

    // Hardcore: one death and the character is gone for good
    public bool hardcore;
    public bool dead;

    // Pact used last time — pre-selected on the Pact Select screen
    public string lastPactId;

    // Weapon (town weapon rack) + smith levels of every weapon this character has upgraded
    public string equippedWeaponId = "";
    public List<WeaponLevelEntry> weaponLevels = new List<WeaponLevelEntry>();

    // Character level: XP from runs (CharacterProgressionConfig). Each level
    // past 1 gives a talent point (talent tree comes later).
    public int level = 1;
    public int xp;                  // progress inside the current level
    public int spentTalentPoints;

    public int TalentPoints => Math.Max(0, level - 1 - spentTalentPoints);

    public int WeaponLevel(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId) || weaponLevels == null) return 0;
        foreach (WeaponLevelEntry e in weaponLevels)
            if (e.weaponId == weaponId) return e.level;
        return 0;
    }

    public void SetWeaponLevel(string weaponId, int value)
    {
        if (string.IsNullOrEmpty(weaponId)) return;
        weaponLevels ??= new List<WeaponLevelEntry>();
        foreach (WeaponLevelEntry e in weaponLevels)
            if (e.weaponId == weaponId) { e.level = value; return; }
        weaponLevels.Add(new WeaponLevelEntry { weaponId = weaponId, level = value });
    }

    // Record
    public int runsPlayed;
    public int victories;
    public int deepestLayer;
    public float bestDepth;
    public int totalKills;

    public int Allocated(PrimaryStat stat) => stat switch
    {
        PrimaryStat.Agility      => agility,
        PrimaryStat.AttackDamage => attackDamage,
        PrimaryStat.Luck         => luck,
        PrimaryStat.Psyche       => psyche,
        PrimaryStat.Health       => health,
        PrimaryStat.Size         => size,
        PrimaryStat.Cooldown     => cooldown,
        _ => 0
    };

    public void SetAllocated(PrimaryStat stat, int value)
    {
        switch (stat)
        {
            case PrimaryStat.Agility:      agility = value; break;
            case PrimaryStat.AttackDamage: attackDamage = value; break;
            case PrimaryStat.Luck:         luck = value; break;
            case PrimaryStat.Psyche:       psyche = value; break;
            case PrimaryStat.Health:       health = value; break;
            case PrimaryStat.Size:         size = value; break;
            case PrimaryStat.Cooldown:     cooldown = value; break;
        }
    }

    public int TotalAllocated =>
        agility + attackDamage + luck + psyche + health + size + cooldown;

    /// <summary>1 + allocated points. Size/Cooldown start at 0 — they're only gained in-game.</summary>
    public int FinalStat(PrimaryStat stat) =>
        CharacterRoster.IsCreationStat(stat) ? CharacterRoster.BaseStatValue + Allocated(stat) : 0;

    public bool CanPlay => !(hardcore && dead);
}

/// <summary>Smith level of one weapon for one character.</summary>
[Serializable]
public class WeaponLevelEntry
{
    public string weaponId;
    public int level;
}
