using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// A pact: a run modifier chosen in the main menu before a run. Usually a
/// trade — harder enemies for more souls, no healing for more damage — but
/// it can be anything RunModifierSet + RunEffects can express.
///
/// DATA-ONLY: a new pact is a new asset. Fill in Modifiers for numeric
/// changes; add Custom Effects (type dropdown) for logic.
///
/// HEAT is the pact's difficulty score. Active pacts' heat is summed, shown
/// in the menu, stored on the run record, and boosts meta-currency rewards
/// (MetaProgressionConfig.currencyPercentPerHeat).
///
/// Unlocking: unlockedByDefault, or an AchievementDefinition's pactToUnlock.
///
/// Create via: Assets → Create → GoToHell → Pact
/// Store in:   Assets/Resources/Pacts/
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Pact")]
public class PactDefinition : ScriptableObject
{
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string pactId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    public Sprite icon;

    [Tooltip("Unlocked without any save-data entry (the player's starting pact).")]
    public bool unlockedByDefault;

    [Header("Difficulty")]
    [Tooltip("Difficulty score. Summed across active pacts; raises meta-currency rewards.")]
    [Range(0, 10)] public int heat = 1;

    [Header("Modifiers")]
    public RunModifierSet modifiers = new RunModifierSet();

    [Header("Custom Effects")]
    [SerializeReference, SubclassSelector]
    public List<RunEffect> effects = new List<RunEffect>();

    public bool IsUnlocked => unlockedByDefault || SaveManager.IsPactUnlocked(pactId);

    /// <summary>Description + every modifier and effect, one per line.</summary>
    public string BuildSummary()
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(description)) sb.Append(description);

        string mods = modifiers != null ? modifiers.Describe() : "";
        if (mods.Length > 0) { if (sb.Length > 0) sb.Append("\n\n"); sb.Append(mods); }

        if (effects != null)
            foreach (RunEffect e in effects)
                if (e != null) { if (sb.Length > 0) sb.Append('\n'); sb.Append(e.Describe(1)); }

        if (heat > 0) sb.Append($"\n\n<color=#FF6A3D>Heat {heat}</color>");
        return sb.ToString();
    }
}
