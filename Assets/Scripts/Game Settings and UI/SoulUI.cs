using UnityEngine;
using TMPro;

public class SoulUI : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public TMP_Text soulText;

    void Start()
    {
        if (playerInventory == null)
            playerInventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (playerInventory != null)
            playerInventory.OnSoulsChanged += UpdateText;

        UpdateText(playerInventory != null ? playerInventory.currentSouls : 0);
    }

    void UpdateText(int amount)
    {
        if (soulText != null)
            soulText.text = "Souls: " + amount;
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnSoulsChanged -= UpdateText;
    }
}