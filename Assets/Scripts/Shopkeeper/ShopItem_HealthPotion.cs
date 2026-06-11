public class ShopItem_HealthPotion : ShopItem
{
    protected override void OnPurchased()
    {
        PlayerHealth health = PlayerRefs.I?.Health;
        if (health == null) return;
        health.RestoreHP(health.maxHP);
    }
}