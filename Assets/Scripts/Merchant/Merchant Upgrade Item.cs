using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// A physical upgrade item in the merchant zone.
/// 
/// Behaviour:
///   - Price tag floats above always
///   - Player walks close → price grows, description panel appears, "Press E to buy" shown
///   - Player presses E with enough souls → souls deducted, item sucked to player, upgrade applied
///   - Player presses E without enough souls → flash red "Not enough souls"
///
/// Setup per item:
///   1. Place a GameObject in the merchant zone
///   2. Add this script and a SphereCollider (trigger, radius ~3)
///   3. Assign the upgradeData and set soulCost in Inspector
///   4. Wire all UI text/panel references
///   5. The item's upgradeData.upgradeId must exist in UpgradeFactory
///
/// The rarity for merchant items is pre-rolled when the scene loads.
/// Call RollOffer(layer, luck) from your level setup code, or set rarity manually.
/// </summary>
public class MerchantUpgradeItem : MonoBehaviour
{
    [Header("Upgrade")]
    public PlayerUpgradeData upgradeData;
    public UpgradeRarity rarity = UpgradeRarity.Common;
    public int soulCost = 50;

    [Header("World UI — always visible")]
    public TMP_Text priceLabel;         // floating price tag above item

    [Header("Proximity UI — shown on approach")]
    public GameObject proximityPanel;   // panel with description + buy prompt
    public TMP_Text descriptionLabel;
    public TMP_Text nameLabel;
    public TMP_Text rarityLabel;
    public TMP_Text buyPromptLabel;   // "Press E to buy" or "Not enough souls"
    public Transform statContainer;
    public GameObject statLinePrefab;

    [Header("Proximity Settings")]
    public float proximityRadius = 3f;   // should match SphereCollider radius
    public float priceScaleNormal = 1f;
    public float priceScaleHover = 1.4f;
    public float priceScaleSpeed = 8f;

    [Header("Suck Settings")]
    public float suckDuration = 0.6f;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private bool playerInRange = false;
    private bool purchased = false;
    private Transform playerTransform;
    private PlayerInventory inventory;
    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private UpgradeOffer offer;

