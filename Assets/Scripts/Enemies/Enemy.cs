using UnityEngine;

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

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHealth;
    private Rigidbody rb;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Simple overload — used by existing code that just passes an int.
    /// No knockback, no crit, normal hit type.
    /// </summary>
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero, 0f, false,
                   FloatingTextManager.HitType.Normal);
    }

    /// <summary>
    /// Full overload — used by arrows, dash, status effects.
    /// </summary>
    public void TakeDamage(int amount, Vector3 hitPosition,
                           Vector3 knockbackDir, float knockbackForce,
                           bool isCrit,
                           FloatingTextManager.HitType hitType)
    {
        currentHealth -= amount;

        // Floating number
        FloatingTextManager.Show(
            amount,
            hitPosition,
            isCrit ? FloatingTextManager.HitType.Critical : hitType
        );

        // Knockback
        if (knockbackForce > 0f && rb != null && knockbackDir != Vector3.zero)
            rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode.Impulse);

        // Hit flash
        if (enemyRenderer != null)
            StartCoroutine(HitFlash());

        if (currentHealth <= 0)
            Die();
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

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

    private System.Collections.IEnumerator HitFlash()
    {
        if (enemyRenderer == null) yield break;
        Color original = enemyRenderer.material.color;
        enemyRenderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        enemyRenderer.material.color = original;
    }
}