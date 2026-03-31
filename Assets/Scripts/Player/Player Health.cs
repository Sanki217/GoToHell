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

    [Header("Invincibility Flash Color")]
    public Color dashInvincColor = new Color(1f, 0.9f, 0.1f); // yellow-gold

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHP;
    private bool isInvincible;

    private bool isDashInvincible = false;
    private Coroutine dashInvincCoroutine;
    private Coroutine dashFlashCoroutine;

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

        if (playerStats != null) maxHP = playerStats.maxHP;

        currentHP = maxHP;

        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        UpdateHealthUI();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void TakeDamage(int amount)
    {
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
    /// Grants invincibility for the specified duration.
    /// Player turns yellow while immune.
    /// Used by DashAbility and Immunity upgrade.
    /// </summary>
    public void StartDashInvincibility(float duration)
    {
        if (dashInvincCoroutine != null) StopCoroutine(dashInvincCoroutine);
        if (dashFlashCoroutine != null) StopCoroutine(dashFlashCoroutine);

        dashInvincCoroutine = StartCoroutine(DashInvincibilityTimer(duration));
        dashFlashCoroutine = StartCoroutine(DashInvincibilityFlash(duration));
    }

    public void RestoreHP(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        playerStats?.RecordHPRestored(amount);
        UpdateHealthUI();
    }

    public void SetHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        UpdateHealthUI();
    }

    /// <summary>
    /// Increase max HP. Always heals the player by the same amount.
    /// Also syncs PlayerStats.maxHP so the Inspector shows the correct value.
    /// </summary>
    public void IncreaseMaxHP(int amount)
    {
        maxHP += amount;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (playerStats != null) playerStats.maxHP = maxHP;
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

    private IEnumerator DashInvincibilityFlash(float duration)
    {
        if (playerRenderer == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Pulse between yellow and white
            float t = Mathf.PingPong(elapsed * 6f, 1f);
            playerRenderer.material.color = Color.Lerp(dashInvincColor, Color.white, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerRenderer.material.color = originalColor;
        dashFlashCoroutine = null;
    }

    private IEnumerator HitFlash()
    {
        if (playerRenderer == null) yield break;
        playerRenderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        playerRenderer.material.color = originalColor;
    }
}