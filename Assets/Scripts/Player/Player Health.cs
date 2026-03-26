using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 100;

    [Header("UI — assign in Inspector")]
    public Slider hpSlider;
    public TMP_Text hpText;

    [Header("Damage Cooldown")]
    public float invincibilityTime = 1f;

    [Header("Hit Flash")]
    public Renderer playerRenderer;
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHP;
    private bool isInvincible;

    // Separate flag for dash invincibility — tracked independently
    private bool isDashInvincible = false;
    private Coroutine dashInvincCoroutine;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private Color originalColor;

    // ================================================================
    //  PUBLIC READ
    // ================================================================

    public int CurrentHP => currentHP;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        playerStats = GetComponent<PlayerStats>();

        if (playerStats != null)
            maxHP = playerStats.maxHP;

        currentHP = maxHP;

        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        UpdateHealthUI();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>Deal damage to the player.</summary>
    public void TakeDamage(int amount)
    {
        // Block during normal iframes or dash invincibility window
        if (isInvincible || isDashInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);

        playerStats?.RecordDamageTaken(amount);
        upgradeManager?.DamageTaken(amount);

        if (currentHP > 0)
            Camera.main?.GetComponent<CameraFollow>()?.Shake(0.15f, 0.15f);

        UpdateHealthUI();
        StartCoroutine(InvincibilityTimer());

        if (playerRenderer != null)
            StartCoroutine(HitFlash());

        if (currentHP <= 0)
            Die();
    }

    /// <summary>
    /// Called by DashAbility at the start of a dash.
    /// Grants invincibility for the specified duration
    /// (dash time + post-dash window from PlayerStats).
    /// </summary>
    public void StartDashInvincibility(float duration)
    {
        if (dashInvincCoroutine != null)
            StopCoroutine(dashInvincCoroutine);

        dashInvincCoroutine = StartCoroutine(DashInvincibilityTimer(duration));
    }

    /// <summary>Restore HP. Will not exceed maxHP.</summary>
    public void RestoreHP(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        playerStats?.RecordHPRestored(amount);
        UpdateHealthUI();
    }

    /// <summary>Instantly set HP to a specific value (used by Revive and Inspector).</summary>
    public void SetHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        UpdateHealthUI();
    }

    /// <summary>Increase max HP (from upgrades). Optionally also heals the difference.</summary>
    public void IncreaseMaxHP(int amount, bool healDifference = false)
    {
        maxHP += amount;
        if (playerStats != null) playerStats.maxHP = maxHP;
        if (healDifference) currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHealthUI();
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void Die()
    {
        Debug.Log("PLAYER DEAD!");
        upgradeManager?.PlayerDied();
        Object.FindFirstObjectByType<GameStartSequence>()?.PlayerDied();
    }

    private void UpdateHealthUI()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
        if (hpText != null)
            hpText.text = $"{currentHP} / {maxHP}";
    }

    private IEnumerator InvincibilityTimer()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private IEnumerator DashInvincibilityTimer(float duration)
    {
        isDashInvincible = true;
        yield return new WaitForSeconds(duration);
        isDashInvincible = false;
        dashInvincCoroutine = null;
    }

    private IEnumerator HitFlash()
    {
        if (playerRenderer == null) yield break;
        playerRenderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        playerRenderer.material.color = originalColor;
    }
}