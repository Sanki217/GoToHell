using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    public int currentOrbs = 0;

    // optional events for UI
    public event Action<int> OnOrbsChanged;

    public void AddOrbs(int amount)
    {
        currentOrbs += amount;
        OnOrbsChanged?.Invoke(currentOrbs);
    }

    public bool SpendOrbs(int amount)
    {
        if (currentOrbs >= amount)
        {
            currentOrbs -= amount;
            OnOrbsChanged?.Invoke(currentOrbs);
            return true;
        }
        return false;
    }
}
