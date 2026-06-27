using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Character Creator flow. Stages: Name → Stats → Pact → Ready.
/// Each stage is a panel. A UnityEvent fires when each stage is entered,
/// so animations can be hooked in the Inspector.
///
/// On Start (the run begins): writes name, stat allocation, and selected pact
/// into RunConfig, then loads Level 1 via SceneFlow.
/// </summary>
public class CharacterCreator : MonoBehaviour
{
    public enum Stage { Name, Stats, Pact, Ready }

    [Header("Stage Panels")]
    public GameObject namePanel;
    public GameObject statsPanel;
    public GameObject pactPanel;

    [Header("Stage Enter Events (hook animations here)")]
    public UnityEvent onEnterName;
    public UnityEvent onEnterStats;
    public UnityEvent onEnterPact;
    public UnityEvent onEnterReady;

    [Header("Name Stage")]
    public TMP_InputField nameInput;
    public Button nameNextButton;

    [Header("Stats Stage")]
    public int totalStatPoints = 10;
    public TMP_Text pointsRemainingText;
    public StatAllocatorRow[] statRows;
    public Button statsNextButton;
    public Button statsBackButton;

    [Header("Pact Stage")]
    public Transform pactListContainer;
    public GameObject pactOptionPrefab;
    public List<PactDefinition> allPacts = new List<PactDefinition>();
    public Button pactBackButton;
    public Button startButton;

    // ── state ───────────────────────────────────────────────────────
    private Stage currentStage;
    private int pointsRemaining;
    private PactOptionButton selectedPactButton;
    private readonly List<PactOptionButton> spawnedPactButtons = new List<PactOptionButton>();

    public int PointsRemaining => pointsRemaining;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        EnsureRunConfig();

        pointsRemaining = totalStatPoints;

        if (statRows != null)
            foreach (var row in statRows)
                row?.Init(this);

        UpdatePointsText();

        if (nameNextButton  != null) nameNextButton.onClick.AddListener(() => GoToStage(Stage.Stats));
        if (statsNextButton != null) statsNextButton.onClick.AddListener(() => GoToStage(Stage.Pact));
        if (statsBackButton != null) statsBackButton.onClick.AddListener(() => GoToStage(Stage.Name));
        if (pactBackButton  != null) pactBackButton.onClick.AddListener(() => GoToStage(Stage.Stats));
        if (startButton     != null) startButton.onClick.AddListener(BeginRun);

        BuildPactList();
        GoToStage(Stage.Name);
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

        if (namePanel  != null) namePanel.SetActive(stage == Stage.Name);
        if (statsPanel != null) statsPanel.SetActive(stage == Stage.Stats);
        if (pactPanel  != null) pactPanel.SetActive(stage == Stage.Pact);

        switch (stage)
        {
            case Stage.Name:  onEnterName?.Invoke();  break;
            case Stage.Stats: onEnterStats?.Invoke(); break;
            case Stage.Pact:  onEnterPact?.Invoke();  break;
            case Stage.Ready: onEnterReady?.Invoke(); break;
        }
    }

    // ================================================================
    //  STAT POOL  (called by StatAllocatorRow)
    // ================================================================

    public bool TrySpendPoint()
    {
        if (pointsRemaining <= 0) return false;
        pointsRemaining--;
        UpdatePointsText();
        return true;
    }

    public void RefundPoint()
    {
        pointsRemaining++;
        UpdatePointsText();
    }

    private void UpdatePointsText()
    {
        if (pointsRemainingText != null)
            pointsRemainingText.text = $"Points: {pointsRemaining}";
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
            if (!SaveManager.IsPactUnlocked(def.pactId)) continue;

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

        cfg.playerName = string.IsNullOrWhiteSpace(nameInput?.text) ? "Sinner" : nameInput.text;

        // Apply stat allocation
        cfg.agility = cfg.attackDamage = cfg.luck = cfg.psyche = 0;
        cfg.health  = cfg.size = cfg.cooldown = 0;

        if (statRows != null)
        {
            foreach (var row in statRows)
            {
                if (row == null) continue;
                switch (row.stat)
                {
                    case PrimaryStat.Agility:      cfg.agility      = row.Value; break;
                    case PrimaryStat.AttackDamage: cfg.attackDamage = row.Value; break;
                    case PrimaryStat.Luck:         cfg.luck         = row.Value; break;
                    case PrimaryStat.Psyche:       cfg.psyche       = row.Value; break;
                    case PrimaryStat.Health:       cfg.health       = row.Value; break;
                    case PrimaryStat.Size:         cfg.size         = row.Value; break;
                    case PrimaryStat.Cooldown:     cfg.cooldown     = row.Value; break;
                }
            }
        }

        cfg.selectedPactId = selectedPactButton != null ? selectedPactButton.Pact.pactId : "";

        SceneFlow.StartRunAtLevel1();
    }
}