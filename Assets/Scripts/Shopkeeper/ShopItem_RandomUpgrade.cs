using UnityEngine;

/// <summary>
/// Shop item: buys a random upgrade, presented through the same rolling
/// reward UI as opening a chest (ChestRewardUI rolls the offer from its own
/// pool, pauses the game, animates, and applies the upgrade on Collect).
/// </summary>
public class ShopItem_RandomUpgrade : ShopItem
{
    protected override void OnPurchased()
    {
        if (ChestRewardUI.Instance == null)
        {
            Debug.LogWarning("[ShopItem_RandomUpgrade] No ChestRewardUI in scene — upgrade not granted.");
            return;
        }

        PlayerStats stats = PlayerRefs.I != null ? PlayerRefs.I.Stats : null;
        float luck = stats != null ? stats.luck : 0f;
        ChestRewardUI.Instance.Show(luck);
    }
}
