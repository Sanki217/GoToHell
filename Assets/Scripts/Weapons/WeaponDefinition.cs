using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A weapon (LMB). Characters equip one at the weapon rack in the town and
/// keep it until they swap. Any class can use any weapon.
///
/// weaponComponentNames lists the player components that make up this weapon
/// (by type name). RunConfigApplier enables the equipped weapon's components
/// and disables every other weapon's.
///   Bow   → PlayerShooting, ArrowRegenerator
///   Sword → PlayerSlash
///
/// LEVELS: the town smith upgrades the equipped weapon level by level, paid in
/// Brimstone. Each level's effects (RunEffects, like meta upgrades) apply on
/// top of every level below it, every time the player spawns. Levels are per
/// character — a favourite weapon grows with the character who carries it.
///
/// Create via: Assets → Create → GoToHell → Weapon
/// Store in: Resources/Weapons/
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Weapon")]
public class WeaponDefinition : ScriptableObject
{
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string weaponId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    [Tooltip("2D sprite shown in the town and Collection.")]
    public Sprite sprite;

    [Tooltip("Unlocked without any save-data entry (e.g. Bow).")]
    public bool unlockedByDefault;

    [Header("Behaviour")]
    [Tooltip("Player component type names that form this weapon (e.g. \"PlayerShooting\").")]
    public string[] weaponComponentNames;

    [Header("Smith Levels (level 1 = first upgrade)")]
    public List<WeaponLevel> levels = new List<WeaponLevel>();

    [Serializable]
    public class WeaponLevel
    {
        [Tooltip("Shown at the smith, e.g. \"Arrows pierce one enemy\".")]
        public string description;
        [Tooltip("Brimstone cost of buying this level.")]
        public int cost = 50;
        [SerializeReference, SubclassSelector]
        public List<RunEffect> effects = new List<RunEffect>();
    }

    public int MaxLevel => levels != null ? levels.Count : 0;

    public bool IsUnlocked => unlockedByDefault || SaveManager.IsWeaponUnlocked(weaponId);

    /// <summary>Cost of going from <paramref name="currentLevel"/> to the next one, or −1 at max.</summary>
    public int NextLevelCost(int currentLevel) =>
        currentLevel >= 0 && currentLevel < MaxLevel ? levels[currentLevel].cost : -1;

    /// <summary>Applies levels 1..<paramref name="level"/> to a freshly spawned player.</summary>
    public void ApplyLevels(GameObject player, int level, bool isRunStart)
    {
        if (player == null || levels == null) return;
        int n = Mathf.Clamp(level, 0, levels.Count);
        for (int i = 0; i < n; i++)
        {
            WeaponLevel l = levels[i];
            if (l?.effects == null) continue;
            var ctx = new RunEffectContext(player, 1, isRunStart);
            foreach (RunEffect e in l.effects)
            {
                if (e == null) continue;
                try { e.Apply(ctx); }
                catch (Exception ex) { Debug.LogException(ex, this); }
            }
        }
    }
}
