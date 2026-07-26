using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fullscreen chest reward screen.
/// Rolls one UpgradeOrbOffer (prefab + rarity + stat bonuses) from the pool.
/// Any key/click during the roll skips straight to the result. Once the result
/// is shown, any key (or the Collect button) collects it; if the player does
/// nothing it auto-collects after autoCollectDelay seconds.
/// On collect: spawns the orb which flies in and applies itself, and the source
/// chest bursts soul pickups into the world (see Chest.BurstSouls).
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

    [Header("Skip / Auto-Collect")]
    [Tooltip("Seconds after the result appears before it collects itself. Any key collects sooner.")]
    public float autoCollectDelay = 5f;

    [Header("Result Display")]
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

    [Header("Orb Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);
    public float spawnEjectForce = 3f;

    [Header("Player References — auto-found if not assigned")]
    public PlayerStats playerStats;
    public PlayerUpgradeManager upgradeManager;
    public PlayerStateController playerState;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private bool isOpen = false;
    private UpgradeOrbOffer pendingOffer;
    private Chest sourceChest;
    private Coroutine rollRoutine;

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
            }
        }

        if (collectButton != null)
            collectButton.onClick.AddListener(OnCollectClicked);
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Show(float luck, PlayerStats statsOverride = null, Chest chest = null)
    {
        if (isOpen || upgradePool == null || upgradePool.upgradePrefabs.Count == 0) return;

        PlayerStats effectiveStats = statsOverride ?? playerStats;
        float effectiveLuck = effectiveStats != null ? effectiveStats.luck : luck;

        pendingOffer = upgradePool.RollChestOffer(effectiveLuck, effectiveStats, upgradeManager);
        if (pendingOffer == null) return;

        sourceChest = chest;

        isOpen = true;
        GameTime.Pause();
        playerState?.DisableControl();

        chestPanel.SetActive(true);
        if (resultCard != null) resultCard.SetActive(false);
        if (collectButton != null) collectButton.gameObject.SetActive(false);
        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(true);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(true);

        rollRoutine = StartCoroutine(RollRoutine());
    }

    // ================================================================
    //  ROLLING ANIMATION
    // ================================================================

    private IEnumerator RollRoutine()
    {
        List<string> allNames = upgradePool.GetAllDisplayNames();
        float elapsed = 0f, nextChange = 0f;

        // Skip the frame the chest was opened on — the key/click that opened
        // it must not count as a skip press.
        yield return null;

        while (elapsed < rollDuration)
        {
            // Any key/click skips straight to the result
            if (Input.anyKeyDown) break;

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

        // Swallow the skip press's frame so it can't instantly collect too
        yield return null;

        // Any key collects; otherwise auto-collect after the delay
        float shown = 0f;
        while (shown < autoCollectDelay)
        {
            shown += Time.unscaledDeltaTime;
            if (Input.anyKeyDown) break;
            yield return null;
        }

        OnCollectClicked();
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

        UpgradeOrbOffer offerToSpawn = pendingOffer;
        Chest chest = sourceChest;
        Close();
        SpawnOrb(offerToSpawn);

        // Souls burst from the chest in the world, like a breaking vase
        chest?.BurstSouls();
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
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        isOpen = false;
        pendingOffer = null;
        sourceChest = null;
        GameTime.Resume();
        chestPanel.SetActive(false);

        if (rollingNameLabel != null) rollingNameLabel.gameObject.SetActive(true);
        if (rollingBackground != null) rollingBackground.gameObject.SetActive(true);

        playerState?.EnableControl();
    }
}
