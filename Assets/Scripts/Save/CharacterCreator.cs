using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Character Creator flow. Stages: Class → Weapon → Pact → Name.
/// Each stage is a panel. A UnityEvent fires when each stage is entered,
/// so animations can be hooked in the Inspector.
///
/// Class and Weapon use CreatorCarousel (sprite carousel with side previews).
/// Class fully determines starting stats (no manual allocation).
/// Pact is optional. Name is last; Start begins the run.
///
/// On Start: writes name, class stats, classId, weaponId, and selected pact
/// into RunConfig, then loads Level 1 via SceneFlow.
/// </summary>
public class CharacterCreator : MonoBehaviour
{
    public enum Stage { Class, Weapon, Pact, Name }

    [Header("Stage Panels")]
    public GameObject classPanel;
    public GameObject weaponPanel;
    public GameObject pactPanel;
    public GameObject namePanel;

    [Header("Stage Enter Events (hook animations here)")]
    public UnityEvent onEnterClass;
    public UnityEvent onEnterWeapon;
    public UnityEvent onEnterPact;
    public UnityEvent onEnterName;

    [Header("Class Stage")]
    public CreatorCarousel classCarousel;
    public List<ClassDefinition> allClasses = new List<ClassDefinition>();
    public Button classNextButton;

    [Header("Weapon Stage")]
    public CreatorCarousel weaponCarousel;
    public List<WeaponDefinition> allWeapons = new List<WeaponDefinition>();
    public Button weaponNextButton;
    public Button weaponBackButton;

    [Header("Pact Stage")]
    public Transform pactListContainer;
    public GameObject pactOptionPrefab;
    public List<PactDefinition> allPacts = new List<PactDefinition>();
    public Button pactNextButton;
    public Button pactBackButton;

    [Header("Name Stage")]
    public TMP_InputField nameInput;
    public Button startButton;
    public Button nameBackButton;

    // ── state ───────────────────────────────────────────────────────
    private Stage currentStage;
    private PactOptionButton selectedPactButton;
    private readonly List<PactOptionButton> spawnedPactButtons = new List<PactOptionButton>();

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        EnsureRunConfig();

        if (classNextButton  != null) classNextButton.onClick.AddListener(() => GoToStage(Stage.Weapon));
        if (weaponNextButton != null) weaponNextButton.onClick.AddListener(() => GoToStage(Stage.Pact));
        if (weaponBackButton != null) weaponBackButton.onClick.AddListener(() => GoToStage(Stage.Class));
        if (pactNextButton   != null) pactNextButton.onClick.AddListener(() => GoToStage(Stage.Name));
        if (pactBackButton   != null) pactBackButton.onClick.AddListener(() => GoToStage(Stage.Weapon));
        if (nameBackButton   != null) nameBackButton.onClick.AddListener(() => GoToStage(Stage.Pact));
        if (startButton      != null) startButton.onClick.AddListener(BeginRun);

        BuildClassCarousel();
        BuildWeaponCarousel();
        BuildPactList();

