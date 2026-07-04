using UnityEngine;

/// <summary>
/// A weapon the player can take on a run (LMB). Weapon XP, skill pools, and
/// between-run purchases come later (backlog #2).
///
/// weaponComponentNames lists the player components that make up this weapon
/// (by type name, matching the class-skill convention). RunConfigApplier
/// enables the selected weapon's components and disables every other weapon's.
///   Bow   → PlayerShooting, ArrowRegenerator
///   Sword → PlayerSlash
///
/// Create via: Assets → Create → GoToHell → Weapon
/// Store in: Resources/Weapons/ (Collection + RunConfigApplier load from there)
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Weapon")]
public class WeaponDefinition : ScriptableObject
{
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string weaponId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    [Tooltip("2D sprite shown in the creator carousel and Collection.")]
    public Sprite sprite;

    [Tooltip("Unlocked without any save-data entry (e.g. Bow).")]
    public bool unlockedByDefault;

    [Header("Behaviour")]
    [Tooltip("Player component type names that form this weapon (e.g. \"PlayerShooting\").")]
    public string[] weaponComponentNames;
}
