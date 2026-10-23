using UnityEngine;

/// <summary>
/// Runtime half of HealOnSoulEffect (Priest passive): heals on every soul
/// pickup. Added to the player by the effect — not placed by hand.
/// </summary>
[DisallowMultipleComponent]
public class HealOnSoulPassive : MonoBehaviour
{
    public int hpPerSoul;

    private PlayerInventory inventory;
    private PlayerHealth health;

    private void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        if (inventory != null) inventory.OnSoulsGained += OnSouls;
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnSoulsGained -= OnSouls;
    }

    private void OnSouls(int amount, bool bonus)
    {
        if (bonus || amount <= 0 || hpPerSoul <= 0 || health == null) return;
        health.RestoreHP(amount * hpPerSoul);
    }
}
