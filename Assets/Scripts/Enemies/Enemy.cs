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

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHealth;
    private EnemyHealthBar healthBar;

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

        GameObject player = GameObject.FindWithTag("Player");
        player?.GetComponent<KillStreak>()?.RegisterDamageDealt();

        if (currentHealth <= 0)
            Die();
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
        var hPatrol    = GetComponent<EnemyPatrolHorizontal>();
        var vPatrol    = GetComponent<EnemyPatrolVertical>();
        var shooter    = GetComponent<EnemyShooter>();
        var wallJumper = GetComponent<EnemyWallJumper>();

        if (hPatrol    != null) hPatrol.ReceiveKnockback(impulse, knockbackDuration);
        if (vPatrol    != null) vPatrol.ReceiveKnockback(impulse, knockbackDuration);
        if (shooter    != null) shooter.ReceiveKnockback(impulse, knockbackDuration);
        if (wallJumper != null) wallJumper.ReceiveKnockback(impulse, knockbackDuration);

        if (hPatrol == null && vPatrol == null && shooter == null && wallJumper == null)
        {
            float elapsed = 0f;
            while (elapsed < knockbackDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / knockbackDuration;
                Vector3 newPos = transform.position + impulse * (1f - t) * Time.deltaTime;
                newPos.z = 0f;
                transform.position = newPos;
                yield return null;
            }
        }
        else yield break;
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
            player.GetComponent<KillStreak>()?.RegisterKill();
        }

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