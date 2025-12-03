using UnityEngine;
using TMPro;

public class OrbUI : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public TMP_Text orbText;

    void Start()
    {
        if (playerInventory == null)
            playerInventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (playerInventory != null)
            playerInventory.OnOrbsChanged += UpdateText;

        UpdateText(playerInventory != null ? playerInventory.currentOrbs : 0);
    }

    void UpdateText(int amount)
    {
        if (orbText != null)
            orbText.text = "Orbs: " + amount;
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnOrbsChanged -= UpdateText;
    }
}
