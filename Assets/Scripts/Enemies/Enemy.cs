using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    // ================================================================
    //  INSPECTOR
    // ================================================================

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

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHealth;
    private Rigidbody rb;

    private HorizontalMovement horizontalMovement;
    private VerticalMovement verticalMovement;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        horizontalMovement = GetComponent<HorizontalMovement>();
        verticalMovement = GetComponent<VerticalMovement>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation |
                             RigidbodyConstraints.FreezePositionZ;
        }

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public bool WillDie(int amount) => currentHealth - amount <= 0;

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero, 0f, false,
                   FloatingTextManager.HitType.Normal);
    }

    public void TakeDamage(int amount, Vector3 hitPosition,
                           Vector3 knockbackDir, float knockbackForce,
                           bool isCrit,
                           FloatingTextManager.HitType hitType)
    {
        // --- Shock: consume before applying damage, boost amount ---
        ShockEffect shock = GetComponent<ShockEffect>();
        if (shock != null)
        {
            float bonusDmg = shock.ConsumeShock(amount);
            amount += Mathf.Max(0, Mathf.RoundToInt(bonusDmg));
            hitType = FloatingTextManager.HitType.ShockConsume;
        }

        currentHealth -= amount;

        FloatingTextManager.Show(
            amount,
            hitPosition,
            isCrit ? FloatingTextManager.HitType.Critical : hitType
        );

        if (knockbackForce > 0f && knockbackDir != Vector3.zero)
            StartCoroutine(ApplyKnockback(knockbackDir.normalized * knockbackForce));

        if (enemyRenderer != null)
            StartCoroutine(HitFlash());

        if (currentHealth <= 0)
            Die();
    }

    // ================================================================
    //  STATUS EFFECTS
    // ================================================================

    /// <summary>
    /// Apply a status effect to this enemy.
    /// If the status is already active, refreshes its duration instead of stacking.
    /// </summary>
    public void ApplyStatus(StatusType type, PlayerStats playerStats,
                             PlayerUpgradeManager upgradeManager)
    {
        switch (type)
        {
            case StatusType.Burn: ApplyOrRefresh<BurnEffect>(playerStats, upgradeManager); break;
            case StatusType.Freeze: ApplyOrRefresh<FreezeEffect>(playerStats, upgradeManager); break;
            case StatusType.Holy: ApplyOrRefresh<HolyEffect>(playerStats, upgradeManager); break;
            case StatusType.Shock: ApplyOrRefresh<ShockEffect>(playerStats, upgradeManager); break;
        }
    }

    private void ApplyOrRefresh<T>(PlayerStats ps, PlayerUpgradeManager mgr)
        where T : StatusEffect
    {
        T existing = GetComponent<T>();
        if (existing != null)
            existing.Initialize(0f, ps, mgr); // re-initialise = refresh
        else
        {
            T effect = gameObject.AddComponent<T>();
            effect.Initialize(0f, ps, mgr);
        }
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private IEnumerator ApplyKnockback(Vector3 impulse)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            Vector3 frameMove = impulse * (1f - t) * Time.deltaTime;
            Vector3 newPos = transform.position + frameMove;
            newPos.z = transform.position.z;

            if (horizontalMovement != null && horizontalMovement.useBounds)
                newPos.x = Mathf.Clamp(newPos.x, horizontalMovement.minX, horizontalMovement.maxX);
            if (verticalMovement != null && verticalMovement.useBounds)
                newPos.y = Mathf.Clamp(newPos.y, verticalMovement.minY, verticalMovement.maxY);

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

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.GetComponent<PlayerEnergy>()?.RestoreEnergy(energyRestoredOnDeath);
            player.GetComponent<PlayerUpgradeManager>()?.EnemyKilled(gameObject);
            player.GetComponent<PlayerStats>()?.RecordEnemyKilled();
        }

        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        if (enemyRenderer == null) yield break;
        Color original = enemyRenderer.material.color;
        enemyRenderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        enemyRenderer.material.color = original;
    }
}