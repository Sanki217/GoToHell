using UnityEngine;
using System.Collections;

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
             "per-frame depenetration check.")]
    public LayerMask wallLayers;

    [Tooltip("Half-extents for the BoxCast / OverlapBox wall checks. " +
             "Should roughly match the enemy's visual half-size.")]
    public Vector3 wallCheckHalfExtents = new Vector3(0.4f, 0.4f, 0.1f);

    [Tooltip("Bounce damping for the fallback knockback path (0 = dead stop, 1 = perfect bounce).")]
    [Range(0f, 1f)]
    public float knockbackBounceDamping = 0.4f;

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
    private IKnockbackReceiver[] knockbackReceivers;
    private Collider[] selfColliders;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        currentHealth = maxHealth;
        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        healthBar = GetComponent<EnemyHealthBar>();
        healthBar?.Initialize(maxHealth, currentHealth);

        // Cache all knockback receivers on this enemy (patrol scripts, shooters, wall jumpers, etc.)
        knockbackReceivers = GetComponents<IKnockbackReceiver>();
        selfColliders = GetComponentsInChildren<Collider>();

        // Auto-default wallLayers to the "Wall" layer if the Inspector left it empty.
        // Everything in this project is on the Wall layer, so this makes wall-bounce
        // and depenetration "just work" without per-prefab setup.
        if (wallLayers == 0)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
                wallLayers = 1 << wallLayer;
        }
    }

    // ================================================================
    //  WALL DEPENETRATION — runs every physics frame
    // ================================================================

    private static readonly Collider[] depenBuffer = new Collider[8];

    private void FixedUpdate()
    {
        if (wallLayers == 0) return;

        // Check if we overlap any wall right now and push out
        int count = Physics.OverlapBoxNonAlloc(
            transform.position, wallCheckHalfExtents, depenBuffer,
            Quaternion.identity, wallLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider wallCol = depenBuffer[i];
            // Skip our own colliders
            bool isSelf = false;
            if (selfColliders != null)
                foreach (var sc in selfColliders)
                    if (sc == wallCol) { isSelf = true; break; }
            if (isSelf) continue;

            // Compute penetration and push out
            // Use a small BoxCollider stand-in via ComputePenetration isn't available
            // without a collider pair — use a simpler approach: cast back toward
            // the wall from our position and snap to the surface.
            Vector3 toWall = wallCol.ClosestPoint(transform.position) - transform.position;
            toWall.z = 0f;
            if (toWall.sqrMagnitude < 0.001f)
            {
                // We're deep inside — pick a direction via bounds center
                toWall = transform.position - wallCol.bounds.center;
                toWall.z = 0f;
                if (toWall.sqrMagnitude < 0.001f) toWall = Vector3.up;
            }

            // If ClosestPoint is AT our position, we're inside the wall
            Vector3 closestOnWall = wallCol.ClosestPoint(transform.position);
            closestOnWall.z = 0f;
            Vector3 meFlat = new Vector3(transform.position.x, transform.position.y, 0f);
            float overlap = (closestOnWall - meFlat).magnitude;

            // Only push if closest point is very near (means we're overlapping)
            if (overlap < wallCheckHalfExtents.x * 1.1f)
            {
                Vector3 pushDir = (meFlat - closestOnWall).normalized;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.up;
                float pushAmount = wallCheckHalfExtents.x - overlap + 0.05f;
                if (pushAmount > 0f)
                {
                    Vector3 newPos = transform.position + pushDir * pushAmount;
                    newPos.z = 0f;
                    transform.position = newPos;
                }
            }
        }
    }

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
            StartCoroutine(ApplyKnockback(knockbackDir.normalized * knockbackForce));

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
    //  PRIVATE
    // ================================================================

    private IEnumerator ApplyKnockback(Vector3 impulse)
    {
        // Dispatch to every IKnockbackReceiver component on this enemy.
        // Patrol scripts, shooters, wall-jumpers, and future enemy types all implement the interface.
        bool handled = false;
        if (knockbackReceivers != null)
        {
            foreach (var r in knockbackReceivers)
            {
                if (r == null) continue;
                r.ReceiveKnockback(impulse, knockbackDuration);
                handled = true;
            }
        }

        if (handled) yield break;

        // Fallback for enemies with no movement scripts (e.g. training dummy):
        // ease-out the impulse directly on transform, with wall bounce.
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            Vector3 step = impulse * (1f - t) * Time.deltaTime;
            step.z = 0f;

            if (wallLayers != 0)
            {
                step = KnockbackBouncer.StepWithWallBounce(
                    step, transform.position, wallCheckHalfExtents,
                    knockbackBounceDamping, wallLayers, selfColliders);
            }

            Vector3 newPos = transform.position + step;
            newPos.z = 0f;
            transform.position = newPos;
            yield return null;
        }
    }

    private void Die()
    {
        int soulCount = Random.Range(minimumSouls, maximumSouls + 1);
        for (int i = 0; i < soulCount; i++)
        {
            if (!soulPrefab) break;
            GameObject s = Instantiate(soulPrefab, transform.position, Quaternion.identity);
            Soul soul = s.GetComponent<Soul>();
            if (soul != null)
            {
                float angle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                soul.Initialize(dir, Random.Range(4f, 9f));
            }
        }

        Camera.main?.GetComponent<CameraFollow>()?.Shake(0.08f, 0.08f);

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