using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Reusable sprite carousel for the Character Creator: one active entry front
/// and center, one inactive entry on each side to the back, arrows to cycle.
/// Used for both the class and weapon stages.
///
/// Scene setup: three Images (center big, left/right smaller + behind — do the
/// sizing/positioning in the RectTransforms), two arrow Buttons, name +
/// description texts. The script only swaps sprites/tints; layout is yours.
///
/// Locked entries are shown darkened with "???" and fire OnSelectionChanged
/// with unlocked=false so the owner can disable its Next button.
/// </summary>
public class CreatorCarousel : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string id;
        public string displayName;
        [TextArea(2, 6)] public string description;
        public Sprite sprite;
        public bool unlocked;
    }

    [Header("Slots")]
    public Image centerImage;
    public Image leftImage;
    public Image rightImage;

    [Header("Controls")]
    public Button leftArrow;
    public Button rightArrow;

    [Header("Texts")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [Header("Locked Display")]
    public Color unlockedTint = Color.white;
    public Color lockedTint = new Color(0.08f, 0.08f, 0.08f, 1f);
    public string lockedName = "???";
    [TextArea(1, 3)] public string lockedDescription = "Locked.";

    // ── state ───────────────────────────────────────────────────────
    private readonly List<Entry> entries = new List<Entry>();
    private int index;

    /// <summary>Fired whenever the centered entry changes (and once on SetEntries).</summary>
    public event Action<Entry> OnSelectionChanged;

    public Entry Current => entries.Count > 0 ? entries[index] : null;

    private void Awake()
    {
        if (leftArrow  != null) leftArrow.onClick.AddListener(() => Step(-1));
        if (rightArrow != null) rightArrow.onClick.AddListener(() => Step(1));
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void SetEntries(List<Entry> newEntries, int startIndex = 0)
    {
        entries.Clear();
        if (newEntries != null)
            foreach (Entry e in newEntries)
                if (e != null) entries.Add(e);

        index = entries.Count > 0 ? Mathf.Clamp(startIndex, 0, entries.Count - 1) : 0;

        bool canCycle = entries.Count > 1;
        if (leftArrow  != null) leftArrow.gameObject.SetActive(canCycle);
        if (rightArrow != null) rightArrow.gameObject.SetActive(canCycle);

        Refresh();
        OnSelectionChanged?.Invoke(Current);
    }

    // ================================================================
    //  INTERNALS
    // ================================================================

    private void Step(int direction)
    {
        if (entries.Count < 2) return;
        index = (index + direction + entries.Count) % entries.Count;
        Refresh();
        OnSelectionChanged?.Invoke(Current);
    }

    private void Refresh()
    {
        if (entries.Count == 0)
        {
            ApplySlot(centerImage, null);
            ApplySlot(leftImage, null);
            ApplySlot(rightImage, null);
            if (nameText != null) nameText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            return;
        }

        Entry center = entries[index];
        ApplySlot(centerImage, center);

        bool showSides = entries.Count > 1;
        ApplySlot(leftImage,  showSides ? entries[(index - 1 + entries.Count) % entries.Count] : null);
        ApplySlot(rightImage, showSides ? entries[(index + 1) % entries.Count] : null);

        if (nameText != null)
            nameText.text = center.unlocked ? center.displayName : lockedName;
        if (descriptionText != null)
            descriptionText.text = center.unlocked ? center.description : lockedDescription;
    }

    private void ApplySlot(Image img, Entry e)
    {
        if (img == null) return;

        if (e == null || e.sprite == null)
        {
            img.enabled = false;
            return;
        }

        img.enabled = true;
        img.sprite = e.sprite;
        img.color = e.unlocked ? unlockedTint : lockedTint;
    }
}
