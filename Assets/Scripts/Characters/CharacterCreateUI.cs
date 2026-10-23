using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character creation: name (empty = "Unknown Sinner"), class (permanent —
/// passive, active skill, class upgrades; the weapon is picked in the town),
/// every stat starting at 1 with CharacterRoster.CreationPoints free points
/// to add (never below 1; Size/Cooldown excluded — gained in-game only), Auto to spend the
/// rest randomly, and an optional Hardcore flag (permanent death).
/// All points must be spent before Create is enabled. Locked classes are
/// listed but not selectable.
/// </summary>
public class CharacterCreateUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TMP_InputField nameInput;
    [Tooltip("Parent for class rows (VerticalLayoutGroup). One row per ClassDefinition is cloned from the template.")]
    public RectTransform classContent;
    [Tooltip("Disabled row inside classContent (Button + TMP_Text). Style it in the scene.")]
    public GameObject classRowTemplate;
    public TMP_Text classDetailText;
    [Tooltip("One row per creation stat — scene objects (label, − and + buttons).")]
    public StatRow[] statRows = new StatRow[0];
    public TMP_Text pointsText;
    public Toggle hardcoreToggle;
    [Tooltip("Spends the remaining points randomly.")]
    public Button autoButton;
    public Button createButton;
    public Button cancelButton;

    [Serializable]
    public class StatRow
    {
        public PrimaryStat stat;
        public TMP_Text label;
        public Button minus;
        public Button plus;
    }

    /// <summary>Fired after a character is saved.</summary>
    public event Action<CharacterData> OnCreated;

    private static PrimaryStat[] Stats => CharacterRoster.CreationStats;

    private readonly Dictionary<PrimaryStat, int> points = new Dictionary<PrimaryStat, int>();
    private readonly List<(ClassDefinition cls, Button button, TMP_Text label)> classRows =
        new List<(ClassDefinition, Button, TMP_Text)>();
    private ClassDefinition selectedClass;

    private int Remaining
    {
        get
        {
            int used = 0;
            foreach (int v in points.Values) used += v;
            return CharacterRoster.CreationPoints - used;
        }
    }

    private void Awake()
    {
        if (createButton != null) createButton.onClick.AddListener(Create);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        if (nameInput != null)
        {
            nameInput.characterLimit = CharacterRoster.MaxNameLength;
            if (nameInput.placeholder is TMP_Text ph) ph.text = CharacterRoster.DefaultName;
        }
        if (autoButton != null) autoButton.onClick.AddListener(AutoAllocate);
        if (classRowTemplate != null) classRowTemplate.SetActive(false);

        foreach (StatRow row in statRows)
        {
            if (row == null) continue;
            PrimaryStat captured = row.stat;
            if (row.minus != null) row.minus.onClick.AddListener(() => Change(captured, -1));
            if (row.plus != null)  row.plus.onClick.AddListener(() => Change(captured, +1));
        }
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        points.Clear();
        foreach (PrimaryStat s in Stats) points[s] = 0;
        selectedClass = null;
        foreach (ClassDefinition c in Content.Classes.All)
            if (c.IsUnlocked) { selectedClass = c; break; }

        if (nameInput != null) nameInput.text = "";
        if (hardcoreToggle != null) hardcoreToggle.isOn = false;
        if (panel != null) panel.SetActive(true);

        BuildClassRows();
        Refresh();
        if (nameInput != null) nameInput.ActivateInputField();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Create()
    {
        if (!CanCreate()) return;
        CharacterData c = CharacterRoster.Create(nameInput != null ? nameInput.text : "", selectedClass,
                                                 points, hardcoreToggle != null && hardcoreToggle.isOn);
        if (c == null) return;
        Close();
        OnCreated?.Invoke(c);
    }

    private bool CanCreate() =>
        selectedClass != null && selectedClass.IsUnlocked && Remaining == 0 && !CharacterRoster.IsFull;

    /// <summary>Spends every remaining point on random creation stats.</summary>
    private void AutoAllocate()
    {
        int left = Remaining;
        for (int i = 0; i < left; i++)
        {
            PrimaryStat s = Stats[UnityEngine.Random.Range(0, Stats.Length)];
            points[s]++;
        }
        Refresh();
    }

    // ================================================================
    //  BUILD
    // ================================================================

    private void BuildClassRows()
    {
        UIRowFactory.Clear(classContent, classRowTemplate);
        classRows.Clear();
        foreach (ClassDefinition c in Content.Classes.All)
        {
            var (button, label) = UIRowFactory.Create(classContent, classRowTemplate);
            ClassDefinition captured = c;
            if (button != null) button.onClick.AddListener(() =>
            {
                if (!captured.IsUnlocked) return;
                selectedClass = captured;
                Refresh();
            });
            classRows.Add((c, button, label));
        }
    }

    private void Change(PrimaryStat s, int delta)
    {
        int v = points[s] + delta;
        if (v < 0 || (delta > 0 && Remaining <= 0)) return;
        points[s] = v;
        Refresh();
    }

    private void Refresh()
    {
        foreach (var (c, button, label) in classRows)
        {
            if (label != null)
                label.text = c.IsUnlocked
                    ? $"<b>{c.displayName}</b>"
                    : "??? — locked";
            UIRowFactory.SetColor(button, !c.IsUnlocked ? UIRowFactory.DisabledColor
                                         : c == selectedClass ? UIRowFactory.SelectedColor : UIRowFactory.RowColor);
        }

        if (classDetailText != null)
        {
            if (selectedClass == null) classDetailText.text = "No class unlocked.";
            else
            {
                classDetailText.text = ClassSummary(selectedClass);
            }
        }

        foreach (StatRow row in statRows)
        {
            if (row == null || row.label == null) continue;
            int add = points.TryGetValue(row.stat, out int v) ? v : 0;
            row.label.text = $"{StatName(row.stat)}   <b>{CharacterRoster.BaseStatValue + add}</b>" + (add > 0 ? $"  (+{add})" : "");
            if (row.minus != null) row.minus.interactable = add > 0;
            if (row.plus != null)  row.plus.interactable = Remaining > 0;
        }

        if (pointsText != null)
            pointsText.text = Remaining > 0 ? $"Points left: <b>{Remaining}</b>" : "All points spent";

        if (createButton != null) createButton.interactable = CanCreate();
        if (autoButton != null) autoButton.interactable = Remaining > 0;
    }

    /// <summary>Class card text: description, passive, active skill, controls.</summary>
    public static string ClassSummary(ClassDefinition c)
    {
        if (c == null) return "";
        string passive = string.IsNullOrEmpty(c.passiveName) ? "—" : $"<b>{c.passiveName}</b> — {c.passiveDescription}";
        string active = string.IsNullOrEmpty(c.activeSkillName) ? "—" : $"<b>{c.activeSkillName}</b> — {c.activeSkillDescription}";
        string key = GameInput.BindingDisplay(GameAction.ClassSkill);
        string weapon = c.startingWeapon != null ? c.startingWeapon.displayName : "any";
        return $"<b>{c.displayName}</b>\n{c.description}\n\n" +
               $"Passive: {passive}\n" +
               $"Skill ({key}): {active}\n\n" +
               $"Starts with: {weapon} (swap weapons in town)";
    }

    public static string StatName(PrimaryStat s) => s switch
    {
        PrimaryStat.AttackDamage => "Attack Damage",
        _ => s.ToString()
    };
}
