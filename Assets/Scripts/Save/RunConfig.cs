using UnityEngine;

/// <summary>
/// Carries the current run's setup choices from the Character Creator scene
/// into the gameplay scene(s). Survives scene loads as a DontDestroyOnLoad singleton.
///
/// Lifecycle:
///   1. Character Creator writes playerName, class/weapon/pact ids, and the
///      class's stat preset here.
///   2. Gameplay scene reads these on load to configure the player.
///   3. Persists across all gameplay layer scenes for the whole run.
///   4. Reset() is called when starting a fresh run or returning to menu.
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

    /// <summary>
    /// Sets the run up from a saved character + the pact chosen for this run:
    /// name, class (weapon), final stats (class base + allocated points), RMB skill.
    /// </summary>
    public void SetupRun(CharacterData c, PactDefinition pact)
    {
        Reset();
        if (c == null) return;

        ClassDefinition cls = Content.Classes.Get(c.classId);
        characterId      = c.id;
        playerName       = c.name;
        hardcore         = c.hardcore;
        selectedClassId  = c.classId;
        selectedWeaponId = cls != null && cls.weapon != null ? cls.weapon.weaponId : "";
        selectedPactId   = pact != null ? pact.pactId : "";

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
