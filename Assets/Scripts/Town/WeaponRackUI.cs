using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Town weapon rack: lists every weapon; unlocked ones can be equipped
/// (locked ones show as "??? — locked"). Equipping rebuilds the player with
/// the new weapon. Rows are cloned from a disabled RowTemplate in the scene.
/// </summary>
public class WeaponRackUI : MonoBehaviour, ITownPanel
{
    [Header("UI")]
    public GameObject panel;
    public RectTransform content;
    [Tooltip("Disabled row inside content (Button + TMP_Text). Style it in the scene.")]
    public GameObject rowTemplate;
    public TMP_Text detailText;
    public Button equipButton;
    public Button closeButton;

    private readonly List<(WeaponDefinition def, Button button, TMP_Text label)> rows =
        new List<(WeaponDefinition, Button, TMP_Text)>();
    private WeaponDefinition selected;

    public bool IsOpen => panel != null && panel.activeSelf;

    private CharacterData Character => TownManager.I != null ? TownManager.I.Character : null;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (equipButton != null) equipButton.onClick.AddListener(Equip);
        if (rowTemplate != null) rowTemplate.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        selected = CharacterRoster.EquippedWeapon(Character);
        if (panel != null) panel.SetActive(true);
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Equip()
    {
        CharacterData c = Character;
        if (c == null || selected == null || !selected.IsUnlocked) return;
        if (c.equippedWeaponId == selected.weaponId) { Close(); return; }
        if (!CharacterRoster.EquipWeapon(c, selected)) return;
        Close();
        if (TownManager.I != null) TownManager.I.RespawnPlayer();   // rebuild the player with the new weapon
    }

    private void Rebuild()
    {
        UIRowFactory.Clear(content, rowTemplate);
        rows.Clear();
        foreach (WeaponDefinition w in Content.Weapons.All)
        {
            var (button, label) = UIRowFactory.Create(content, rowTemplate);
            WeaponDefinition captured = w;
            if (button != null) button.onClick.AddListener(() => { selected = captured; Refresh(); });
            rows.Add((w, button, label));
        }
        Refresh();
    }

    private void Refresh()
    {
        CharacterData c = Character;
        string equippedId = c != null ? CharacterRoster.EquippedWeapon(c)?.weaponId : null;

        foreach (var (def, button, label) in rows)
        {
            bool unlocked = def.IsUnlocked;
            int lvl = c != null ? c.WeaponLevel(def.weaponId) : 0;
            if (label != null)
                label.text = !unlocked ? "??? — locked"
                    : $"<b>{def.displayName}</b>{(lvl > 0 ? $" +{lvl}" : "")}{(def.weaponId == equippedId ? "   [equipped]" : "")}";
            UIRowFactory.SetColor(button, !unlocked ? UIRowFactory.DisabledColor
                                         : def == selected ? UIRowFactory.SelectedColor : UIRowFactory.RowColor);
        }

        if (detailText != null)
        {
            if (selected == null) detailText.text = "No weapons.\nCreate WeaponDefinition assets in Assets/Resources/Weapons.";
            else if (!selected.IsUnlocked) detailText.text = "Locked.\nFind or earn this weapon to use it.";
            else
            {
                int lvl = c != null ? c.WeaponLevel(selected.weaponId) : 0;
                detailText.text = $"<b>{selected.displayName}</b>   Level {lvl} / {selected.MaxLevel}\n\n{selected.description}";
            }
        }

        if (equipButton != null)
        {
            equipButton.interactable = selected != null && selected.IsUnlocked;
            TMP_Text l = equipButton.GetComponentInChildren<TMP_Text>(true);
            if (l != null) l.text = selected != null && selected.weaponId == equippedId ? "Equipped" : "Equip";
        }
    }
}
