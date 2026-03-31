using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the level-up upgrade picker.
/// Call LevelUpUI.Show(layer, playerStats, upgradeManager) from wherever
/// you track XP/level progression.
///
/// Setup in Unity:
///   1. Create a UI Panel "LevelUpPanel" in your Canvas
///   2. Add 3 UpgradeCardUI children inside it
///   3. Add this script to a persistent GameObject (e.g. GameManager)
///   4. Wire levelUpPanel, cards[0/1/2], upgradePool in Inspector
///   5. Wire playerStateController so we can block input while open
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [Header("References")]
    public GameObject levelUpPanel;
    public UpgradeCardUI[] cards;            // assign 3 card GameObjects
    public PlayerUpgradePool upgradePool;

    [Header("Player References � auto-found if not set")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;

    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
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

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>Call this when the player levels up.</summary>
    public void Show(int currentLayer)
    {
        if (isOpen || upgradePool == null) return;

        float luck = playerStats != null ? playerStats.luck : 0f;
        List<UpgradeOffer> offers = upgradePool.RollLevelUpOffers(currentLayer, luck, playerStats, 3);

        if (offers.Count == 0) return;

        // Pause game
        Time.timeScale = 0f;
        isOpen = true;

        playerState?.DisableControl();

        // Setup cards
        for (int i = 0; i < cards.Length; i++)
        {
            if (i < offers.Count)
                cards[i].Setup(offers[i], OnCardPicked);
            else
                cards[i].Hide();
        }

        levelUpPanel.SetActive(true);
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void OnCardPicked(UpgradeCardUI card)
    {
        if (!isOpen) return;

        UpgradeOffer offer = card.offer;

        // Apply stat bonuses immediately
        if (playerStats != null)
        {
            foreach (var bonus in offer.statBonuses)
                bonus.Apply(playerStats);
        }

        // Apply the behaviour upgrade via upgradeManager
        // The upgradeId must match a PlayerUpgrade.Id registered in the pool
        if (upgradeManager != null && !offer.data.isPureStatUpgrade)
        {
            PlayerUpgrade upgrade = UpgradeFactory.Create(offer.data.upgradeId);
            if (upgrade != null)
                upgradeManager.ApplyUpgrade(upgrade);
            else
                Debug.LogWarning($"[LevelUpUI] No upgrade behaviour found for id '{offer.data.upgradeId}'. " +
                                 $"Register it in UpgradeFactory.");
        }

        Close();
    }

    private void Close()
    {
        isOpen = false;
        Time.timeScale = 1f;
        levelUpPanel.SetActive(false);
        playerState?.EnableControl();
    }
}