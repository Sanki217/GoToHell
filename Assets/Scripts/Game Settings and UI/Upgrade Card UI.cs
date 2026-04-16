using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Drives a single upgrade card in the level-up picker.
///
/// Description: calls upgrade.GetDynamicDescription(stats, simulatedBonuses) — returns
/// rich-text with live-calculated numbers coloured by primary stat.
///
/// NEW badge: shown when the upgrade ID hasn't been seen this session.
///
/// Stat lines: always show current → after picking.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBackground;
    public Image iconImage;
    public TMP_Text rarityLabel;
    public TMP_Text nameLabel;
    public TMP_Text descriptionLabel;
    public Transform statBonusContainer;
    public GameObject statBonusLinePrefab;
    public Button pickButton;

    [Header("NEW Badge")]
    [Tooltip("GameObject containing the 'NEW' label. Hidden if upgrade was seen before this session.")]
    public GameObject newBadge;

    [Header("Rarity Background Alpha")]
    public float backgroundAlpha = 0.35f;

    // ================================================================
    //  SESSION-SCOPED SEEN SET
    // ================================================================
    private static readonly HashSet<string> seenUpgradeIds = new HashSet<string>();

    private UpgradeOrbOffer offer;
    private System.Action<UpgradeOrbOffer> onPicked;
    private PlayerStats playerStats;

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Setup(UpgradeOrbOffer upgradeOffer, System.Action<UpgradeOrbOffer> pickedCallback)
    {
        offer = upgradeOffer;
        onPicked = pickedCallback;

        if (playerStats == null)
            playerStats = PlayerRefs.I?.Stats;

        PlayerUpgrade upgrade = offer.upgrade;
        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        if (cardBackground != null)
        {
            Color bg = rarityColor; bg.a = backgroundAlpha;
            cardBackground.color = bg;
        }

        if (iconImage != null)
        {
            iconImage.sprite = upgrade != null ? upgrade.icon : null;
            iconImage.enabled = upgrade != null && upgrade.icon != null;
        }

        if (rarityLabel != null)
        {
            rarityLabel.text = UpgradeRarityRoller.GetRarityName(offer.rarity).ToUpper();
            rarityLabel.color = rarityColor;
        }

        if (nameLabel != null)
            nameLabel.text = upgrade != null ? upgrade.displayName : "";

        // Dynamic description — live-calculated coloured numbers
        if (descriptionLabel != null)
        {
            descriptionLabel.text = upgrade != null
                ? upgrade.GetDynamicDescription(playerStats, offer.statBonuses)
                : "";
        }

        // NEW badge — visible only on first time this upgrade is shown this session
        string upgradeId = upgrade != null ? upgrade.UpgradeId : "";
        bool isNew = !string.IsNullOrEmpty(upgradeId) && !seenUpgradeIds.Contains(upgradeId);
        if (newBadge != null) newBadge.SetActive(isNew);
        if (!string.IsNullOrEmpty(upgradeId)) seenUpgradeIds.Add(upgradeId);

        BuildStatLines();

        if (pickButton != null)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(OnPickClicked);
        }

        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void BuildStatLines()
    {
        if (statBonusContainer == null) return;

        for (int i = statBonusContainer.childCount - 1; i >= 0; i--)
            Destroy(statBonusContainer.GetChild(i).gameObject);

        if (statBonusLinePrefab == null || offer.statBonuses == null) return;

        foreach (var bonus in offer.statBonuses)
        {
            GameObject line = Instantiate(statBonusLinePrefab, statBonusContainer);
            TMP_Text txt = line.GetComponent<TMP_Text>();
            if (txt == null) continue;

            txt.text = playerStats != null
                ? bonus.GetPreviewLine(playerStats)
                : bonus.GetDescription();
            txt.color = bonus.value >= 0f ? Color.green : Color.red;
        }
    }

    private void OnPickClicked() => onPicked?.Invoke(offer);
}