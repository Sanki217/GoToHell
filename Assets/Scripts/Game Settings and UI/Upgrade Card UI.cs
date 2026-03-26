using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Drives a single upgrade card in the level-up picker or chest reward screen.
/// Wire up all UI references in the Inspector.
///
/// Card layout (build this in your Canvas):
///   - Background Image (tinted by rarity color)
///   - Icon Image
///   - Rarity Label (TMP)
///   - Name Label (TMP)
///   - Description Label (TMP)
///   - Stat Bonus container with StatBonusLinePrefab children
///   - Pick Button
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBackground;
    public Image iconImage;
    public TMP_Text rarityLabel;
    public TMP_Text nameLabel;
    public TMP_Text descriptionLabel;
    public Transform statBonusContainer;  // parent for stat bonus lines
    public GameObject statBonusLinePrefab; // prefab: just a TMP_Text
    public Button pickButton;

    [Header("Rarity Background Alpha")]
    public float backgroundAlpha = 0.35f;

    // Set externally by LevelUpUI before showing
    [HideInInspector] public UpgradeOffer offer;

    private System.Action<UpgradeCardUI> onPicked;

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Setup(UpgradeOffer upgradeOffer, System.Action<UpgradeCardUI> pickedCallback)
    {
        offer = upgradeOffer;
        onPicked = pickedCallback;

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        // Background tint
        if (cardBackground != null)
        {
            Color bg = rarityColor;
            bg.a = backgroundAlpha;
            cardBackground.color = bg;
        }

        // Icon
        if (iconImage != null)
        {
            iconImage.sprite = offer.data.icon;
            iconImage.enabled = offer.data.icon != null;
        }

        // Rarity label
        if (rarityLabel != null)
        {
            rarityLabel.text = UpgradeRarityRoller.GetRarityName(offer.rarity).ToUpper();
            rarityLabel.color = rarityColor;
        }

        // Name
        if (nameLabel != null)
            nameLabel.text = offer.data.displayName;

        // Description
        if (descriptionLabel != null)
            descriptionLabel.text = offer.data.GetDescription(offer.rarity);

        // Stat bonuses
        if (statBonusContainer != null)
        {
            // Clear old lines
            foreach (Transform child in statBonusContainer)
                Destroy(child.gameObject);

            // Spawn a line per stat bonus
            foreach (var bonus in offer.statBonuses)
            {
                if (statBonusLinePrefab == null) break;
                GameObject line = Instantiate(statBonusLinePrefab, statBonusContainer);
                TMP_Text txt = line.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.text = bonus.GetDescription();
                    txt.color = bonus.value >= 0f ? Color.green : Color.red;
                }
            }
        }

        // Button
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

    private void OnPickClicked()
    {
        onPicked?.Invoke(this);
    }
}