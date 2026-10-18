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
    [Tooltip("One button per character slot (CharacterRoster.MaxCharacters), in order. Scene objects — style freely.")]
    public Button[] slotButtons = new Button[0];
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

    private readonly List<CharacterData> slotData = new List<CharacterData>();
    private string selectedId;
    private bool confirmDelete;

    private void Awake()
    {
        if (playButton != null)   playButton.onClick.AddListener(Play);
        if (createButton != null) createButton.onClick.AddListener(OpenCreate);
        if (deleteButton != null) deleteButton.onClick.AddListener(Delete);
        if (backButton != null)   backButton.onClick.AddListener(Close);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            if (slotButtons[i] != null) slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
        }
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

    private void OnSlotClicked(int index)
    {
        CharacterData c = index < slotData.Count ? slotData[index] : null;
        if (c == null) { OpenCreate(); return; }
        selectedId = c.id;
        confirmDelete = false;
        Refresh();
    }

    private void Rebuild()
    {
        slotData.Clear();
        IReadOnlyList<CharacterData> all = CharacterRoster.Characters;
        for (int i = 0; i < slotButtons.Length; i++) slotData.Add(i < all.Count ? all[i] : null);
        if (all.Count > slotButtons.Length)
            Debug.LogWarning($"[CharacterSelectUI] {all.Count} characters but only {slotButtons.Length} slot buttons.");
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button button = slotButtons[i];
            if (button == null) continue;
            CharacterData c = i < slotData.Count ? slotData[i] : null;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = c != null ? SlotLine(c) : "— empty slot —";
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
        string hc = c.hardcore ? (c.dead ? "  † FALLEN" : "  HARDCORE") : "";
        return $"<b>{c.name}</b>   {className} · Lv {c.level}{hc}";
    }

    private static string Details(CharacterData c)
    {
        ClassDefinition cls = Content.Classes.Get(c.classId);
        var sb = new StringBuilder();
        sb.Append($"<b>{c.name}</b>\n");
        sb.Append($"{(cls != null ? cls.displayName : c.classId)}  ·  Level {c.level}");
        if (c.hardcore) sb.Append(c.dead ? "  ·  Fallen (hardcore)" : "  ·  Hardcore");
        sb.Append("\n\n");

        foreach (PrimaryStat s in CharacterRoster.CreationStats)
        {
            int add = c.Allocated(s);
            sb.Append($"{CharacterCreateUI.StatName(s)}:  {c.FinalStat(s)}");
            if (add > 0) sb.Append($"  (+{add})");
            sb.Append('\n');
        }

        sb.Append($"\nRuns: {c.runsPlayed}   Victories: {c.victories}\n");
        sb.Append($"Deepest layer: {Mathf.Max(1, c.deepestLayer)}   Kills: {c.totalKills}");
        PactDefinition last = Content.Pacts.Get(c.lastPactId);
        if (last != null) sb.Append($"\nLast pact: {last.displayName}");
        return sb.ToString();
    }
}
