using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fullscreen slot machine for chest rewards.
/// The rarity is pre-determined when the chest opens.
/// The display rolls through random upgrade names rapidly, decelerates, then lands on the result.
///
/// Setup in Unity:
///   1. Create a fullscreen UI Panel "ChestRewardPanel"
///   2. Inside it: a central card area with nameLabel, rarityLabel, descriptionLabel, iconImage
///   3. A "Collect" button shown after the roll stops
///   4. Assign upgradePool and player references
///
/// Call ChestRewardUI.Show(layer, playerStats, upgradeManager) when a chest is opened.
/// </summary>
public class ChestRewardUI : MonoBehaviour
{
    public static ChestRewardUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject chestPanel;

    [Header("Rolling Display")]
    public TMP_Text rollingNameLabel;    // shows rapidly changing names during roll
    public Image rollingBackground;   // background tinted by current displayed rarity
    public float rollStartInterval = 0.05f;  // time between name changes at start (fast)
    public float rollEndInterval = 0.35f;  // time between changes at end (slow)
    public float rollDuration = 2.5f;   // total roll duration before stopping

    [Header("Result Display (shown after roll stops)")]
    public GameObject resultCard;
    public TMP_Text resultNameLabel;
    public TMP_Text resultRarityLabel;
    public TMP_Text resultDescriptionLabel;
    public Image resultIcon;
    public Image resultCardBackground;
    public Transform resultStatContainer;
    public GameObject statLinePrefab;
    public Button collectButton;

    [Header("References")]
    public PlayerUpgradePool upgradePool;

    [Header("Player References — auto-found if not set")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;

    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;
        if (chestPanel != null) chestPanel.SetActive(false);
        if (resultCard != null) resultCard.SetActive(false);
        if (collectButton != null) collectButton.gameObject.SetActive(false);
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

        if (collectButton != null)
            collectButton.onClick.AddListener(OnCollectClicked);
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>Open the chest reward screen and start the roll.</summary>
    public void Show(int currentLayer)
    {
        if (isOpen || upgradePool == null || upgradePool.upgrades.Count == 0) return;

        float luck = playerStats != null ? playerStats.luck : 0f;
        UpgradeOffer offer = upgradePool.RollChestOffer(currentLayer, luck, playerStats);
        if (offer == null) return;

        isOpen = true;
        Time.timeScale = 0f;
        playerState?.DisableControl();

        chestPanel.SetActive(true);
        if (resultCard != null) resultCard.SetActive(false);
        if (collectButton != null) collectButton.gameObject.SetActive(false);

        StartCoroutine(RollRoutine(offer));
    }

    // ================================================================
    //  PRIVATE — Roll coroutine
    // ================================================================

    private IEnumerator RollRoutine(UpgradeOffer finalOffer)
    {
        List<PlayerUpgradeData> allUpgrades = upgradePool.upgrades;
        float elapsed = 0f;
        float interval = rollStartInterval;

        float nextChange = 0f;

        // Roll loop — runs in unscaled time because game is paused
        while (elapsed < rollDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Lerp interval from fast → slow using ease-out curve
            float t = elapsed / rollDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f); // cubic ease-out
            interval = Mathf.Lerp(rollStartInterval, rollEndInterval, eased);

            if (elapsed >= nextChange)
            {
                nextChange = elapsed + interval;

                // Show a random upgrade name during roll
                PlayerUpgradeData rand = allUpgrades[Random.Range(0, allUpgrades.Count)];
                if (rollingNameLabel != null)
                    rollingNameLabel.text = rand.displayName;

                // Tint background with a random rarity color for visual noise
                if (rollingBackground != null)
                {
                    UpgradeRarity randRarity = (UpgradeRarity)Random.Range(0, 4);
                    Color c = UpgradeRarityRoller.GetRarityColor(randRarity);
                    c.a = 0.4f;
                    rollingBackground.color = c;
                }
            }

            yield return null;
        }

        // Roll finished — show result
        ShowResult(finalOffer);
    }

    private void ShowResult(UpgradeOffer offer)
    {
        // Hide rolling display
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(false);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(false);

        // Show result card
        if (resultCard != null) resultCard.SetActive(true);

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        if (resultCardBackground != null)
        {
            Color bg = rarityColor; bg.a = 0.4f;
            resultCardBackground.color = bg;
        }

        if (resultNameLabel != null)
            resultNameLabel.text = offer.data.displayName;

        if (resultRarityLabel != null)
        {
            resultRarityLabel.text = UpgradeRarityRoller.GetRarityName(offer.rarity).ToUpper();
            resultRarityLabel.color = rarityColor;
        }

        if (resultDescriptionLabel != null)
            resultDescriptionLabel.text = offer.data.GetDescription(offer.rarity);

        if (resultIcon != null)
        {
            resultIcon.sprite = offer.data.icon;
            resultIcon.enabled = offer.data.icon != null;
        }

        // Stat bonus lines
        if (resultStatContainer != null)
        {
            foreach (Transform child in resultStatContainer) Destroy(child.gameObject);
            foreach (var bonus in offer.statBonuses)
            {
                if (statLinePrefab == null) break;
                GameObject line = Instantiate(statLinePrefab, resultStatContainer);
                TMP_Text txt = line.GetComponent<TMP_Text>();
                if (txt != null) { txt.text = bonus.GetDescription(); txt.color = bonus.value >= 0 ? Color.green : Color.red; }
            }
        }

        if (collectButton != null) collectButton.gameObject.SetActive(true);

        // Store offer for collection
        pendingOffer = offer;
    }

    private UpgradeOffer pendingOffer;

    private void OnCollectClicked()
    {
        if (pendingOffer == null) return;

        // Apply stat bonuses
        if (playerStats != null)
            foreach (var bonus in pendingOffer.statBonuses)
                bonus.Apply(playerStats);

        // Apply behaviour upgrade
        if (upgradeManager != null && !pendingOffer.data.isPureStatUpgrade)
        {
            PlayerUpgrade upgrade = UpgradeFactory.Create(pendingOffer.data.upgradeId);
            if (upgrade != null) upgradeManager.ApplyUpgrade(upgrade);
        }

        Close();
    }

    private void Close()
    {
        isOpen = false;
        pendingOffer = null;
        Time.timeScale = 1f;
        chestPanel.SetActive(false);

        // Reset rolling display for next time
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(true);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(true);

        playerState?.EnableControl();
    }
}