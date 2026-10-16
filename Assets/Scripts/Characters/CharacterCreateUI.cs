using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character creation: name (empty = "Unknown Sinner"), class (= weapon),
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
    [Tooltip("Parent for class rows (VerticalLayoutGroup).")]
    public RectTransform classContent;
    public TMP_Text classDetailText;
    [Tooltip("Parent for the stat rows (VerticalLayoutGroup).")]
    public RectTransform statsContent;
    public TMP_Text pointsText;
    public Toggle hardcoreToggle;
    [Tooltip("Spends the remaining points randomly. Created at runtime next to the points text if empty.")]
    public Button autoButton;
    public Button createButton;
    public Button cancelButton;

    /// <summary>Fired after a character is saved.</summary>
    public event Action<CharacterData> OnCreated;

    private static PrimaryStat[] Stats => CharacterRoster.CreationStats;

    private readonly Dictionary<PrimaryStat, int> points = new Dictionary<PrimaryStat, int>();
    private readonly List<(ClassDefinition cls, Button button, TMP_Text label)> classRows =
        new List<(ClassDefinition, Button, TMP_Text)>();
    private readonly Dictionary<PrimaryStat, TMP_Text> statLabels = new Dictionary<PrimaryStat, TMP_Text>();
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
        if (autoButton == null) autoButton = CreateAutoButton();
        if (autoButton != null) autoButton.onClick.AddListener(AutoAllocate);
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
        BuildStatRows();
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

    /// <summary>Fallback for panels built before the Auto button existed: a small copy of Create.</summary>
    private Button CreateAutoButton()
    {
        if (createButton == null || pointsText == null) return null;
        GameObject go = Instantiate(createButton.gameObject, createButton.transform.parent);
        go.name = "Auto";
        Button b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        RectTransform rt = (RectTransform)go.transform;
        RectTransform pt = pointsText.rectTransform;
        rt.anchorMin = rt.anchorMax = pt.anchorMin;
        rt.sizeDelta = new Vector2(150f, pt.sizeDelta.y);
        rt.anchoredPosition = pt.anchoredPosition + new Vector2(pt.sizeDelta.x * 0.5f - 75f, 0f);
        TMP_Text label = go.GetComponentInChildren<TMP_Text>();
        if (label != null) { label.text = "AUTO"; label.fontSize = 24; }
        pointsText.alignment = TextAlignmentOptions.MidlineLeft;
        return b;
    }

    // ================================================================
    //  BUILD
    // ================================================================

    private void BuildClassRows()
    {
        UIRowFactory.Clear(classContent);
        classRows.Clear();
        foreach (ClassDefinition c in Content.Classes.All)
        {
            var (button, label) = UIRowFactory.Create(classContent, null, 70f);
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

    private void BuildStatRows()
    {
        UIRowFactory.Clear(statsContent);
        statLabels.Clear();
        foreach (PrimaryStat s in Stats)
        {
            var (label, minus, plus) = UIRowFactory.CreateStepper(statsContent);
            PrimaryStat captured = s;
            minus.onClick.AddListener(() => Change(captured, -1));
            plus.onClick.AddListener(() => Change(captured, +1));
            statLabels[s] = label;
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
                    ? $"<b>{c.displayName}</b>   <color=#AAAAAA>{(c.weapon != null ? c.weapon.displayName : "")}</color>"
                    : "<color=#777777>??? — locked</color>";
            UIRowFactory.SetColor(button, !c.IsUnlocked ? UIRowFactory.DisabledColor
                                         : c == selectedClass ? UIRowFactory.SelectedColor : UIRowFactory.RowColor);
        }

        if (classDetailText != null)
        {
            if (selectedClass == null) classDetailText.text = "No class unlocked.";
            else
            {
                string alt = string.IsNullOrEmpty(selectedClass.altAttackDisplayName) ? "—" : selectedClass.altAttackDisplayName;
                string weapon = selectedClass.weapon != null ? selectedClass.weapon.displayName : "—";
                classDetailText.text = $"<b>{selectedClass.displayName}</b>\n{selectedClass.description}\n\n" +
                                       $"LMB: {weapon}\nShift+LMB: {alt}\nRMB: chosen per run (pact)";
            }
        }

        foreach (PrimaryStat s in Stats)
        {
            if (!statLabels.TryGetValue(s, out TMP_Text label) || label == null) continue;
            int baseVal = CharacterRoster.BaseStatValue;
            int add = points[s];
            label.text = $"{StatName(s)}   <b>{baseVal + add}</b>" + (add > 0 ? $"  <color=#88FF88>(+{add})</color>" : "");
        }

        if (pointsText != null)
            pointsText.text = Remaining > 0 ? $"Points left: <b>{Remaining}</b>" : "<color=#88FF88>All points spent</color>";

        if (createButton != null) createButton.interactable = CanCreate();
        if (autoButton != null) autoButton.interactable = Remaining > 0;
    }

    public static string StatName(PrimaryStat s) => s switch
    {
        PrimaryStat.AttackDamage => "Attack Damage",
        _ => s.ToString()
    };
}
