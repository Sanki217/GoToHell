using UnityEngine;

/// <summary>
/// Carries the current run's setup choices from the Character Creator scene
/// into the gameplay scene(s). Survives scene loads as a DontDestroyOnLoad singleton.
///
/// Lifecycle:
///   1. Character Creator writes playerName, stat allocation, and selectedPactId here.
///   2. Gameplay scene reads these on load to configure the player.
///   3. Persists across all gameplay layer scenes for the whole run.
///   4. Reset() is called when starting a fresh run or returning to menu.
///
/// This holds ONLY the current run's setup — not save data. Persistent unlocks
/// (pacts, achievements, history) live in SaveManager.
/// </summary>
public class RunConfig : MonoBehaviour
{
    public static RunConfig I { get; private set; }

    [Header("Run Setup (set by Character Creator)")]
    public string playerName = "Sinner";

    // Starting allocation of the 10-point primary stat pool
    public int agility;
    public int attackDamage;
    public int luck;
    public int psyche;
    public int health;
    public int size;
    public int cooldown;

    // Empty string = no pact selected
    public string selectedPactId = "";

    [Header("Run Progress (set during gameplay)")]
    public int currentLayer = 1;
    public float runStartTime;

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

    /// <summary>Total points currently allocated across all stats.</summary>
    public int TotalAllocated =>
        agility + attackDamage + luck + psyche + health + size + cooldown;

    /// <summary>Wipe all run setup back to defaults. Call when starting fresh.</summary>
    public void Reset()
    {
        playerName     = "Sinner";
        agility        = 0;
        attackDamage   = 0;
        luck           = 0;
        psyche         = 0;
        health         = 0;
        size           = 0;
        cooldown       = 0;
        selectedPactId = "";
        currentLayer   = 1;
        runStartTime   = 0f;
    }

    /// <summary>Marks the moment gameplay begins, for run-duration tracking.</summary>
    public void MarkRunStart()
    {
        runStartTime = Time.time;
        currentLayer = 1;
    }

    /// <summary>Seconds elapsed since the run began.</summary>
    public float RunDuration => Time.time - runStartTime;
}
