using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Town smith: upgrades the character's EQUIPPED weapon one level at a time
/// for Brimstone. Shows every level of the weapon (bought ones ticked).
/// Buying rebuilds the player so the new level applies at once.
/// </summary>
public class SmithUI : MonoBehaviour, ITownPanel
{
    [Header("UI")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text levelsText;
    public TMP_Text currencyText;
    public Button upgradeButton;
    public TMP_Text upgradeButtonLabel;
    public Button closeButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private CharacterData Character => TownManager.I != null ? TownManager.I.Character : null;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(Upgrade);
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Upgrade()
    {
        CharacterData c = Character;
        WeaponDefinition w = CharacterRoster.EquippedWeapon(c);
        if (!CharacterRoster.TryUpgradeWeapon(c, w)) { Refresh(); return; }
        Close();
        if (TownManager.I != null) TownManager.I.RespawnPlayer();   // apply the new level now
    }

    private void Refresh()
    {
        CharacterData c = Character;
        WeaponDefinition w = CharacterRoster.EquippedWeapon(c);
        int level = c != null && w != null ? c.WeaponLevel(w.weaponId) : 0;
        int cost = w != null ? w.NextLevelCost(level) : -1;

        if (titleText != null)
            titleText.text = w == null ? "No weapon equipped" : $"{w.displayName}   Level {level} / {w.MaxLevel}";

        if (levelsText != null)
        {
            var sb = new StringBuilder();
            if (w == null || w.MaxLevel == 0) sb.Append("This weapon can't be upgraded yet.");
            else
                for (int i = 0; i < w.MaxLevel; i++)
                {
                    WeaponDefinition.WeaponLevel l = w.levels[i];
                    string mark = i < level ? "[x]" : i == level ? "[>]" : "[ ]";
                    sb.Append($"{mark}  Level {i + 1}: {l.description}");
                    if (i >= level) sb.Append($"   ({l.cost} {MetaProgression.CurrencyName})");
                    sb.Append('\n');
                }
            levelsText.text = sb.ToString();
        }

        if (currencyText != null)
            currencyText.text = $"{MetaProgression.Currency} {MetaProgression.CurrencyName}";

        bool canBuy = cost >= 0 && MetaProgression.Currency >= cost;
        if (upgradeButton != null) upgradeButton.interactable = canBuy;
        if (upgradeButtonLabel != null)
            upgradeButtonLabel.text = cost < 0 ? "Max level" : $"Upgrade ({cost})";
    }
}
