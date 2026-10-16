/// <summary>Shop item: one resurrection this run (reset by RunManager at run start/end).</summary>
public class ShopItem_ExtraLife : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerHealth.HasExtraLife = true;
    }
}
