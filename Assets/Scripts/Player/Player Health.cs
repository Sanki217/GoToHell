using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    // ================================================================
    //  INSPECTOR
    // ================================================================

    [Header("HP Settings")]
    public int maxHP = 100;

    [Header("UI — assign in Inspector")]
    public Slider hpSlider;       // drag your HP slider here
    public TMP_Text hpText;         // optional: shows "75 / 100"

    [Header("Damage Cooldown")]
    public float invincibilityTime = 1f;

    [Header("Hit Flash")]
    public Renderer playerRenderer;         // drag the player's MeshRenderer / SpriteRenderer
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHP;
    private bool isInvincible;

    private DashAbility dashAbility;
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
        dashAbility = GetComponent<DashAbility>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        playerStats = GetComponent<PlayerStats>();

        // Use maxHP from PlayerStats if available, else use local field
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
        // Invincibility frames — ignore damage while dashing or in iframes
        if (isInvincible)
            return;
        if (dashAbility != null && dashAbility.isDashing)
            return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);

        // Record in stats
        playerStats?.RecordDamageTaken(amount);

        // Fire upgrade event
        upgradeManager?.DamageTaken(amount);

        // Camera shake on hit (but not on death)
        if (currentHP > 0)
            Camera.main?.GetComponent<CameraFollow>()?.Shake(0.15f, 0.15f);

        UpdateHealthUI();
        StartCoroutine(InvincibilityTimer());

        if (playerRenderer != null)
            StartCoroutine(HitFlash());

        if (currentHP <= 0)
            Die();
    }

    /// <summary>Restore HP. Will not exceed maxHP.</summary>
    public void RestoreHP(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        playerStats?.RecordHPRestored(amount);  // see note below
        UpdateHealthUI();
    }

    /// <summary>Instantly set HP to a specific value (used by Revive).</summary>
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

        if (healDifference)
            currentHP = Mathf.Min(currentHP + amount, maxHP);

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

    private IEnumerator HitFlash()
    {
        if (playerRenderer == null) yield break;
        playerRenderer.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        playerRenderer.material.color = originalColor;
    }
}
