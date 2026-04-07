using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a single upgrade card in the level-up picker.
/// Reads display data from the PlayerUpgrade MonoBehaviour on the offer's prefab.
/// Shows rolled stat bonuses that will be applied on collection.
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

    [Header("Rarity Background Alpha")]
    public float backgroundAlpha = 0.35f;

    private UpgradeOrbOffer offer;
    private System.Action<UpgradeOrbOffer> onPicked;

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Setup(UpgradeOrbOffer upgradeOffer, System.Action<UpgradeOrbOffer> pickedCallback)
    {
        offer = upgradeOffer;
        onPicked = pickedCallback;

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

        if (descriptionLabel != null)
            descriptionLabel.text = upgrade != null ? upgrade.description : "";

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
        {
            Destroy(statBonusContainer.GetChild(i).gameObject);
        }

        if (statBonusLinePrefab == null || offer.statBonuses == null) return;

        foreach (var bonus in offer.statBonuses)
        {
            GameObject line = Instantiate(statBonusLinePrefab, statBonusContainer);
            TMP_Text txt = line.GetComponent<TMP_Text>();
            if (txt == null) continue;
            txt.text = bonus.GetDescription();
            txt.color = bonus.value >= 0f ? Color.green : Color.red;
        }
    }

    private void OnPickClicked() => onPicked?.Invoke(offer);
}