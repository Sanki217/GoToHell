using UnityEngine;

/// <summary>
/// Data for one enemy TYPE — the numbers a designer balances — separate from
/// the prefab that holds its behaviour scripts and visuals.
///
///   Imp.asset         maxHealth 3, contactDamage 10, souls 1–3
///   Imp (Layer 5)     → same asset; healthPerLayer / damagePerLayer scale it
///   Armored Imp.asset → a second asset on a copy of the same prefab
///
/// HOW IT APPLIES (Enemy.Awake):
///   health  = maxHealth  × (1 + healthPerLayer × (layer−1)) × pact/meta
///   damage  = contact / projectile damage × (1 + damagePerLayer × (layer−1)) × pact/meta
///   souls   = min/max × (1 + soulsPerLayer × (layer−1))
///   speed   = moveSpeedMultiplier × pact/meta (via Enemy.SpeedMultiplier)
///
/// An Enemy WITHOUT a definition keeps using its own Inspector fields
/// (pact / meta enemy modifiers still apply), so migrating is optional and
/// per prefab. Tools → Go To Hell → Data → Create Enemy Definitions builds an
/// asset for every enemy prefab from its current values.
///
/// Behaviour-specific tuning (patrol path, jump arcs, fire rate) stays on the
/// behaviour scripts — this asset holds what every enemy shares.
///
/// Create via: Assets → Create → GoToHell → Enemy
/// Store in:   Assets/Data/Enemies/ (any folder works — prefabs reference it)
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Enemy")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable id (bestiary, achievements, spawn tables). Never change once shipped.")]
    public string enemyId;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Combat")]
    [Min(1)] public int maxHealth = 3;
    [Tooltip("Damage of the EnemyDamage contact trigger.")]
    [Min(0)] public int contactDamage = 10;
    [Tooltip("Damage of this enemy's projectiles. 0 = use the projectile prefab's own value.")]
    [Min(0)] public int projectileDamage;
    [Tooltip("Multiplies every movement script's speed (Enemy.SpeedMultiplier).")]
    [Min(0f)] public float moveSpeedMultiplier = 1f;

    [Header("Rewards")]
    [Min(0)] public int minSouls = 1;
    [Min(0)] public int maxSouls = 3;
    [Min(0f)] public float energyOnDeath = 5f;

    [Header("Scaling per Layer (0.25 = +25% per layer below Layer 1)")]
    [Min(0f)] public float healthPerLayer = 0.25f;
    [Min(0f)] public float damagePerLayer = 0.15f;
    [Min(0f)] public float soulsPerLayer = 0.10f;

    public static int CurrentLayer => RunConfig.I != null ? Mathf.Max(1, RunConfig.I.currentLayer) : 1;

    public float HealthScale(int layer) => 1f + healthPerLayer * (Mathf.Max(1, layer) - 1);
    public float DamageScale(int layer) => 1f + damagePerLayer * (Mathf.Max(1, layer) - 1);
    public float SoulScale(int layer)   => 1f + soulsPerLayer   * (Mathf.Max(1, layer) - 1);

    private void OnValidate()
    {
        if (maxSouls < minSouls) maxSouls = minSouls;
    }
}
