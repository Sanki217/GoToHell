using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a single upgrade card in the level-up picker or chest reward screen.
///
/// Shows:
///   - Name, rarity colour, icon, description for the NEXT level
///   - "New Upgrade" if not owned, "Level X → Y" if upgrading
///   - Each stat bonus as: StatName  CurrentValue → NewValue (green)
///   - "MAXED" badge if a derived stat is already at its cap
///
/// SETUP:
///   statBonusContainer — empty Transform, gets stat line prefabs spawned inside it
///   statBonusLinePrefab — prefab with a TMP_Text component
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBackground;
    public Image iconImage;
    public TMP_Text rarityLabel;
    public TMP_Text levelLabel;         // "New Upgrade" or "Level 2 → 3"
    public TMP_Text nameLabel;
    public TMP_Text descriptionLabel;
    public Transform statBonusContainer;
    public GameObject statBonusLinePrefab; // prefab with TMP_Text
    public Button pickButton;

    [Header("Rarity Background Alpha")]
    public float backgroundAlpha = 0.35f;

    [HideInInspector] public UpgradeOffer offer;
    private System.Action<UpgradeCardUI> onPicked;
    private PlayerStats playerStats;

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Setup(UpgradeOffer upgradeOffer, System.Action<UpgradeCardUI> pickedCallback,
                      PlayerStats stats = null)
    {
        offer = upgradeOffer;
        onPicked = pickedCallback;
        playerStats = stats;

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        // Background tint
        if (cardBackground != null)
        {
            Color bg = rarityColor; bg.a = backgroundAlpha;
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

        // Level label: "New Upgrade" or "Level X → Y"
        if (levelLabel != null)
        {
            if (offer.currentLevel == 0)
                levelLabel.text = "New Upgrade";
            else
                levelLabel.text = $"Level {offer.currentLevel} → {offer.currentLevel + 1}";
        }

        // Name
        if (nameLabel != null)
            nameLabel.text = offer.data.displayName;

        // Description for the NEXT level
        int nextLevel = offer.currentLevel + 1;
        if (descriptionLabel != null)
            descriptionLabel.text = offer.data.GetDescription(nextLevel);

        // Stat bonus lines
        BuildStatLines();

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

    private void BuildStatLines()
    {
        if (statBonusContainer == null) return;

        // Clear existing lines
        for (int i = statBonusContainer.childCount - 1; i >= 0; i--)
        {
            var child = statBonusContainer.GetChild(i);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (statBonusLinePrefab == null || offer.statBonuses == null) return;

        foreach (var bonus in offer.statBonuses)
        {
            GameObject line = Instantiate(statBonusLinePrefab, statBonusContainer);
            TMP_Text txt = line.GetComponent<TMP_Text>();
            if (txt == null) continue;

            if (playerStats != null)
            {
                // Show current → new preview
                txt.text = bonus.GetPreviewLine(playerStats);
            }
            else
            {
                // Fallback: just show "+X StatName"
                txt.text = bonus.GetDescription();
                txt.color = Color.green;
            }
        }
    }

    private void OnPickClicked() => onPicked?.Invoke(this);
}