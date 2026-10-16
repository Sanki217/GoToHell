/// <summary>Shop item: +1 quiver capacity.</summary>
public class ShopItem_BetterQuiver : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerStats stats = PlayerRefs.I != null ? PlayerRefs.I.Stats : null;
        if (stats == null) return;
        stats.MaxArrows += 1;
    }
}
