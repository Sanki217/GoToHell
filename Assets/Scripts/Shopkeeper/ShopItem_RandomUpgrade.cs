using UnityEngine;

public class ShopItem_RandomUpgrade : ShopItem
{
    [Header("Random Upgrade")]
    public PlayerUpgradePool upgradePool;
    public Vector3 orbSpawnOffset = new Vector3(0f, 1f, 0f);
    public float orbEjectForce = 3f;

    protected override void OnPurchased()
    {
        if (upgradePool == null) return;

        PlayerStats stats = PlayerRefs.I?.Stats;
        PlayerUpgradeManager mgr = PlayerRefs.I?.Upgrades;
        Transform playerTransform = PlayerRefs.I?.T;

        if (playerTransform == null) return;

        float luck = stats != null ? stats.luck : 0f;
        UpgradeOrbOffer offer = upgradePool.RollChestOffer(luck, stats, mgr);
        if (offer == null) return;

        Vector3 spawnPos = playerTransform.position + orbSpawnOffset;
        GameObject obj = Instantiate(offer.prefab, spawnPos, Quaternion.identity);
        UpgradeOrb orb = obj.GetComponent<UpgradeOrb>();
        if (orb == null) return;

        orb.rolledStatBonuses = offer.statBonuses?.ToArray();
        orb.rolledRarity = offer.rarity;
        orb.Initialize(orbSpawnOffset.normalized == Vector3.zero ? Vector3.up : orbSpawnOffset.normalized,
                       orbEjectForce);
    }
}