using UnityEngine;

/// <summary>
/// A weapon the player can take on a run (LMB). Display data + stable id only —
/// weapon XP, skill pools, and between-run purchases come later (backlog #2).
///
/// Create via: Assets → Create → GoToHell → Weapon
/// Store in: Resources/Weapons/ (Collection loads from there)
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
}
