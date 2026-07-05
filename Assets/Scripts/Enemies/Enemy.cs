using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy core: health, status effects, death + drops. Knockback and wall
/// depenetration live in EnemyKnockback (auto-added at runtime — existing
/// prefabs need no changes; all tuning stays serialized here).
///
/// Public API is stable: TakeDamage overloads, WillDie, OnDied, ApplyStatus.
/// Enemies self-register in EnemyRegistry (OnEnable/OnDisable) so systems
/// iterate the registry instead of scanning the scene.
///
/// Planned: BossBase subclass overriding the virtual TakeDamage.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;

    [Header("Soul Drops")]
    public GameObject soulPrefab;
    public float energyRestoredOnDeath = 5f;
    public int minimumSouls = 1;
    public int maximumSouls = 3;

    [Header("Hit Flash")]
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
    //  PRIVATE STATE
    // ================================================================

    private int currentHealth;
    private EnemyHealthBar healthBar;
    private EnemyKnockback knockback;

    // ================================================================
    //  INIT / REGISTRY
    // ================================================================

    private void Start()
    {
        currentHealth = maxHealth;
        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        healthBar = GetComponent<EnemyHealthBar>();
        healthBar?.Initialize(maxHealth, currentHealth);

        // Auto-default wallLayers to the "Wall" layer if the Inspector left it empty.
        if (wallLayers == 0)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
                wallLayers = 1 << wallLayer;
        }

        // Knockback/depenetration component — added at runtime so existing
        // prefabs keep working without manual edits. All tuning stays here.
        knockback = GetComponent<EnemyKnockback>();
        if (knockback == null) knockback = gameObject.AddComponent<EnemyKnockback>();
        knockback.Configure(
            wallLayers, wallCheckHalfExtents, knockbackBounceDamping,
            knockbackDuration, alwaysDepenetrate,
            GetComponents<IKnockbackReceiver>(),
            GetComponentsInChildren<Collider>());
    }

    private void OnEnable() => EnemyRegistry.Register(this);
    private void OnDisable() => EnemyRegistry.Unregister(this);

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public bool WillDie(int amount) => currentHealth - amount <= 0;

    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero, 0f, false,
                   FloatingTextManager.HitType.Normal);
    }

    public virtual void TakeDamage(int amount, Vector3 hitPosition,
                           Vector3 knockbackDir, float knockbackForce,
                           bool isCrit, FloatingTextManager.HitType hitType)
    {
        ShockEffect shock = GetComponent<ShockEffect>();
        if (shock != null)
        {
            float bonusDmg = shock.ConsumeShock(amount);
            amount += Mathf.Max(0, Mathf.RoundToInt(bonusDmg));
            hitType = FloatingTextManager.HitType.ShockConsume;
        }

        currentHealth -= amount;

        FloatingTextManager.Show(amount, hitPosition,
            isCrit ? FloatingTextManager.HitType.Critical : hitType);

        healthBar?.NotifyDamage(Mathf.Max(0, currentHealth));

        if (knockbackForce > 0f && knockbackDir != Vector3.zero)
            knockback?.ApplyKnockback(knockbackDir.normalized * knockbackForce);

        if (enemyRenderer != null)
            StartCoroutine(HitFlash());

        PlayerRefs.I?.Killstreak?.RegisterDamageDealt();

        if (currentHealth <= 0) Die();
    }

    // ================================================================
    //  STATUS EFFECTS
    // ================================================================

    public void ApplyStatus(StatusType type, PlayerStats ps, PlayerUpgradeManager mgr)
    {
        switch (type)
        {
            case StatusType.Burn: ApplyOrRefresh<BurnEffect>(ps, mgr); break;
            case StatusType.Freeze: ApplyOrRefresh<FreezeEffect>(ps, mgr); break;
            case StatusType.Holy: ApplyOrRefresh<HolyEffect>(ps, mgr); break;
            case StatusType.Shock: ApplyOrRefresh<ShockEffect>(ps, mgr); break;
        }
    }

    private void ApplyOrRefresh<T>(PlayerStats ps, PlayerUpgradeManager mgr)
        where T : StatusEffect
    {
        T existing = GetComponent<T>();
        if (existing != null) existing.Initialize(0f, ps, mgr);
        else gameObject.AddComponent<T>().Initialize(0f, ps, mgr);
    }

    // ================================================================
    //  DEATH
    // ================================================================

    private void Die()
    {
        int soulCount = Random.Range(minimumSouls, maximumSouls + 1);
        for (int i = 0; i < soulCount; i++)
        {
            if (!soulPrefab) break;
            GameObject s = Pool.Spawn(soulPrefab, transform.position, Quaternion.identity);
            Soul soul = s.GetComponent<Soul>();
            if (soul != null)
            {
                float angle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                soul.Initialize(dir, Random.Range(4f, 9f));
            }
        }

        PlayerRefs.CamFollow?.Shake(0.08f, 0.08f);

        var refs = PlayerRefs.I;
        if (refs != null)
        {
            refs.Energy?.RestoreEnergy(energyRestoredOnDeath);
            refs.Upgrades?.EnemyKilled(gameObject);
            refs.Stats?.RecordEnemyKilled();
            refs.Killstreak?.RegisterKill();
        }

        // Fire event BEFORE destroying — handlers may still want to read this enemy's transform.
        OnDied?.Invoke(this);

        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        if (enemyRenderer == null) yield break;
        Color original = enemyRenderer.material.color;
        enemyRenderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (this != null) enemyRenderer.material.color = original;
    }
}
