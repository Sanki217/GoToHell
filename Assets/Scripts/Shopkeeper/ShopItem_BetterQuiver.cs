public class ShopItem_BetterQuiver : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerStats stats = PlayerRefs.I?.Stats;
        if (stats == null) return;
        stats.MaxArrows += 1;
    }
}