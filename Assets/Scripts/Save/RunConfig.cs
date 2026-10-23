using UnityEngine;

/// <summary>
/// Carries the active character's loadout between scenes (town → run layers →
/// town). Survives scene loads as a DontDestroyOnLoad singleton.
///
/// Lifecycle:
///   1. Main menu → SetupTown(character): name, class, equipped weapon + its
///      smith level, final stats. inTown = true.
///   2. Town → pact chosen → SetupRun(character, pact): same, plus the pact.
///   3. Gameplay scenes read this on load to configure the player
///      (RunConfigApplier) — every layer of the run.
///   4. Reset() when returning to the main menu.
///
/// This holds ONLY the current run's setup — not save data. Persistent unlocks
/// (classes, weapons, pacts, achievements, history) live in SaveManager.
/// </summary>
public class RunConfig : MonoBehaviour
{
    public static RunConfig I { get; private set; }

    [Header("Run Setup (set by Character Creator)")]
    public string playerName = "Sinner";

    // Empty string = nothing selected (e.g. gameplay scene tested directly)
    public string selectedClassId  = "";
    public string selectedWeaponId = "";
    public string selectedPactId   = "";   // RMB skill (PactDefinition.pactId)
    public string characterId      = "";   // CharacterData.id ("" = testing a scene directly)
    public bool   hardcore;
    public int    weaponLevel;             // smith level of the equipped weapon
    [Tooltip("True in the town hub: no pact, no meta run bonuses.")]
    public bool   inTown;

    // Starting primary stats — copied from the selected class's preset
    public int agility;
    public int attackDamage;
    public int luck;
    public int psyche;
    public int health;
    public int size;
    public int cooldown;

    [Header("Run Progress (set during gameplay)")]
    public int currentLayer = 1;
    public float runStartTime;

    // Depth accumulated from COMPLETED layers (banked by LevelExit on layer
    // advance). The current layer's live depth lives in DepthTracker.
    public float bankedDepth;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => I = null;

    /// <summary>Total points currently allocated across all stats.</summary>
    public int TotalAllocated =>
        agility + attackDamage + luck + psyche + health + size + cooldown;

    /// <summary>Wipe all run setup back to defaults. Call when starting fresh.</summary>
    public void Reset()
    {
        playerName       = "Sinner";
        selectedClassId  = "";
        selectedWeaponId = "";
        selectedPactId   = "";
        characterId      = "";
        hardcore         = false;
        weaponLevel      = 0;
        inTown           = false;
        agility          = 0;
        attackDamage     = 0;
        luck             = 0;
        psyche           = 0;
        health           = 0;
        size             = 0;
        cooldown         = 0;
        currentLayer     = 1;
        runStartTime     = 0f;
        bankedDepth      = 0f;
        RunModifiers.MarkDirty();
    }

    /// <summary>The character walks around the town (no pact, no run bonuses).</summary>
    public void SetupTown(CharacterData c)
    {
        LoadCharacter(c);
        inTown = true;
    }

    /// <summary>
    /// Sets the run up from a saved character + the pact chosen for this run:
    /// name, class, equipped weapon (+ level), final stats, RMB skill.
    /// </summary>
    public void SetupRun(CharacterData c, PactDefinition pact)
    {
        LoadCharacter(c);
        inTown = false;
        selectedPactId = pact != null ? pact.pactId : "";
    }

    private void LoadCharacter(CharacterData c)
    {
        Reset();
        if (c == null) return;

        WeaponDefinition weapon = CharacterRoster.EquippedWeapon(c);
        characterId      = c.id;
        playerName       = c.name;
        hardcore         = c.hardcore;
        selectedClassId  = c.classId;
        selectedWeaponId = weapon != null ? weapon.weaponId : "";
        weaponLevel      = weapon != null ? c.WeaponLevel(weapon.weaponId) : 0;

        agility      = c.FinalStat(PrimaryStat.Agility);
        attackDamage = c.FinalStat(PrimaryStat.AttackDamage);
        luck         = c.FinalStat(PrimaryStat.Luck);
        psyche       = c.FinalStat(PrimaryStat.Psyche);
        health       = c.FinalStat(PrimaryStat.Health);
        size         = c.FinalStat(PrimaryStat.Size);
        cooldown     = c.FinalStat(PrimaryStat.Cooldown);
    }

    /// <summary>Marks the moment gameplay begins, for run-duration tracking.</summary>
    public void MarkRunStart()
    {
        runStartTime = Time.time;
        currentLayer = 1;
        bankedDepth  = 0f;
    }

    /// <summary>Seconds elapsed since the run began.</summary>
    public float RunDuration => Time.time - runStartTime;
}
