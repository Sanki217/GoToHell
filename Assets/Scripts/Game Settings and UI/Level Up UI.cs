using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Level-up UI — deferred Q-press system.
///
/// KEY BEHAVIOURS:
///   - Offers are rolled and CACHED when the picker first opens.
///     Pressing Q to close and reopen shows the SAME offers, same rarity.
///   - Stat bonuses are applied IMMEDIATELY when a card is picked (before the orb flies),
///     so any chained picker always shows fully updated stats.
///   - Pending level-ups stack; chained pickers open automatically after each pick.
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [Header("Card Picker")]
    public GameObject levelUpPanel;
    public UpgradeCardUI[] cards;
    public PlayerUpgradePool upgradePool;

    [Header("HUD Prompt")]
    [Tooltip("TMP_Text shown in the HUD while the player has unclaimed level-ups.")]
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

    // Cached offers survive Q-close so the same roll is shown next time Q is pressed.
    // Cleared only when a card is actually picked.
    private List<UpgradeOrbOffer> cachedOffers = null;

    // IDs of upgrades picked but whose orbs haven't landed yet.
    // Excluded from future rolls in the same chained session so duplicates never appear.
    private readonly HashSet<string> pendingPickedIds = new HashSet<string>();

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
            // Save and close — cached offers are kept, same roll next time.
            // Also clear pending IDs: the player is breaking the chain intentionally,
            // so future independent Q-presses should roll fresh without the exclusions.
            pendingLevelUps++;
            pendingPickedIds.Clear();
            Close(restoreControl: true);
            UpdatePrompt();
            return;
        }

        if (pendingLevelUps > 0)
            OpenPicker();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void QueueLevelUp(int currentLayer)
    {
        storedLayer = currentLayer;
        pendingLevelUps++;
        UpdatePrompt();
    }

    public void Show(int currentLayer) => QueueLevelUp(currentLayer);

    // ================================================================
    //  INTERNAL
    // ================================================================

    private void OpenPicker()
    {
        if (upgradePool == null || pendingLevelUps <= 0) return;

        pendingLevelUps--;

        // Reuse cached offers if they exist (player closed without picking)
        if (cachedOffers == null || cachedOffers.Count == 0)
        {
            float luck = playerStats != null ? playerStats.luck : 0f;
            cachedOffers = upgradePool.RollLevelUpOffers(
                storedLayer, luck, playerStats, 3, upgradeManager, pendingPickedIds);
        }

        if (cachedOffers == null || cachedOffers.Count == 0)
        {
            pendingLevelUps++;
            UpdatePrompt();
            return;
        }

        Time.timeScale = 0f;
        isOpen = true;
        playerState?.DisableControl();

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < cachedOffers.Count)
                cards[i].Setup(cachedOffers[i], OnCardPicked);
            else
                cards[i].Hide();
        }

        levelUpPanel.SetActive(true);
        UpdatePrompt();
    }

    private void OnCardPicked(UpgradeOrbOffer offer)
    {
        if (!isOpen) return;

        // Apply stat bonuses NOW — before spawning the orb or opening the next picker.
        // This guarantees the chained picker's descriptions show the post-pick stats.
        if (playerStats != null && offer.statBonuses != null)
            foreach (var bonus in offer.statBonuses)
                bonus.Apply(playerStats);

        // Track this upgrade as picked-but-not-yet-landed so the next roll excludes it
        if (offer.upgrade != null)
            pendingPickedIds.Add(offer.upgrade.UpgradeId);

        // Discard this pick's cache — next picker must roll fresh.
        cachedOffers = null;

        Close(restoreControl: false);

        // Spawn orb for behaviour wiring (OnAdded), but tell it stats are already applied.
        SpawnOrb(offer);

        if (pendingLevelUps > 0)
            OpenPicker();
        else
        {
            // No more picks queued — clear the pending set so it doesn't
            // linger across future level-up sessions
            pendingPickedIds.Clear();
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
        orb.statBonusesAlreadyApplied = true;  // LevelUpUI already applied them

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