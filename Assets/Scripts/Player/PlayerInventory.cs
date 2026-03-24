using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    public int currentSouls = 0;
    public event Action<int> OnSoulsChanged;

    public void AddSouls(int amount)
    {
        currentSouls += amount;
        OnSoulsChanged?.Invoke(currentSouls);
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