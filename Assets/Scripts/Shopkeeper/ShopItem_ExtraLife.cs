public class ShopItem_ExtraLife : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerHealth.HasExtraLife = true;
    }
}