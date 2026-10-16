using UnityEngine;

/// <summary>
/// A playable class = the character's weapon kit.
///   LMB       → weapon (WeaponDefinition: Rogue = Bow, Warrior = Sword)
///   Shift+LMB → a small alt attack (Rogue = kick for vases when out of arrows)
///   RMB       → NOT the class: chosen per run as a Pact (Dash, Shield…)
///
/// Characters pick a class at creation; stats are the same for every class
/// (all start at 1 + free points — see CharacterRoster).
/// RunConfigApplier enables this class's weapon/alt components on the player
/// and disables every other class's.
///
/// Create via: Assets → Create → GoToHell → Class
/// Store in:   Assets/Resources/Classes/
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Class")]
public class ClassDefinition : ScriptableObject
{
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string classId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    [Tooltip("2D sprite shown in character creation and the Collection.")]
    public Sprite sprite;

    [Tooltip("Selectable in character creation without unlocking.")]
    public bool unlockedByDefault;

    [Header("Player Prefab")]
    [Tooltip("Spawned by PlayerSpawner. Must carry this class's weapon components AND every pact skill " +
             "(Tools → Go To Hell → Characters → Sync Pact Skills Into Class Prefabs).")]
    public GameObject playerPrefab;

    [Header("LMB Weapon")]
    public WeaponDefinition weapon;

    [Header("Shift + LMB")]
    [Tooltip("Component type name of the alt attack (e.g. \"PlayerSlash\" in kick mode). Empty = none.")]
    public string altAttackComponentName;
    public string altAttackDisplayName;

    public bool IsUnlocked => unlockedByDefault || SaveManager.IsClassUnlocked(classId);

}
