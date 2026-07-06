using UnityEngine;

/// <summary>
/// Shop item: heals the player by healAmount (default 50), never above max HP.
/// Logs the purchase — if you buy and see no log line, the purchase itself
/// isn't registering (check the item prefab has a trigger collider).
/// </summary>
public class ShopItem_HealthPotion : ShopItem
{
    [Header("Health Potion")]
    [Tooltip("HP restored, clamped to max HP. 0 = heal to full.")]
    public int healAmount = 50;

    protected override void OnPurchased()
    {
        PlayerHealth health = PlayerRefs.I?.Health;
        if (health == null)
        {
            Debug.LogWarning("[HealthPotion] No player found — no heal applied.");
            return;
        }

        int before = health.CurrentHP;
        int amount = healAmount > 0 ? healAmount : health.maxHP;
        health.RestoreHP(amount);   // RestoreHP clamps at maxHP

        Debug.Log($"[HealthPotion] Healed {health.CurrentHP - before} HP " +
                  $"({before} → {health.CurrentHP} / {health.maxHP}).");
    }
}
