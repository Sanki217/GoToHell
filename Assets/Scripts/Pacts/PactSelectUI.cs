using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu pact picker. Lists every PactDefinition in Resources/Pacts:
/// unlocked ones toggle on/off (up to MetaProgressionConfig.maxActivePacts),
/// locked ones show as "???" until their achievement is earned.
///
/// The selection is remembered in the save (SaveData.lastPactIds) and
/// MainMenu copies it into RunConfig when Play is pressed — so the next run
/// starts with the same pacts until the player changes them.
///
/// Build a default panel: Tools → Go To Hell → Progression → Build Pact + Meta Shop UI
/// (Main Menu scene). Every visual is replaceable; only the references matter.
/// </summary>
public class PactSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    [Tooltip("Parent for the rows (give it a VerticalLayoutGroup).")]
    public RectTransform content;
    [Tooltip("Optional styled row prefab (Button + TMP_Text). Empty = plain rows.")]
    public GameObject rowTemplate;
    public TMP_Text detailText;
    public TMP_Text headerText;
    public Button closeButton;

    private readonly List<(PactDefinition def, Button button, TMP_Text label)> rows =
        new List<(PactDefinition, Button, TMP_Text)>();

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (rowTemplate != null) rowTemplate.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>The saved selection, filtered to pacts that still exist and are unlocked.</summary>
    public static List<string> SavedSelection()
    {
        var result = new List<string>();
        int max = MetaProgression.Config.maxActivePacts;
        foreach (string id in SaveManager.Data.lastPactIds)
        {
            PactDefinition p = Content.Pacts.Get(id);
            if (p != null && p.IsUnlocked && !result.Contains(id) && result.Count < max) result.Add(id);
        }
        return result;
    }

    private void Rebuild()
    {
        UIRowFactory.Clear(content, rowTemplate);
        rows.Clear();

        foreach (PactDefinition def in Content.Pacts.All)
        {
            var (button, label) = UIRowFactory.Create(content, rowTemplate);
            PactDefinition captured = def;
            if (button != null) button.onClick.AddListener(() => Toggle(captured));
            rows.Add((def, button, label));
        }

        if (rows.Count == 0 && detailText != null)
            detailText.text = "No pacts yet.\nCreate PactDefinition assets in Assets/Resources/Pacts.";
        Refresh();
    }

    private void Toggle(PactDefinition def)
    {
        if (def == null) return;
        if (detailText != null) detailText.text = def.IsUnlocked ? def.BuildSummary() : LockedText();
        if (!def.IsUnlocked) return;

        List<string> sel = SavedSelection();
        int max = MetaProgression.Config.maxActivePacts;

        if (sel.Contains(def.pactId)) sel.Remove(def.pactId);
        else if (max > 0)
        {
            if (sel.Count >= max) sel.RemoveAt(0);   // replace the oldest pick
            sel.Add(def.pactId);
        }

        SaveManager.Data.lastPactIds = sel;
        SaveManager.Save();
        Refresh();
    }

    private void Refresh()
    {
        List<string> sel = SavedSelection();
        int heat = 0;

        foreach (var (def, button, label) in rows)
        {
            bool unlocked = def.IsUnlocked;
            bool on = sel.Contains(def.pactId);
            if (on) heat += def.heat;

            if (label != null)
                label.text = unlocked
                    ? $"<b>{def.displayName}</b>   <color=#FF6A3D>Heat {def.heat}</color>{(on ? "   <color=#FFD700>ACTIVE</color>" : "")}"
                    : "<color=#777777>??? — locked</color>";

            UIRowFactory.SetColor(button, !unlocked ? UIRowFactory.DisabledColor
                                         : on ? UIRowFactory.SelectedColor : UIRowFactory.RowColor);
        }

        if (headerText != null)
        {
            int max = MetaProgression.Config.maxActivePacts;
            headerText.text = $"PACTS   {sel.Count}/{max}   Heat {heat}";
        }
    }

    private static string LockedText() => "Locked.\nComplete achievements to unlock new pacts.";
}