        GoToStage(Stage.Class);
    }

    private void EnsureRunConfig()
    {
        // Safety net so the creator scene can be tested directly
        if (RunConfig.I == null)
        {
            GameObject go = new GameObject("RunConfig");
            go.AddComponent<RunConfig>();
        }
    }

    // ================================================================
    //  STAGE FLOW
    // ================================================================

    private void GoToStage(Stage stage)
    {
        currentStage = stage;

        if (classPanel  != null) classPanel.SetActive(stage == Stage.Class);
        if (weaponPanel != null) weaponPanel.SetActive(stage == Stage.Weapon);
        if (pactPanel   != null) pactPanel.SetActive(stage == Stage.Pact);
        if (namePanel   != null) namePanel.SetActive(stage == Stage.Name);

        switch (stage)
        {
            case Stage.Class:  onEnterClass?.Invoke();  break;
            case Stage.Weapon: onEnterWeapon?.Invoke(); break;
            case Stage.Pact:   onEnterPact?.Invoke();   break;
            case Stage.Name:   onEnterName?.Invoke();   break;
        }
    }

    // ================================================================
    //  CLASS CAROUSEL
    // ================================================================

    private void BuildClassCarousel()
    {
        if (classCarousel == null) return;

        var entries = new List<CreatorCarousel.Entry>();
        foreach (ClassDefinition def in allClasses)
        {
            if (def == null) continue;
            entries.Add(new CreatorCarousel.Entry
            {
                id          = def.classId,
                displayName = def.displayName,
                description = BuildClassDescription(def),
                sprite      = def.sprite,
                unlocked    = def.unlockedByDefault || SaveManager.IsClassUnlocked(def.classId)
            });
        }

        classCarousel.OnSelectionChanged += e =>
        {
            if (classNextButton != null)
                classNextButton.interactable = e != null && e.unlocked;
        };
        classCarousel.SetEntries(entries);
    }

    private static string BuildClassDescription(ClassDefinition d)
    {
        string desc = d.description;

        if (!string.IsNullOrEmpty(d.skillDisplayName))
            desc += $"\n\n<b>Skill (RMB):</b> {d.skillDisplayName}\n{d.skillDescription}";

        desc += "\n\n"
             + $"<color=#FFA500>AGI {d.agility}</color>  "
             + $"<color=#FF5555>ATK {d.attackDamage}</color>  "
             + $"<color=#ADFF2F>LCK {d.luck}</color>  "
             + $"<color=#FF66CC>PSY {d.psyche}</color>\n"
             + $"<color=#66FF66>HP {d.health}</color>  "
             + $"<color=#BBBBBB>SIZ {d.size}</color>  "
             + $"<color=#66FFFF>CDR {d.cooldown}</color>";

        return desc;
    }

    // ================================================================
    //  WEAPON CAROUSEL
    // ================================================================

    private void BuildWeaponCarousel()
    {
        if (weaponCarousel == null) return;

        var entries = new List<CreatorCarousel.Entry>();
        foreach (WeaponDefinition def in allWeapons)
        {
            if (def == null) continue;
            entries.Add(new CreatorCarousel.Entry
            {
                id          = def.weaponId,
                displayName = def.displayName,
                description = def.description,
                sprite      = def.sprite,
                unlocked    = def.unlockedByDefault || SaveManager.IsWeaponUnlocked(def.weaponId)
            });
        }

        weaponCarousel.OnSelectionChanged += e =>
        {
            if (weaponNextButton != null)
                weaponNextButton.interactable = e != null && e.unlocked;
        };
        weaponCarousel.SetEntries(entries);
    }

    // ================================================================
    //  PACT LIST
    // ================================================================

    private void BuildPactList()
    {
        if (pactListContainer == null || pactOptionPrefab == null) return;

        foreach (PactDefinition def in allPacts)
        {
            if (def == null) continue;
            if (!def.unlockedByDefault && !SaveManager.IsPactUnlocked(def.pactId)) continue;

            GameObject go = Instantiate(pactOptionPrefab, pactListContainer);
            PactOptionButton opt = go.GetComponent<PactOptionButton>();
            if (opt != null)
            {
                opt.Setup(def, this);
                spawnedPactButtons.Add(opt);
            }
        }
    }

    public void SelectPact(PactOptionButton option)
    {
        // Toggle off if re-clicking the selected one
        if (selectedPactButton == option)
        {
            selectedPactButton.SetSelected(false);
            selectedPactButton = null;
            return;
        }

        if (selectedPactButton != null) selectedPactButton.SetSelected(false);
        selectedPactButton = option;
        selectedPactButton.SetSelected(true);
    }

    // ================================================================
    //  BEGIN RUN
    // ================================================================

    private void BeginRun()
    {
        RunConfig cfg = RunConfig.I;
        if (cfg == null) return;

        CreatorCarousel.Entry classEntry  = classCarousel  != null ? classCarousel.Current  : null;
        CreatorCarousel.Entry weaponEntry = weaponCarousel != null ? weaponCarousel.Current : null;
        if (classEntry == null || !classEntry.unlocked) return;
        if (weaponEntry == null || !weaponEntry.unlocked) return;

        ClassDefinition classDef = allClasses.Find(c => c != null && c.classId == classEntry.id);
        if (classDef == null) return;

        cfg.playerName = string.IsNullOrWhiteSpace(nameInput?.text) ? "Sinner" : nameInput.text;

        cfg.selectedClassId  = classDef.classId;
        cfg.selectedWeaponId = weaponEntry.id;

        // Class preset fully determines the starting stat allocation
        cfg.agility      = classDef.agility;
        cfg.attackDamage = classDef.attackDamage;
        cfg.luck         = classDef.luck;
        cfg.psyche       = classDef.psyche;
        cfg.health       = classDef.health;
        cfg.size         = classDef.size;
        cfg.cooldown     = classDef.cooldown;

        cfg.selectedPactId = selectedPactButton != null ? selectedPactButton.Pact.pactId : "";

        SceneFlow.StartRunAtLevel1();
    }
}
