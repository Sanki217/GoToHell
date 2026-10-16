using UnityEngine;

/// <summary>
/// A pact = the RMB skill a character takes into a run (Dash, Shield, …).
/// Not tied to any class — any character can take any unlocked pact, and
/// pick a different one each run (Pact Select screen before the run).
///
/// The skill itself is a component on every class's player prefab, found by
/// type name (skillComponentName). RunConfigApplier enables the chosen one
/// and disables the others. Adding a pact:
///   1. Write the skill component (e.g. GrappleAbility) and add it to one
///      class prefab.
///   2. Tools → Go To Hell → Characters → Sync Pact Skills Into Class Prefabs
///      (copies it, disabled, into every other class prefab).
///   3. Create this asset in Assets/Resources/Pacts/.
///
/// Unlocking: unlockedByDefault, or an AchievementDefinition's pactToUnlock.
///
/// Create via: Assets → Create → GoToHell → Pact
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

    [Tooltip("Available without unlocking (starting pacts).")]
    public bool unlockedByDefault;

    [Header("RMB Skill")]
    [Tooltip("Exact component type name on the player prefab, e.g. \"DashAbility\".")]
    public string skillComponentName;

    [Tooltip("Lower = earlier in the pact list.")]
    public int sortOrder;

    public bool IsUnlocked => unlockedByDefault || SaveManager.IsPactUnlocked(pactId);
}
