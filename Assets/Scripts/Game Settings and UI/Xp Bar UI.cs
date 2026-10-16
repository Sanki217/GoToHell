using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the XP bar in the HUD.
///
/// Setup:
///   1. Create a UI Slider in your Canvas — Min=0, Max=1, Whole Numbers=off
///   2. Optionally add TMP_Text elements for level and XP numbers
///   3. Add this script to any GameObject, wire references in Inspector
/// </summary>
public class XpBarUI : MonoBehaviour
{
    [Header("References")]
    public Slider xpSlider;    // fill bar — value goes 0-1
    public TMP_Text levelText;   // shows "Lv 5"
    public TMP_Text xpText;      // shows "240 / 353 XP"

    private PlayerLevelSystem levelSystem;

    // Last displayed values — TMP text is only rebuilt when something changed
    private int shownLevel = -1;
    private int shownXP = -1;
    private int shownXPToNext = -1;

    private void Start()
    {
        levelSystem = PlayerRefs.I != null ? PlayerRefs.I.LevelSystem : null;

        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;
            xpSlider.wholeNumbers = false;
        }
    }

    private void Update()
    {
        if (levelSystem == null)
        {
            levelSystem = PlayerRefs.I != null ? PlayerRefs.I.LevelSystem : null;
            if (levelSystem == null) return;
        }

        bool maxed = levelSystem.CurrentLevel >= PlayerLevelSystem.MaxLevel;

        if (xpSlider != null)
            xpSlider.value = maxed ? 1f : levelSystem.XPFraction;

        int level = levelSystem.CurrentLevel;
        int xp = Mathf.FloorToInt(levelSystem.CurrentXP);
        int toNext = Mathf.FloorToInt(levelSystem.XPToNextLevel);
        if (level == shownLevel && xp == shownXP && toNext == shownXPToNext) return;
        shownLevel = level; shownXP = xp; shownXPToNext = toNext;

        if (levelText != null)
            levelText.text = maxed ? "MAX" : $"Lv {level}";

        if (xpText != null)
            xpText.text = maxed ? "MAX LEVEL" : $"{xp} / {toNext} XP";
    }
}