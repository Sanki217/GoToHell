using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a single upgrade card in the level-up picker or chest reward screen.
///
/// IMPORTANT — Stat Bonus Lines:
///   The statBonusContainer should be an EMPTY Transform (no TMP_Text children in the scene).
///   The statBonusLinePrefab should be a PREFAB ASSET (not a child of this card).
///   Create it: right-click in Project → Create → Empty GameObject → add TMP_Text → drag to Prefabs folder.
///   If you have a default "New Text" child inside statBonusContainer, delete it from the scene.
/// </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBackground;
    public Image iconImage;
    public TMP_Text rarityLabel;
    public TMP_Text nameLabel;
    public TMP_Text descriptionLabel;
    public Transform statBonusContainer;   // should be empty — no children in scene
    public GameObject statBonusLinePrefab;  // prefab asset with a TMP_Text component
    public Button pickButton;

    [Header("Rarity Background Alpha")]
    public float backgroundAlpha = 0.35f;

    [HideInInspector] public UpgradeOffer offer;
    private System.Action<UpgradeCardUI> onPicked;

    public void Setup(UpgradeOffer upgradeOffer, System.Action<UpgradeCardUI> pickedCallback)
    {
        offer = upgradeOffer;
        onPicked = pickedCallback;

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        if (cardBackground != null)
        {
            Color bg = rarityColor;
            bg.a = backgroundAlpha;
            cardBackground.color = bg;
        }

        if (iconImage != null)
        {
            iconImage.sprite = offer.data.icon;
            iconImage.enabled = offer.data.icon != null;
        }

        if (rarityLabel != null)
        {
            rarityLabel.text = UpgradeRarityRoller.GetRarityName(offer.rarity).ToUpper();
            rarityLabel.color = rarityColor;
        }

        if (nameLabel != null)
            nameLabel.text = offer.data.displayName;

        if (descriptionLabel != null)
            descriptionLabel.text = offer.data.GetDescription(offer.rarity);

        // ── Stat Bonus Lines ──────────────────────────────────────────
        if (statBonusContainer != null)
        {
            // Disable all existing children immediately (avoids Destroy end-of-frame delay)
            // and destroy them so they don't accumulate
            for (int i = statBonusContainer.childCount - 1; i >= 0; i--)
            {
                var child = statBonusContainer.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            // Spawn one line per stat bonus
            if (statBonusLinePrefab != null && offer.statBonuses != null)
            {
                foreach (var bonus in offer.statBonuses)
                {
                    GameObject line = Instantiate(statBonusLinePrefab, statBonusContainer);
                    TMP_Text txt = line.GetComponent<TMP_Text>();
                    if (txt != null)
                    {
                        txt.text = bonus.GetDescription();
                        txt.color = bonus.value >= 0f ? Color.green : Color.red;
                    }
                }
            }
        }

        if (pickButton != null)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(OnPickClicked);
        }

        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnPickClicked() => onPicked?.Invoke(this);
}