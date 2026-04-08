using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A physical upgrade item in the merchant zone.
///
/// Setup per item:
///   1. Place a GameObject in the merchant zone with a mesh + SphereCollider (trigger, radius ~3)
///   2. Add this script
///   3. Assign upgradePrefab — a prefab with UpgradeOrb + a PlayerUpgrade subclass on it
///   4. Assign upgradePool so stat bonuses can be rolled on Start
///   5. Set soulCost and rarity (or call RollOffer() from level setup code)
///   6. Wire all UI text/panel references
///
/// On purchase: souls are deducted, the orb prefab is spawned at this item's position,
/// and the orb flies to the player and applies itself exactly like a level-up orb.
/// </summary>
public class MerchantUpgradeItem : MonoBehaviour
{
    [Header("Upgrade")]
    [Tooltip("Orb prefab — must have UpgradeOrb + a PlayerUpgrade subclass on it.")]
    public GameObject upgradePrefab;
    public PlayerUpgradePool upgradePool;
    public UpgradeRarity rarity = UpgradeRarity.Common;
    public int soulCost = 50;

    [Header("World UI — always visible")]
    public TMP_Text priceLabel;

    [Header("Proximity UI — shown on approach")]
    public GameObject proximityPanel;
    public TMP_Text descriptionLabel;
    public TMP_Text nameLabel;
    public TMP_Text rarityLabel;
    public TMP_Text buyPromptLabel;
    public Transform statContainer;
    public GameObject statLinePrefab;

    [Header("Proximity Settings")]
    public float proximityRadius = 3f;
    public float priceScaleNormal = 1f;
    public float priceScaleHover = 1.4f;
    public float priceScaleSpeed = 8f;

    [Header("Orb Spawn")]
    [Tooltip("Upward pop force when the orb is released on purchase.")]
    public float spawnEjectForce = 4f;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private bool playerInRange = false;
    private bool purchased = false;

    private Transform playerTransform;
    private PlayerInventory inventory;
    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;

    // Rolled once on Start and displayed; injected into the orb on purchase
    private UpgradeOrbOffer rolledOffer;

    private float targetPriceScale;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            inventory = player.GetComponent<PlayerInventory>();
            upgradeManager = player.GetComponent<PlayerUpgradeManager>();
            playerStats = player.GetComponent<PlayerStats>();
        }

        if (upgradePrefab != null && upgradePool != null)
        {
            PlayerUpgrade upgrade = upgradePrefab.GetComponent<PlayerUpgrade>();
            if (upgrade != null)
                rolledOffer = upgradePool.BuildOffer(upgradePrefab, upgrade, rarity, playerStats);
        }

        UpdatePriceLabel();
        if (proximityPanel != null) proximityPanel.SetActive(false);
        targetPriceScale = priceScaleNormal;
        BuildProximityPanel();
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (purchased) return;

        if (priceLabel != null)
        {
            float current = priceLabel.transform.localScale.x;
            float next = Mathf.Lerp(current, targetPriceScale, Time.deltaTime * priceScaleSpeed);
            priceLabel.transform.localScale = Vector3.one * next;
        }

        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryPurchase();
    }

    // ================================================================
    //  TRIGGER
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
            if (buyPromptLabel != null)
                StartCoroutine(FlashNotEnoughSouls());
            return;
        }

        purchased = true;
        if (proximityPanel != null) proximityPanel.SetActive(false);

        SpawnOrb();
        Destroy(gameObject);
    }

    private void SpawnOrb()
    {
        if (upgradePrefab == null) return;

        GameObject obj = Instantiate(upgradePrefab, transform.position, Quaternion.identity);
        UpgradeOrb orb = obj.GetComponent<UpgradeOrb>();
        if (orb == null) return;

        // Inject the stat bonuses that were rolled and displayed
        orb.rolledStatBonuses = rolledOffer?.statBonuses?.ToArray();
        orb.rolledRarity = rarity;

        // Pop upward — Looter will pick it up and attract it
        orb.Initialize(Vector3.up, spawnEjectForce);
    }

    // ================================================================
    //  UI HELPERS
    // ================================================================

    private void UpdatePriceLabel()
    {
        if (priceLabel == null) return;
        priceLabel.text = $"{soulCost} Souls";
        priceLabel.color = UpgradeRarityRoller.GetRarityColor(rarity);
    }

    private void BuildProximityPanel()
    {
        PlayerUpgrade upgrade = upgradePrefab != null
            ? upgradePrefab.GetComponent<PlayerUpgrade>() : null;

        if (upgrade == null) return;

        Color rarityColor = UpgradeRarityRoller.GetRarityColor(rarity);

        if (nameLabel != null)
            nameLabel.text = upgrade.displayName;

        if (rarityLabel != null)
        {
            rarityLabel.text = UpgradeRarityRoller.GetRarityName(rarity).ToUpper();
            rarityLabel.color = rarityColor;
        }

        if (descriptionLabel != null)
            descriptionLabel.text = upgrade.description;

        if (statContainer != null && statLinePrefab != null && rolledOffer?.statBonuses != null)
        {
            foreach (Transform child in statContainer) Destroy(child.gameObject);
            foreach (var bonus in rolledOffer.statBonuses)
            {
                GameObject line = Instantiate(statLinePrefab, statContainer);
                TMP_Text txt = line.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.text = bonus.GetDescription();
                    txt.color = bonus.value >= 0 ? Color.green : Color.red;
                }
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
    /// Call from level setup to randomise rarity and re-roll stat bonuses.
    /// e.g. item.RollOffer(currentLayer, player.luck);
    /// </summary>
    public void RollOffer(int layer, float luck)
    {
      //  rarity = UpgradeRarityRoller.Roll(layer, luck);

        if (upgradePrefab != null && upgradePool != null)
        {
            PlayerUpgrade upgrade = upgradePrefab.GetComponent<PlayerUpgrade>();
            if (upgrade != null)
                rolledOffer = upgradePool.BuildOffer(upgradePrefab, upgrade, rarity, playerStats);
        }

        UpdatePriceLabel();
        BuildProximityPanel();
    }
}