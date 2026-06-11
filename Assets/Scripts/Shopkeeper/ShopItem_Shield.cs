public class ShopItem_Shield : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerHealth health = PlayerRefs.I?.Health;
        if (health != null) health.AddShield(3);
    }
}