    private float targetPriceScale;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            inventory = player.GetComponent<PlayerInventory>();
            upgradeManager = player.GetComponent<PlayerUpgradeManager>();
            playerStats = player.GetComponent<PlayerStats>();
        }

        // Build offer from data + rarity
        if (upgradeData != null)
        {
            // Find the pool to build stat bonuses — optional, item works without it
            PlayerUpgradePool pool = Resources.Load<PlayerUpgradePool>("UpgradePool");
            if (pool != null)
                offer = pool.BuildOffer(upgradeData, rarity);
            else
            {
                offer = new UpgradeOffer();
                offer.data = upgradeData;
                offer.rarity = rarity;
                offer.statBonuses = new System.Collections.Generic.List<UpgradeStatBonus>();
            }
        }

        // Set up price label
        UpdatePriceLabel();

        // Hide proximity panel initially
        if (proximityPanel != null) proximityPanel.SetActive(false);

        targetPriceScale = priceScaleNormal;

        // Populate proximity panel content (built once, shown/hidden on proximity)
        BuildProximityPanel();
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (purchased) return;

        // Price label scale lerp
        if (priceLabel != null)
        {
            float current = priceLabel.transform.localScale.x;
            float next = Mathf.Lerp(current, targetPriceScale, Time.deltaTime * priceScaleSpeed);
            priceLabel.transform.localScale = Vector3.one * next;
        }

        if (!playerInRange) return;

        // E to buy
        if (Input.GetKeyDown(KeyCode.E))
            TryPurchase();
    }

    // ================================================================
    //  TRIGGER — proximity detection
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        targetPriceScale = priceScaleHover;
        if (proximityPanel != null) proximityPanel.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        targetPriceScale = priceScaleNormal;
        if (proximityPanel != null) proximityPanel.SetActive(false);
    }

    // ================================================================
    //  PURCHASE
    // ================================================================

    private void TryPurchase()
    {
        if (inventory == null) return;

        if (!inventory.SpendSouls(soulCost))
        {
            // Not enough souls — flash the prompt red
            if (buyPromptLabel != null)
                StartCoroutine(FlashNotEnoughSouls());
            return;
        }

        // Purchase successful
        purchased = true;
        if (proximityPanel != null) proximityPanel.SetActive(false);

        StartCoroutine(SuckToPlayer());
    }

    private IEnumerator SuckToPlayer()
    {
        if (playerTransform == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;

        while (elapsed < suckDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / suckDuration;

            // Move toward player
            transform.position = Vector3.Lerp(startPos, playerTransform.position, t);
            // Shrink as it gets closer
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        // Apply upgrade
        ApplyUpgrade();
        Destroy(gameObject);
    }

    private void ApplyUpgrade()
    {
        if (offer == null || offer.data == null) return;

        // Apply stat bonuses
        if (playerStats != null)
            foreach (var bonus in offer.statBonuses)
                bonus.Apply(playerStats);

        // Apply behaviour upgrade
        if (upgradeManager != null)
        {
            PlayerUpgrade upgrade = UpgradeFactory.Create(offer.data.upgradeId);
            if (upgrade != null)
                upgradeManager.ApplyUpgrade(upgrade);
        }
    }

    // ================================================================
    //  PRIVATE HELPERS
    // ================================================================

    private void UpdatePriceLabel()
    {
        if (priceLabel == null) return;
        Color rarityColor = UpgradeRarityRoller.GetRarityColor(rarity);
        priceLabel.text = $"{soulCost} Souls";
        priceLabel.color = rarityColor;
    }

    private void BuildProximityPanel()
    {
        if (offer == null) return;

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(offer.rarity);

        if (nameLabel != null)
            nameLabel.text = offer.data.displayName;

        if (rarityLabel != null)
        {
            rarityLabel.text = UpgradeRarityRoller.GetRarityName(offer.rarity).ToUpper();
            rarityLabel.color = rarityColor;
        }

        if (descriptionLabel != null)
            descriptionLabel.text = offer.data.GetDescription(offer.rarity);

        if (statContainer != null && statLinePrefab != null)
        {
            foreach (Transform child in statContainer) Destroy(child.gameObject);
            foreach (var bonus in offer.statBonuses)
            {
                GameObject line = Instantiate(statLinePrefab, statContainer);
                TMP_Text txt = line.GetComponent<TMP_Text>();
                if (txt != null) { txt.text = bonus.GetDescription(); txt.color = bonus.value >= 0 ? Color.green : Color.red; }
            }
        }

        if (buyPromptLabel != null)
            buyPromptLabel.text = $"[E] Buy — {soulCost} Souls";
    }

    private IEnumerator FlashNotEnoughSouls()
    {
        if (buyPromptLabel == null) yield break;

        string original = buyPromptLabel.text;
        Color originalCol = buyPromptLabel.color;

        buyPromptLabel.text = "Not enough souls!";
        buyPromptLabel.color = Color.red;

        yield return new WaitForSeconds(1.2f);

        buyPromptLabel.text = original;
        buyPromptLabel.color = originalCol;
    }

    /// <summary>
    /// Call this from your level setup to randomise the offer on spawn.
    /// e.g. item.RollOffer(currentLayer, player.luck);
    /// </summary>
    public void RollOffer(int layer, float luck)
    {
        rarity = UpgradeRarityRoller.Roll(layer, luck);
        // Rebuild offer with new rarity
        PlayerUpgradePool pool = Resources.Load<PlayerUpgradePool>("UpgradePool");
        if (pool != null)
            offer = pool.BuildOffer(upgradeData, rarity);
        UpdatePriceLabel();
        BuildProximityPanel();
    }
}