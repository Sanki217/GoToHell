/// <summary>Shop item: 3 shield charges (each absorbs one hit).</summary>
public class ShopItem_Shield : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerHealth health = PlayerRefs.I != null ? PlayerRefs.I.Health : null;
        if (health != null) health.AddShield(3);
    }
}
