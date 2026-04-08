using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Level-up UI — deferred Q-press system.
///
/// HOW IT WORKS:
///   - When the player levels up, a pending count increments and a HUD prompt appears.
///   - The game is NOT paused — player presses Q when ready.
///   - Q opens the card picker. While the picker is open, Q again closes it (saving for later).
///   - After picking a card, if more level-ups are pending, the next picker opens automatically.
///
/// SETUP:
///   - levelUpPanel: the card picker panel
///   - cards: 3 UpgradeCardUI components
///   - upgradePool: PlayerUpgradePool ScriptableObject
///   - pendingPromptLabel: HUD TMP_Text showing "Press Q — ×2 level-ups" (can be null)
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [Header("Card Picker")]
    public GameObject levelUpPanel;
    public UpgradeCardUI[] cards;
    public PlayerUpgradePool upgradePool;

    [Header("HUD Prompt")]
    [Tooltip("TMP_Text in the HUD shown while the player has unclaimed level-ups.")]
    public TMP_Text pendingPromptLabel;

    [Header("Orb Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);
    public float spawnEjectForce = 3f;

    [Header("Player References — auto-found if not set")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;

    // ================================================================
    //  STATE
    // ================================================================

    private bool isOpen = false;
    private int pendingLevelUps = 0;
    private int storedLayer = 1;

    /// <summary>CameraFollow queries this to suppress shake while UI is open.</summary>
    public bool IsOpen => isOpen;

    // ================================================================
    //  INIT
    // ================================================================

    private void Awake()
    {
        Instance = this;
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (pendingPromptLabel != null) pendingPromptLabel.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (playerStats == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
                upgradeManager = player.GetComponent<PlayerUpgradeManager>();
                playerState = player.GetComponent<PlayerStateController>();
            }
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;

        if (isOpen)
        {
            // Close and bank the pending slot back — player saves pick for later
            pendingLevelUps++;
            Close(restoreControl: true);
            UpdatePrompt();
            return;
        }

        if (pendingLevelUps > 0)
            OpenPicker();
    }

    // ================================================================
    //  PUBLIC API — called by PlayerLevelSystem on level-up
    // ================================================================

    /// <summary>
    /// Queue one level-up pick. Never opens the UI directly — player presses Q.
    /// Replaces the old Show(layer) call.
    /// </summary>
    public void QueueLevelUp(int currentLayer)
    {
        storedLayer = currentLayer;
        pendingLevelUps++;
        UpdatePrompt();
    }

    // Keep old Show() working for anything still calling it externally
    public void Show(int currentLayer) => QueueLevelUp(currentLayer);

    // ================================================================
    //  INTERNAL FLOW
    // ================================================================

    private void OpenPicker()
    {
        if (upgradePool == null || pendingLevelUps <= 0) return;

        pendingLevelUps--;

        float luck = playerStats != null ? playerStats.luck : 0f;
        List<UpgradeOrbOffer> offers = upgradePool.RollLevelUpOffers(
            storedLayer, luck, playerStats, 3, upgradeManager);

        if (offers.Count == 0)
        {
            pendingLevelUps++;  // refund
            UpdatePrompt();
            return;
        }

        Time.timeScale = 0f;
        isOpen = true;
        playerState?.DisableControl();

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < offers.Count)
                cards[i].Setup(offers[i], OnCardPicked);
            else
                cards[i].Hide();
        }

        levelUpPanel.SetActive(true);
        UpdatePrompt();
    }

    private void OnCardPicked(UpgradeOrbOffer offer)
    {
        if (!isOpen) return;

        Close(restoreControl: false);
        SpawnOrb(offer);

        if (pendingLevelUps > 0)
        {
            // Chain into the next pick immediately
            OpenPicker();
        }
        else
        {
            Time.timeScale = 1f;
            playerState?.EnableControl();
        }

        UpdatePrompt();
    }

    private void Close(bool restoreControl)
    {
        isOpen = false;
        levelUpPanel.SetActive(false);

        if (restoreControl)
        {
            Time.timeScale = 1f;
            playerState?.EnableControl();
        }
    }

    private void SpawnOrb(UpgradeOrbOffer offer)
    {
        Transform playerTransform = upgradeManager != null
            ? upgradeManager.transform
            : GameObject.FindWithTag("Player")?.transform;

        if (playerTransform == null) return;

        Vector3 spawnPos = playerTransform.position + spawnOffset;
        GameObject obj = Instantiate(offer.prefab, spawnPos, Quaternion.identity);
        UpgradeOrb orb = obj.GetComponent<UpgradeOrb>();
        if (orb == null) return;

        orb.rolledStatBonuses = offer.statBonuses?.ToArray();
        orb.rolledRarity = offer.rarity;

        Vector3 ejectDir = spawnOffset.normalized == Vector3.zero ? Vector3.up : spawnOffset.normalized;
        orb.Initialize(ejectDir, spawnEjectForce);
    }

    private void UpdatePrompt()
    {
        if (pendingPromptLabel == null) return;

        if (!isOpen && pendingLevelUps > 0)
        {
            pendingPromptLabel.gameObject.SetActive(true);
            pendingPromptLabel.text = pendingLevelUps == 1
                ? "Press <color=#FFD700>Q</color> to level up"
                : $"Press <color=#FFD700>Q</color> to level up  ×{pendingLevelUps}";
        }
        else
        {
            pendingPromptLabel.gameObject.SetActive(false);
        }
    }
}