using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fullscreen chest reward screen.
/// Called by Chest.cs when the player opens a chest.
///
/// Flow:
///   1. Chest.Open() calls ChestRewardUI.Instance.Show(luck, playerStats)
///   2. Rarity is rolled using luck only (no layer influence)
///   3. Upgrade is rolled from pool at that rarity
///   4. Soul reward is rolled based on rarity (configured in Inspector)
///   5. Slot machine animation plays, then result is shown
///   6. Player clicks Collect — upgrade and souls are applied
/// </summary>
public class ChestRewardUI : MonoBehaviour
{
    public static ChestRewardUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject chestPanel;

    [Header("Rolling Display")]
    public TMP_Text rollingNameLabel;
    public Image rollingBackground;
    public float rollStartInterval = 0.05f;
    public float rollEndInterval = 0.35f;
    public float rollDuration = 2.5f;

    [Header("Result Display")]
    public GameObject resultCard;
    public TMP_Text resultNameLabel;
    public TMP_Text resultRarityLabel;
    public TMP_Text resultDescriptionLabel;
    public TMP_Text resultSoulsLabel;       // shows "+ 24 Souls"
    public Image resultIcon;
    public Image resultCardBackground;
    public Transform resultStatContainer;
    public GameObject statLinePrefab;
    public Button collectButton;

    [Header("Soul Rewards per Rarity")]
    [Tooltip("Souls granted to the player on collect, based on rolled rarity.")]
    public int soulsCommonMin = 5;
    public int soulsCommonMax = 15;
    public int soulsRareMin = 15;
    public int soulsRareMax = 30;
    public int soulsEpicMin = 30;
    public int soulsEpicMax = 60;
    public int soulsLegendaryMin = 60;
    public int soulsLegendaryMax = 120;

    [Header("References")]
    public PlayerUpgradePool upgradePool;

    [Header("Player References — auto-found if not assigned")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;
    public PlayerInventory playerInventory;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private bool isOpen = false;
    private UpgradeOffer pendingOffer;
    private int pendingSouls;

    // ================================================================
    //  INIT
    // ================================================================

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
                playerInventory = player.GetComponent<PlayerInventory>();
            }
        }

        if (collectButton != null)
            collectButton.onClick.AddListener(OnCollectClicked);
    }

    // ================================================================
    //  PUBLIC API — called by Chest.cs
    // ================================================================

    /// <summary>
    /// Open the chest reward screen.
    /// Rarity is determined by luck only — no layer influence.
    /// </summary>
    public void Show(float luck, PlayerStats statsOverride = null)
    {
        if (isOpen || upgradePool == null || upgradePool.upgrades.Count == 0) return;

        PlayerStats effectiveStats = statsOverride ?? playerStats;
        float effectiveLuck = effectiveStats != null ? effectiveStats.luck : luck;

        // Roll rarity using luck only
        UpgradeRarity rarity = UpgradeRarityRoller.RollWithLuckOnly(effectiveLuck);

        // Roll upgrade at that rarity
        if (upgradePool.upgrades.Count == 0) return;
        var data = upgradePool.upgrades[Random.Range(0, upgradePool.upgrades.Count)];
        var offer = upgradePool.BuildOffer(data, rarity, effectiveStats);
        if (offer == null) return;

        // Roll souls for this rarity
        pendingSouls = RollSouls(rarity);
        pendingOffer = offer;

        // Open UI
        isOpen = true;
        Time.timeScale = 0f;
        playerState?.DisableControl();

        chestPanel.SetActive(true);
        if (resultCard != null) resultCard.SetActive(false);
        if (collectButton != null) collectButton.gameObject.SetActive(false);

        // Show rolling display
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(true);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(true);

        StartCoroutine(RollRoutine(offer));
    }

    // ================================================================
    //  PRIVATE — Roll animation
    // ================================================================

    private IEnumerator RollRoutine(UpgradeOffer finalOffer)
    {
        List<PlayerUpgradeData> all = upgradePool.upgrades;
        float elapsed = 0f;
        float nextChange = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / rollDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float interval = Mathf.Lerp(rollStartInterval, rollEndInterval, eased);

            if (elapsed >= nextChange)
            {
                nextChange = elapsed + interval;

                PlayerUpgradeData rand = all[Random.Range(0, all.Count)];
                if (rollingNameLabel != null)
                    rollingNameLabel.text = rand.displayName;

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

        ShowResult(finalOffer);
    }

    private void ShowResult(UpgradeOffer offer)
    {
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(false);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(false);

        if (resultCard != null) resultCard.SetActive(true);

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        if (resultCardBackground != null)
        {
            Color bg = rarityColor; bg.a = 0.4f;
            resultCardBackground.color = bg;
        }

        if (resultRarityLabel != null)
        {
            resultRarityLabel.text = UpgradeRarityRoller.GetRarityName(offer.rarity).ToUpper();
            resultRarityLabel.color = rarityColor;
        }

        if (resultNameLabel != null)
            resultNameLabel.text = offer.data.displayName;

        if (resultDescriptionLabel != null)
            resultDescriptionLabel.text = offer.data.GetDescription(offer.rarity);

        if (resultIcon != null)
        {
            resultIcon.sprite = offer.data.icon;
            resultIcon.enabled = offer.data.icon != null;
        }

        // Souls label
        if (resultSoulsLabel != null)
            resultSoulsLabel.text = $"+ {pendingSouls} Souls";

        // Stat bonus lines
        if (resultStatContainer != null)
        {
            foreach (Transform child in resultStatContainer) Destroy(child.gameObject);
            foreach (var bonus in offer.statBonuses)
            {
                if (statLinePrefab == null) break;
                GameObject line = Instantiate(statLinePrefab, resultStatContainer);
                TMP_Text txt = line.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.text = bonus.GetDescription();
                    txt.color = bonus.value >= 0f ? Color.green : Color.red;
                }
            }
        }

        if (collectButton != null) collectButton.gameObject.SetActive(true);
    }

    // ================================================================
    //  COLLECT
    // ================================================================

    private void OnCollectClicked()
    {
        if (pendingOffer == null) return;

        // Grant souls
        var inv = playerInventory;
        if (inv == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) inv = p.GetComponent<PlayerInventory>();
        }
        inv?.AddSouls(pendingSouls);

        // Apply stat bonuses
        PlayerStats s = playerStats;
        if (s != null)
            foreach (var bonus in pendingOffer.statBonuses)
                bonus.Apply(s);

        // Apply behaviour upgrade
        if (upgradeManager != null)
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
        pendingSouls = 0;
        Time.timeScale = 1f;
        chestPanel.SetActive(false);

        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(true);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(true);

        playerState?.EnableControl();
    }

    // ================================================================
    //  SOUL ROLL
    // ================================================================

    private int RollSouls(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => Random.Range(soulsCommonMin, soulsCommonMax + 1),
        UpgradeRarity.Rare => Random.Range(soulsRareMin, soulsRareMax + 1),
        UpgradeRarity.Epic => Random.Range(soulsEpicMin, soulsEpicMax + 1),
        UpgradeRarity.Legendary => Random.Range(soulsLegendaryMin, soulsLegendaryMax + 1),
        _ => Random.Range(soulsCommonMin, soulsCommonMax + 1)
    };
}