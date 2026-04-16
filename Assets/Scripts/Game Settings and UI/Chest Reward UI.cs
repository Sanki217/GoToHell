using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fullscreen chest reward screen.
/// Rolls one UpgradeOrbOffer (prefab + rarity + stat bonuses) from the pool.
/// On collect: grants souls immediately, spawns the orb which flies in and applies itself.
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
    public TMP_Text resultSoulsLabel;
    public Image resultIcon;
    public Image resultCardBackground;
    public Transform resultStatContainer;
    public GameObject statLinePrefab;
    public Button collectButton;

    [Header("Soul Rewards per Rarity")]
    public int soulsCommonMin = 5; public int soulsCommonMax = 15;
    public int soulsRareMin = 15; public int soulsRareMax = 30;
    public int soulsEpicMin = 30; public int soulsEpicMax = 60;
    public int soulsLegendaryMin = 60; public int soulsLegendaryMax = 120;

    [Header("References")]
    public PlayerUpgradePool upgradePool;

    [Header("Orb Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);
    public float spawnEjectForce = 3f;

    [Header("Player References — auto-found if not assigned")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;
    public PlayerInventory playerInventory;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private bool isOpen = false;
    private UpgradeOrbOffer pendingOffer;
    private int pendingSouls;

    /// <summary>Used by CameraFollow to suppress shake while UI is open.</summary>
    public bool IsOpen => isOpen;

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
            var refs = PlayerRefs.I;
            if (refs != null)
            {
                playerStats = refs.Stats;
                upgradeManager = refs.Upgrades;
                playerState = refs.StateCtrl;
                playerInventory = refs.Inventory;
            }
        }

        if (collectButton != null)
            collectButton.onClick.AddListener(OnCollectClicked);
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Show(float luck, PlayerStats statsOverride = null)
    {
        if (isOpen || upgradePool == null || upgradePool.upgradePrefabs.Count == 0) return;

        PlayerStats effectiveStats = statsOverride ?? playerStats;
        float effectiveLuck = effectiveStats != null ? effectiveStats.luck : luck;

        pendingOffer = upgradePool.RollChestOffer(effectiveLuck, effectiveStats, upgradeManager);
        if (pendingOffer == null) return;

        pendingSouls = RollSouls(pendingOffer.rarity);

        isOpen = true;
        Time.timeScale = 0f;
        playerState?.DisableControl();

        chestPanel.SetActive(true);
        if (resultCard != null) resultCard.SetActive(false);
        if (collectButton != null) collectButton.gameObject.SetActive(false);
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(true);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(true);

        StartCoroutine(RollRoutine());
    }

    // ================================================================
    //  ROLLING ANIMATION
    // ================================================================

    private IEnumerator RollRoutine()
    {
        List<string> allNames = upgradePool.GetAllDisplayNames();
        float elapsed = 0f, nextChange = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / rollDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float interval = Mathf.Lerp(rollStartInterval, rollEndInterval, eased);

            if (elapsed >= nextChange)
            {
                nextChange = elapsed + interval;
                if (rollingNameLabel != null && allNames.Count > 0)
                    rollingNameLabel.text = allNames[Random.Range(0, allNames.Count)];

                if (rollingBackground != null)
                {
                    UpgradeRarity randRarity = (UpgradeRarity)Random.Range(0, 4);
                    Color c = UpgradeRarityRoller.GetRarityColor(randRarity); c.a = 0.4f;
                    rollingBackground.color = c;
                }
            }
            yield return null;
        }

        ShowResult();
    }

    private void ShowResult()
    {
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(false);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(false);
        if (resultCard != null) resultCard.SetActive(true);

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(pendingOffer.rarity);

        if (resultCardBackground != null)
        {
            Color bg = rarityColor; bg.a = 0.4f;
            resultCardBackground.color = bg;
        }

        if (resultRarityLabel != null)
        {
            resultRarityLabel.text = UpgradeRarityRoller.GetRarityName(pendingOffer.rarity).ToUpper();
            resultRarityLabel.color = rarityColor;
        }

        PlayerUpgrade upgrade = pendingOffer.upgrade;
        if (upgrade != null)
        {
            if (resultNameLabel != null) resultNameLabel.text = upgrade.displayName;
            if (resultDescriptionLabel != null) resultDescriptionLabel.text = upgrade.description;
            if (resultIcon != null)
            {
                resultIcon.sprite = upgrade.icon;
                resultIcon.enabled = upgrade.icon != null;
            }
        }

        if (resultSoulsLabel != null)
            resultSoulsLabel.text = $"+ {pendingSouls} Souls";

        // Stat bonus lines — show current → after
        if (resultStatContainer != null && pendingOffer.statBonuses != null)
        {
            foreach (Transform child in resultStatContainer) Destroy(child.gameObject);
            foreach (var bonus in pendingOffer.statBonuses)
            {
                if (statLinePrefab == null) break;
                GameObject line = Instantiate(statLinePrefab, resultStatContainer);
                TMP_Text txt = line.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.text = playerStats != null
                        ? bonus.GetPreviewLine(playerStats)
                        : bonus.GetDescription();
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

        var inv = playerInventory;
        if (inv == null)
        {
            inv = PlayerRefs.I?.Inventory;
        }
        inv?.AddSouls(pendingSouls);

        UpgradeOrbOffer offerToSpawn = pendingOffer;
        Close();
        SpawnOrb(offerToSpawn);
    }

    private void SpawnOrb(UpgradeOrbOffer offer)
    {
        Transform playerTransform = upgradeManager != null
            ? upgradeManager.transform
            : PlayerRefs.I?.T;

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

    private int RollSouls(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => Random.Range(soulsCommonMin, soulsCommonMax + 1),
        UpgradeRarity.Rare => Random.Range(soulsRareMin, soulsRareMax + 1),
        UpgradeRarity.Epic => Random.Range(soulsEpicMin, soulsEpicMax + 1),
        UpgradeRarity.Legendary => Random.Range(soulsLegendaryMin, soulsLegendaryMax + 1),
        _ => Random.Range(soulsCommonMin, soulsCommonMax + 1)
    };
}