using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pact Select — the last screen before a run: pick the RMB skill this
/// character takes down (Dash, Shield…). Lists every PactDefinition; unlocked
/// ones are selectable, locked ones show as "???". The character's last pact
/// is pre-selected. Opened by CharacterSelectUI; "Descend" calls back with
/// the chosen pact.
/// </summary>
public class PactSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    [Tooltip("Parent for the rows (give it a VerticalLayoutGroup).")]
    public RectTransform content;
    [Tooltip("Disabled row inside content (Button + TMP_Text), cloned once per entry. Style it in the scene.")]
    public GameObject rowTemplate;
    public TMP_Text headerText;
    public TMP_Text detailText;
    public Button startButton;
    public Button backButton;

    private readonly List<(PactDefinition def, Button button, TMP_Text label)> rows =
        new List<(PactDefinition, Button, TMP_Text)>();
    private PactDefinition selected;
    private Action<PactDefinition> onStart;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (startButton != null) startButton.onClick.AddListener(Confirm);
        if (rowTemplate != null) rowTemplate.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Show the picker for a character. <paramref name="start"/> runs on "Descend".</summary>
    public void Open(CharacterData character, Action<PactDefinition> start)
    {
        onStart = start;
        selected = null;

        PactDefinition last = character != null ? Content.Pacts.Get(character.lastPactId) : null;
        if (last != null && last.IsUnlocked) selected = last;
        if (selected == null)
            foreach (PactDefinition p in Content.Pacts.All)
                if (p.IsUnlocked) { selected = p; break; }

        if (headerText != null)
            headerText.text = character != null ? $"CHOOSE A PACT — {character.name}" : "CHOOSE A PACT";

        if (panel != null) panel.SetActive(true);
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Confirm()
    {
        if (selected == null || !selected.IsUnlocked) return;
        Close();
        onStart?.Invoke(selected);
    }

    private void Rebuild()
    {
        UIRowFactory.Clear(content, rowTemplate);
        rows.Clear();

        foreach (PactDefinition def in Content.Pacts.All)
        {
            var (button, label) = UIRowFactory.Create(content, rowTemplate);
            PactDefinition captured = def;
            if (button != null) button.onClick.AddListener(() => Select(captured));
            rows.Add((def, button, label));
        }
        Refresh();
    }

    private void Select(PactDefinition def)
    {
        if (def != null && def.IsUnlocked) selected = def;
        Refresh(def);
    }

    private void Refresh(PactDefinition shown = null)
    {
        shown ??= selected;

        foreach (var (def, button, label) in rows)
        {
            bool unlocked = def.IsUnlocked;
            if (label != null)
                label.text = unlocked ? $"<b>{def.displayName}</b>" : "??? — locked";
            UIRowFactory.SetColor(button, !unlocked ? UIRowFactory.DisabledColor
                                         : def == selected ? UIRowFactory.SelectedColor : UIRowFactory.RowColor);
        }

        if (detailText != null)
            detailText.text = shown == null
                ? "No pacts unlocked.\nCreate PactDefinition assets in Assets/Resources/Pacts."
                : shown.IsUnlocked
                    ? $"<b>{shown.displayName}</b>\nRMB skill\n\n{shown.description}"
                    : "Locked.\nComplete achievements to unlock new pacts.";

        if (startButton != null) startButton.interactable = selected != null;
    }
}
