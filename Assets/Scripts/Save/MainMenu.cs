using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Put on a manager object in the Main Menu scene.
/// Wire the 4 buttons in the Inspector.
///
/// Play     → starts a run at Level 1 with the default loadout. There is no
///            character creation anymore — the name prompt lives in the
///            Level 1 scene (see NamePromptUI).
/// Settings → toggles the settings panel (kept as-is for now)
/// Collection → Collection scene
/// Exit     → quits the game
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button collectionButton;
    public Button exitButton;

    [Header("Settings Panel (optional)")]
    public GameObject settingsPanel;
    public Button settingsBackButton;

    [Header("Default Loadout")]
    [Tooltip("Class whose prefab and stat preset every run uses now that there is no class selection.")]
    public string defaultClassId = "rogue";
    [Tooltip("Written to RunConfig so weapon-gated upgrades (e.g. bow upgrades) stay available. " +
             "All weapon components are enabled regardless — see RunConfigApplier.")]
    public string defaultWeaponId = "bow";

    private void Start()
    {
        // Starting a fresh run from the menu wipes any leftover run config
        if (RunConfig.I != null) RunConfig.I.Reset();

        if (playButton != null)       playButton.onClick.AddListener(OnPlay);
        if (settingsButton != null)   settingsButton.onClick.AddListener(OnSettings);
        if (collectionButton != null) collectionButton.onClick.AddListener(OnCollection);
        if (exitButton != null)       exitButton.onClick.AddListener(OnExit);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnPlay()
    {
        EnsureRunConfig();
        RunConfig.I.Reset();
        ApplyDefaultLoadout(RunConfig.I);
        SceneFlow.StartRunAtLevel1();
    }

    private void OnCollection() => SceneFlow.GoToCollection();
    private void OnExit()       => SceneFlow.QuitGame();

    private void OnSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ================================================================
    //  RUN SETUP
    // ================================================================

    private void EnsureRunConfig()
    {
        if (RunConfig.I == null)
        {
            GameObject go = new GameObject("RunConfig");
            go.AddComponent<RunConfig>();
        }
    }

    /// <summary>
    /// Every run uses the same loadout: the default class's prefab + stat
    /// preset (spawned via PlayerSpawner) and the full weapon kit.
    /// </summary>
    private void ApplyDefaultLoadout(RunConfig cfg)
    {
        cfg.selectedWeaponId = defaultWeaponId;

        ClassDefinition def = null;
        foreach (ClassDefinition c in Resources.LoadAll<ClassDefinition>("Classes"))
            if (c != null && c.classId == defaultClassId) { def = c; break; }

        if (def == null)
        {
            Debug.LogWarning($"[MainMenu] No ClassDefinition '{defaultClassId}' in Resources/Classes — prefab defaults will apply.");
            return;
        }

        cfg.selectedClassId = def.classId;
        cfg.agility      = def.agility;
        cfg.attackDamage = def.attackDamage;
        cfg.luck         = def.luck;
        cfg.psyche       = def.psyche;
        cfg.health       = def.health;
        cfg.size         = def.size;
        cfg.cooldown     = def.cooldown;
    }
}
