using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Select — the 8 character slots (Diablo / No Rest for the Wicked
/// style). Pick a character → Play (opens Pact Select → run), or Create New,
/// or Delete (click twice to confirm). Fallen hardcore characters stay
/// visible as graves but can't be played.
///
/// MainMenu opens this on Play and listens to OnStartRun.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    [Tooltip("Parent for the slot rows (give it a VerticalLayoutGroup).")]
    public RectTransform content;
    [Tooltip("Optional styled row prefab (Button + TMP_Text). Empty = plain rows.")]
    public GameObject rowTemplate;
    public TMP_Text detailText;
    public Button playButton;
    public Button createButton;
    public Button deleteButton;
    public TMP_Text deleteButtonLabel;
    public Button backButton;

    [Header("Screens")]
    public CharacterCreateUI createUI;
    public PactSelectUI pactSelect;

    /// <summary>Character + pact chosen; MainMenu starts the run.</summary>
    public event Action<CharacterData, PactDefinition> OnStartRun;

    private readonly List<(CharacterData c, Button button, TMP_Text label)> rows =
        new List<(CharacterData, Button, TMP_Text)>();
    private string selectedId;
    private bool confirmDelete;

    private void Awake()
    {
        if (playButton != null)   playButton.onClick.AddListener(Play);
        if (createButton != null) createButton.onClick.AddListener(OpenCreate);
        if (deleteButton != null) deleteButton.onClick.AddListener(Delete);
        if (backButton != null)   backButton.onClick.AddListener(Close);
        if (rowTemplate != null)  rowTemplate.SetActive(false);
        if (createUI != null)     createUI.OnCreated += OnCreated;
        if (panel != null)        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (createUI != null) createUI.OnCreated -= OnCreated;
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        if (CharacterRoster.Get(selectedId) == null)
            selectedId = CharacterRoster.Characters.Count > 0 ? CharacterRoster.Characters[0].id : null;
        confirmDelete = false;
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ================================================================
    //  ACTIONS
    // ================================================================

    private void Play()
    {
        CharacterData c = CharacterRoster.Get(selectedId);
        if (c == null || !c.CanPlay) return;

        if (pactSelect != null) pactSelect.Open(c, pact => OnStartRun?.Invoke(c, pact));
        else OnStartRun?.Invoke(c, null);
    }

    private void OpenCreate()
    {
        if (CharacterRoster.IsFull || createUI == null) return;
        createUI.Open();
    }

    private void OnCreated(CharacterData c)
    {
        selectedId = c != null ? c.id : selectedId;
        Open();
    }

    private void Delete()
    {
        if (CharacterRoster.Get(selectedId) == null) return;
        if (!confirmDelete) { confirmDelete = true; Refresh(); return; }

        CharacterRoster.Delete(selectedId);
        selectedId = CharacterRoster.Characters.Count > 0 ? CharacterRoster.Characters[0].id : null;
        confirmDelete = false;
        Rebuild();
    }

    // ================================================================
    //  VIEW
    // ================================================================

    private void Rebuild()
    {
        UIRowFactory.Clear(content, rowTemplate);
        rows.Clear();

        IReadOnlyList<CharacterData> all = CharacterRoster.Characters;
        for (int i = 0; i < CharacterRoster.MaxCharacters; i++)
        {
            CharacterData c = i < all.Count ? all[i] : null;
            var (button, label) = UIRowFactory.Create(content, rowTemplate, 80f);
            if (button != null)
            {
                if (c != null) button.onClick.AddListener(() => { selectedId = c.id; confirmDelete = false; Refresh(); });
                else button.onClick.AddListener(OpenCreate);
            }
            rows.Add((c, button, label));
        }
        Refresh();
    }

    private void Refresh()
    {
        foreach (var (c, button, label) in rows)
        {
            if (label != null) label.text = c != null ? SlotLine(c) : "<color=#666666>— empty slot —</color>";
            Color col = c == null ? UIRowFactory.DisabledColor
                      : c.id == selectedId ? UIRowFactory.SelectedColor : UIRowFactory.RowColor;
            UIRowFactory.SetColor(button, col);
        }

        CharacterData sel = CharacterRoster.Get(selectedId);
        if (detailText != null) detailText.text = sel != null ? Details(sel) : "Create a character to begin your descent.";
        if (playButton != null) playButton.interactable = sel != null && sel.CanPlay;
        if (deleteButton != null) deleteButton.interactable = sel != null;
        if (deleteButtonLabel != null) deleteButtonLabel.text = confirmDelete ? "CONFIRM DELETE" : "DELETE";
        if (createButton != null) createButton.interactable = !CharacterRoster.IsFull;
    }

    private static string SlotLine(CharacterData c)
    {
        ClassDefinition cls = Content.Classes.Get(c.classId);
        string className = cls != null ? cls.displayName : c.classId;
        string hc = c.hardcore ? (c.dead ? "  <color=#FF4444>† FALLEN</color>" : "  <color=#FF6A3D>HARDCORE</color>") : "";
        return $"<b>{c.name}</b>   <color=#AAAAAA>{className} · Lv {c.level}</color>{hc}";
    }

    private static string Details(CharacterData c)
    {
        ClassDefinition cls = Content.Classes.Get(c.classId);
        var sb = new StringBuilder();
        sb.Append($"<size=120%><b>{c.name}</b></size>\n");
        sb.Append($"{(cls != null ? cls.displayName : c.classId)}  ·  Level {c.level}");
        if (c.hardcore) sb.Append(c.dead ? "  ·  <color=#FF4444>Fallen (hardcore)</color>" : "  ·  <color=#FF6A3D>Hardcore</color>");
        sb.Append("\n\n");

        foreach (PrimaryStat s in CharacterRoster.CreationStats)
        {
            int add = c.Allocated(s);
            sb.Append($"{CharacterCreateUI.StatName(s)}:  {c.FinalStat(s)}");
            if (add > 0) sb.Append($"  <color=#88FF88>(+{add})</color>");
            sb.Append('\n');
        }

        sb.Append($"\nRuns: {c.runsPlayed}   Victories: {c.victories}\n");
        sb.Append($"Deepest layer: {Mathf.Max(1, c.deepestLayer)}   Kills: {c.totalKills}");
        PactDefinition last = Content.Pacts.Get(c.lastPactId);
        if (last != null) sb.Append($"\nLast pact: {last.displayName}");
        return sb.ToString();
    }
}
