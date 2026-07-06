using UnityEngine;
using TMPro;

/// <summary>
/// Scene-side souls counter. Resolves the (possibly runtime-spawned) player
/// via PlayerRefs in Start — PlayerSpawner runs in Awake, so the player
/// always exists by now. No HUDRefs entry needed.
/// </summary>
public class SoulUI : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public TMP_Text soulText;

    void Start()
    {
        if (playerInventory == null)
            playerInventory = PlayerRefs.I != null ? PlayerRefs.I.Inventory : null;
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
