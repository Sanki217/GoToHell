using UnityEngine;

/// <summary>
/// A playable class: predefined starting stats, a 2D sprite, and an RMB skill.
/// The skill is referenced by component type name (e.g. "DashAbility") — the
/// player prefab carries every class-skill component, and RunConfigApplier
/// enables only the selected class's one. Matches the upgrade convention of
/// identity-by-type-name.
///
/// Create via: Assets → Create → GoToHell → Class
/// Store in: Resources/Classes/ (Collection + RunConfigApplier load from there)
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Class")]
public class ClassDefinition : ScriptableObject
{
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string classId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    [Tooltip("2D sprite shown in the creator carousel and Collection.")]
    public Sprite sprite;

    [Tooltip("Unlocked without any save-data entry (e.g. Rogue).")]
    public bool unlockedByDefault;

    [Header("Player Prefab")]
    [Tooltip("The class-specific player prefab spawned by PlayerSpawner at run start. " +
             "Carries ONLY this class's skill components.")]
    public GameObject playerPrefab;

    [Header("RMB Skill")]
    [Tooltip("Exact component type name on the player prefab, e.g. \"DashAbility\".")]
    public string skillComponentName;
    public string skillDisplayName;
    [TextArea(2, 4)]
    public string skillDescription;

    [Header("Starting Primary Stats")]
    public int agility;
    public int attackDamage;
    public int luck;
    public int psyche;
    public int health;
    public int size;
    public int cooldown;
}
