using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A playable class — chosen once at character creation, kept forever.
/// The class is WHO you are, the weapon is WHAT you hold:
///
///   LMB        → weapon (equipped in the town, any class can use any weapon)
///   Shift+LMB  → kick (universal, PlayerKick)
///   F          → this class's ACTIVE skill (a ClassSkill component)
///   passive    → this class's PASSIVE (RunEffects applied when the player spawns)
///   RMB        → pact, chosen before each run
///   run upgrades with PlayerUpgrade.requiredClass = this classId only drop for this class
///
/// RunConfigApplier enables this class's skill component and applies its
/// passive effects; every other class's skill stays disabled.
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
    [Tooltip("Spawned by PlayerSpawner (town and runs). Must carry every weapon, the kick, every class skill " +
             "and every pact skill (Tools → Go To Hell → Characters → Sync Kit Into Class Prefabs).")]
    public GameObject playerPrefab;

    [Header("Starting Weapon")]
    [Tooltip("Equipped on a new character. Swap weapons at the weapon rack in the town.")]
    [FormerlySerializedAs("weapon")]
    public WeaponDefinition startingWeapon;

    [Header("Active Skill (F)")]
    [Tooltip("Component type name of this class's ClassSkill (e.g. \"WarCrySkill\").")]
    public string activeSkillComponentName;
    public string activeSkillName;
    [TextArea(2, 4)]
    public string activeSkillDescription;

    [Header("Passive")]
    public string passiveName;
    [TextArea(2, 4)]
    public string passiveDescription;
    [Tooltip("What the passive does — applied every time the player spawns (town and every layer).")]
    [SerializeReference, SubclassSelector]
    public List<RunEffect> passiveEffects = new List<RunEffect>();

    public bool IsUnlocked => unlockedByDefault || SaveManager.IsClassUnlocked(classId);

    /// <summary>Applies the passive to a freshly spawned player.</summary>
    public void ApplyPassive(GameObject player, bool isRunStart)
    {
        if (player == null || passiveEffects == null) return;
        var ctx = new RunEffectContext(player, 1, isRunStart);
        foreach (RunEffect e in passiveEffects)
        {
            if (e == null) continue;
            try { e.Apply(ctx); }
            catch (System.Exception ex) { Debug.LogException(ex, this); }
        }
    }
}
