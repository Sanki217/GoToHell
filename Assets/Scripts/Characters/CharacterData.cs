using System;

/// <summary>
/// One saved character (a slot on the Character Select screen). Lives in
/// SaveData.characters. The class gives the weapon; every stat starts
/// at 1 and the allocated points are what the player added at creation.
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

    // Progression (level design comes later — fields reserved so saves don't change shape)
    public int level = 1;
    public int xp;

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
