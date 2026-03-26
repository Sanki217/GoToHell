using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    public int currentSouls = 0;
    public event Action<int> OnSoulsChanged;

    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();
    }

    public void AddSouls(int amount)
    {
        currentSouls += amount;
        OnSoulsChanged?.Invoke(currentSouls);

        // Record in run history and fire upgrade event
        playerStats?.RecordSoulCollected(amount);
        upgradeManager?.SoulCollected(amount);
    }

    public bool SpendSouls(int amount)
    {
        if (currentSouls >= amount)
        {
            currentSouls -= amount;
            OnSoulsChanged?.Invoke(currentSouls);
            return true;
        }
        return false;
    }
}