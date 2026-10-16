using UnityEngine;
using System;

/// <summary>
/// Player's soul wallet.
///
/// Events:
///   • OnSoulsChanged(total)          — any change (UI counters).
///   • OnSoulsGained(amount, isBonus) — souls added. XP listens to THIS, not
///     to total differences, so spending souls or bonus grants can never be
///     misread as income (Soul Bonus used to grant its XP twice).
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public int currentSouls = 0;

    public event Action<int> OnSoulsChanged;
    public event Action<int, bool> OnSoulsGained;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;
    private float soulCarry;   // fractional souls from RunModifiers.SoulGainMult

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();
    }

    /// <summary>
    /// Souls picked up in the world. Scaled by pacts / meta upgrades
    /// (RunModifiers.SoulGainMult); fractions carry over so ×1.1 still adds up.
    /// </summary>
    public void AddSouls(int amount)
    {
        if (amount <= 0) return;

        float scaled = amount * RunModifiers.SoulGainMult + soulCarry;
        amount = Mathf.FloorToInt(scaled);
        soulCarry = scaled - amount;
        if (amount <= 0) return;

        currentSouls += amount;
        OnSoulsChanged?.Invoke(currentSouls);
        OnSoulsGained?.Invoke(amount, false);

        // Record in run history and fire upgrade event
        if (playerStats != null) playerStats.RecordSoulCollected(amount);
        if (upgradeManager != null) upgradeManager.SoulCollected(amount);
    }

    /// <summary>
    /// Add souls that are already a computed bonus (e.g. from UpgradeSoulBonus).
    /// Updates the UI and stats tab but does NOT fire OnSoulCollected upgrade events
    /// (which would cause infinite loops in bonus upgrades).
    /// </summary>
    public void AddBonusSouls(int amount)
    {
        if (amount <= 0) return;
        currentSouls += amount;
        OnSoulsChanged?.Invoke(currentSouls);
        OnSoulsGained?.Invoke(amount, true);
        if (playerStats != null) playerStats.RecordSoulCollected(amount);
    }

    public bool SpendSouls(int amount)
    {
        if (amount < 0 || currentSouls < amount) return false;
        currentSouls -= amount;
        OnSoulsChanged?.Invoke(currentSouls);
        return true;
    }
}
