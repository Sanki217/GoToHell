using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the level-up upgrade picker.
/// Rolls 3 UpgradeOrbOffers from the pool (each with a prefab + rarity + stat bonuses).
/// When the player picks one, spawns the orb prefab with the rolled data injected,
/// then the orb flies in and applies itself.
/// </summary>
public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [Header("References")]
    public GameObject levelUpPanel;
    public UpgradeCardUI[] cards;
    public PlayerUpgradePool upgradePool;

    [Header("Orb Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);
    public float spawnEjectForce = 3f;

    [Header("Player References — auto-found if not set")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;

    private bool isOpen = false;

    /// <summary>Used by CameraFollow to suppress shake while UI is open.</summary>
    public bool IsOpen => isOpen;

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

    public void Show(int currentLayer)
    {
        if (isOpen || upgradePool == null) return;

        float luck = playerStats != null ? playerStats.luck : 0f;
        List<UpgradeOrbOffer> offers = upgradePool.RollLevelUpOffers(
            currentLayer, luck, playerStats, 3, upgradeManager);

        if (offers.Count == 0) return;

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
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void OnCardPicked(UpgradeOrbOffer offer)
    {
        if (!isOpen) return;
        Close();
        SpawnOrb(offer);
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

        // Inject rolled data before the orb starts attracting
        orb.rolledStatBonuses = offer.statBonuses?.ToArray();
        orb.rolledRarity = offer.rarity;

        Vector3 ejectDir = spawnOffset.normalized == Vector3.zero ? Vector3.up : spawnOffset.normalized;
        orb.Initialize(ejectDir, spawnEjectForce);
    }

    private void Close()
    {
        isOpen = false;
        Time.timeScale = 1f;
        levelUpPanel.SetActive(false);
        playerState?.EnableControl();
    }
}