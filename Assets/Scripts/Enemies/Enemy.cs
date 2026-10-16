using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy core: health, status effects, death + drops. Knockback and wall
/// depenetration live in EnemyKnockback (auto-added at runtime — existing
/// prefabs need no changes; all tuning stays serialized here).
///
/// LIFECYCLE: Awake/Start/OnEnable/OnDisable are protected virtual — subclasses
/// (TrainingDummy, LevelExitTarget, future BossBase) override and call base.
///
/// DAMAGE: TakeDamage returns true when that hit killed the enemy. Hits on an
/// already-dead enemy are ignored, so two arrows landing in the same frame can
/// never drop double souls or count two kills.
///
/// MOVEMENT HOOKS for status effects / systems (no per-type special cases):
///   • SpeedMultiplier — every movement script multiplies its speed by this
///     (Freeze writes it through SetSlow/ClearSlow).
///   • SetBehavioursEnabled — sleeps/wakes every movement script (all of them
///     implement IKnockbackReceiver), used by SceneOptimizer.
///
/// Enemies self-register in EnemyRegistry (OnEnable/OnDisable) so systems
/// iterate the registry instead of scanning the scene.
///
/// DATA: assign an EnemyDefinition to drive health, damage, souls, speed and
/// per-layer scaling from one asset (balance sheet editable). Without one,
/// the fields below are used. Pact / meta enemy modifiers (RunModifiers)
/// apply either way, to combatants only.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Data (optional — overrides the values below)")]
    [Tooltip("Enemy type data. Empty = use this component's own fields.")]
    public EnemyDefinition definition;

    [Header("Health")]
    public int maxHealth = 3;

    [Header("Soul Drops")]
    public GameObject soulPrefab;
    public float energyRestoredOnDeath = 5f;
    public int minimumSouls = 1;
    public int maximumSouls = 3;

    [Header("Hit Flash")]
    [Tooltip("Body renderer to flash/tint. Empty = every renderer on the enemy.")]
    public Renderer enemyRenderer;
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.08f;

    [Header("Knockback")]
    public float knockbackDuration = 0.15f;

    [Header("Wall Safety")]
    [Tooltip("Layers considered solid walls. Used by the fallback knockback path " +
             "(enemies with no IKnockbackReceiver, e.g. training dummy) and the " +
             "post-knockback depenetration check.")]
    public LayerMask wallLayers;

    [Tooltip("Half-extents for the BoxCast / OverlapBox wall checks. " +
             "Should roughly match the enemy's visual half-size.")]
    public Vector3 wallCheckHalfExtents = new Vector3(0.4f, 0.4f, 0.1f);

    [Tooltip("Bounce damping for the fallback knockback path (0 = dead stop, 1 = perfect bounce).")]
    [Range(0f, 1f)]
    public float knockbackBounceDamping = 0.4f;

    [Tooltip("Run wall depenetration every physics frame instead of only after " +
             "knockback. Enable per-prefab only if this enemy gets stuck in walls.")]
    public bool alwaysDepenetrate = false;

    // ================================================================
    //  EVENTS
    // ================================================================

    /// <summary>Fired right before the enemy is destroyed. The Enemy reference is still valid inside the handler.</summary>
    public event System.Action<Enemy> OnDied;

    // ================================================================
    //  STATE
    // ================================================================

    protected int currentHealth;
    private bool isDead;
    private EnemyHealthBar healthBar;
    private EnemyKnockback knockback;
    private TintController tint;
    private IKnockbackReceiver[] receivers;
    private readonly Dictionary<object, float> slows = new Dictionary<object, float>();

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    /// <summary>Max health after definition, layer scaling and run modifiers.</summary>
    public int MaxHealth { get; private set; }

    /// <summary>Scale for everything this enemy deals (layer scaling × run modifiers). EnemyDamage / projectiles read it.</summary>
    public float DamageMultiplier { get; private set; } = 1f;

    public EnemyDefinition Definition => definition;

    // Effective rewards (definition or fields, layer-scaled)
    private int soulsMin, soulsMax;
    private float energyOnDeath;
    private float baseSpeed = 1f;

    /// <summary>
    /// False for objects that use the Enemy damage path but aren't real
    /// opponents (portal keystone, level-exit target). Auto-targeting, AI
    /// helpers and the enemy barrier skip non-combatants.
    /// </summary>
    public virtual bool IsCombatant => true;

    /// <summary>Movement speed scale from status effects (1 = normal). Movement scripts multiply by this.</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    /// <summary>Colour owner for flashes and status tints.</summary>
    public TintController Tint
    {
        get
        {
            if (tint == null)
            {
                tint = TintController.For(gameObject);
                if (enemyRenderer != null) tint.SetTargets(enemyRenderer);
            }
            return tint;
        }
    }

    // ================================================================
    //  LIFECYCLE
    // ================================================================

    protected virtual void Awake()
    {
        ResolveStats();
        // Health is valid before Start so a same-frame hit never sees 0 HP
        currentHealth = MaxHealth;
        receivers = GetComponents<IKnockbackReceiver>();
        ConfigureBody(GetComponent<Rigidbody>());
    }

    protected virtual void Start()
    {
        healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar != null) healthBar.Initialize(MaxHealth, currentHealth);

        // Auto-default wallLayers to the "Wall" layer if the Inspector left it empty.
        if (wallLayers == 0)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
                wallLayers = 1 << wallLayer;
        }

        // Knockback/depenetration component — added at runtime so existing
        // prefabs keep working without manual edits. All tuning stays here.
        if (!TryGetComponent(out knockback))
            knockback = gameObject.AddComponent<EnemyKnockback>();
        knockback.Configure(
            wallLayers, wallCheckHalfExtents, knockbackBounceDamping,
            knockbackDuration, alwaysDepenetrate,
            receivers,
            GetComponentsInChildren<Collider>());
    }

    protected virtual void OnEnable() => EnemyRegistry.Register(this);
    protected virtual void OnDisable() => EnemyRegistry.Unregister(this);

    /// <summary>
    /// Physics setup for a code-driven 2.5D enemy: no tumbling from player
    /// collisions and no drift off the gameplay plane. Override for physics-
    /// driven enemies (TrainingDummy swings on a hinge).
    /// </summary>
    /// <summary>
    /// Effective stats from the definition (or fields), layer scaling and
    /// pact / meta modifiers. Non-combatants (portal keys, exit targets) are
    /// never scaled.
    /// </summary>
    private void ResolveStats()
    {
        int layer = EnemyDefinition.CurrentLayer;
        bool scaled = IsCombatant;

        int hp = definition != null ? definition.maxHealth : maxHealth;
        float hpScale = 1f, dmgScale = 1f, soulScale = 1f;

        if (definition != null)
        {
            soulsMin = definition.minSouls;
            soulsMax = definition.maxSouls;
            energyOnDeath = definition.energyOnDeath;
            baseSpeed = definition.moveSpeedMultiplier;
            if (scaled)
            {
                hpScale = definition.HealthScale(layer);
                dmgScale = definition.DamageScale(layer);
                soulScale = definition.SoulScale(layer);
            }
        }
        else
        {
            soulsMin = minimumSouls;
            soulsMax = maximumSouls;
            energyOnDeath = energyRestoredOnDeath;
            baseSpeed = 1f;
        }

        if (scaled)
        {
            hpScale *= RunModifiers.EnemyHealthMult;
            dmgScale *= RunModifiers.EnemyDamageMult;
            baseSpeed *= RunModifiers.EnemySpeedMult;
        }

        MaxHealth = Mathf.Max(1, Mathf.RoundToInt(hp * hpScale));
        DamageMultiplier = dmgScale;
        soulsMin = Mathf.RoundToInt(soulsMin * soulScale);
        soulsMax = Mathf.Max(soulsMin, Mathf.RoundToInt(soulsMax * soulScale));
        RecomputeSpeed();
    }

    /// <summary>Contact damage after definition + scaling. <paramref name="fallback"/> = the EnemyDamage field.</summary>
    public int ScaleContactDamage(int fallback)
    {
        int baseDmg = definition != null ? definition.contactDamage : fallback;
        return baseDmg <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseDmg * DamageMultiplier));
    }

    /// <summary>Projectile damage after definition + scaling. <paramref name="fallback"/> = the projectile prefab's value.</summary>
    public int ScaleProjectileDamage(int fallback)
    {
        int baseDmg = definition != null && definition.projectileDamage > 0 ? definition.projectileDamage : fallback;
        return baseDmg <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseDmg * DamageMultiplier));
    }

    protected virtual void ConfigureBody(Rigidbody rb)
    {
        if (rb == null) return;
        rb.constraints |= RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public bool WillDie(int amount) => !isDead && currentHealth - amount <= 0;

    public bool TakeDamage(int amount)
    {
        return TakeDamage(amount, transform.position, Vector3.zero, 0f, false,
                          FloatingTextManager.HitType.Normal);
    }

    /// <summary>Applies one hit. Returns true if this hit killed the enemy.</summary>
    public virtual bool TakeDamage(int amount, Vector3 hitPosition,
                                   Vector3 knockbackDir, float knockbackForce,
                                   bool isCrit, FloatingTextManager.HitType hitType)
    {
        if (isDead) return false;

        if (TryGetComponent(out ShockEffect shock))
        {
            int bonus = Mathf.Max(0, Mathf.RoundToInt(shock.ConsumeShock(amount)));
            if (bonus > 0)
            {
                amount += bonus;
                hitType = FloatingTextManager.HitType.ShockConsume;
            }
        }

        currentHealth -= amount;

        FloatingTextManager.Show(amount, hitPosition,
            isCrit ? FloatingTextManager.HitType.Critical : hitType);

        if (healthBar != null) healthBar.NotifyDamage(Mathf.Max(0, currentHealth));

        if (knockbackForce > 0f && knockbackDir != Vector3.zero && knockback != null)
            knockback.ApplyKnockback(knockbackDir.normalized * knockbackForce);

        if (hitFlashDuration > 0f) Tint.Flash(hitFlashColor, hitFlashDuration);

        PlayerRefs refs = PlayerRefs.I;
        if (refs != null && refs.Killstreak != null) refs.Killstreak.RegisterDamageDealt();

        if (currentHealth > 0) return false;
        Die();
        return true;
    }

    // ================================================================
    //  STATUS EFFECTS
    // ================================================================

    public void ApplyStatus(StatusType type, PlayerStats ps, PlayerUpgradeManager mgr)
    {
        if (isDead) return;
        switch (type)
        {
            case StatusType.Burn:   ApplyOrRefresh<BurnEffect>(ps, mgr); break;
            case StatusType.Freeze: ApplyOrRefresh<FreezeEffect>(ps, mgr); break;
            case StatusType.Holy:   ApplyOrRefresh<HolyEffect>(ps, mgr); break;
            case StatusType.Shock:  ApplyOrRefresh<ShockEffect>(ps, mgr); break;
        }
    }

    private void ApplyOrRefresh<T>(PlayerStats ps, PlayerUpgradeManager mgr) where T : StatusEffect
    {
        if (TryGetComponent(out T existing)) existing.Reapply(ps, mgr);
        else gameObject.AddComponent<T>().Initialize(ps, mgr);
    }

    /// <summary>Slow this enemy's movement. Several sources compose — the strongest slow wins.</summary>
    public void SetSlow(object owner, float multiplier)
    {
        if (owner == null) return;
        slows[owner] = Mathf.Clamp01(multiplier);
        RecomputeSpeed();
    }

    public void ClearSlow(object owner)
    {
        if (owner != null && slows.Remove(owner)) RecomputeSpeed();
    }

    private void RecomputeSpeed()
    {
        float m = 1f;
        foreach (float v in slows.Values) if (v < m) m = v;
        SpeedMultiplier = baseSpeed * m;
    }

    // ================================================================
    //  SLEEP (SceneOptimizer)
    // ================================================================

    /// <summary>Enable/disable every movement/AI script on this enemy.</summary>
    public void SetBehavioursEnabled(bool value)
    {
        if (receivers == null) return;
        foreach (IKnockbackReceiver r in receivers)
            if (r is Behaviour b && b != null) b.enabled = value;
    }

    // ================================================================
    //  DEATH
    // ================================================================

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        Soul.Burst(soulPrefab, transform.position, Random.Range(soulsMin, soulsMax + 1),
                   -60f, 60f, 4f, 9f);

        CameraFollow cam = PlayerRefs.CamFollow;
        if (cam != null) cam.Shake(0.08f, 0.08f);

        PlayerRefs refs = PlayerRefs.I;
        if (refs != null)
        {
            if (refs.Energy != null) refs.Energy.RestoreEnergy(energyOnDeath);
            if (refs.Upgrades != null) refs.Upgrades.EnemyKilled(gameObject);
            if (refs.Stats != null) refs.Stats.RecordEnemyKilled();
            if (refs.Killstreak != null) refs.Killstreak.RegisterKill();
        }

        // Fire event BEFORE destroying — handlers may still want to read this enemy's transform.
        OnDied?.Invoke(this);

        // Leave the registry immediately so same-frame queries skip the corpse
        EnemyRegistry.Unregister(this);
        Destroy(gameObject);
    }
}
