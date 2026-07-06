using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 100;

    [Header("UI")]
    public Slider hpSlider;
    public TMP_Text hpText;

    [Header("Damage Cooldown")]
    public float invincibilityTime = 1f;

    [Header("Hit Flash")]
    public Renderer playerRenderer;
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;

    [Header("Invincibility Flash Color")]
    public Color dashInvincColor = new Color(1f, 0.9f, 0.1f);

    [Header("Camera Shake on Damage")]
    public float shakeMagnitudeBase = 0.12f;
    public float shakeMagnitudePer10Dmg = 0.08f;
    public float shakeDurationOnHit = 0.15f;
    public float shakeMagnitudeOnDeath = 0.45f;
    public float shakeDurationOnDeath = 0.4f;

    [Header("Shield")]
    public GameObject shieldVFX;

    [Header("Extra Life — Resurrection")]
    public GameObject resurrectionVFX;
    public float resurrectionSlowDuration = 1.5f;
    public float resurrectionMinTimeScale = 0.05f;
    public float resurrectionCameraZoom = 3f;
    public float resurrectionZoomSpeed = 2f;

    // ================================================================
    //  STATIC STATE  (persists across scenes)
    // ================================================================

    public static bool HasExtraLife = false;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHP;
    private bool isInvincible;
    private int shieldBlocks = 0;

    private bool isDashInvincible;
    private Coroutine dashInvincCoroutine;
    private Coroutine dashFlashCoroutine;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private ShieldAbility shieldAbility;
    private Color originalColor;

    public int CurrentHP => currentHP;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        playerStats = GetComponent<PlayerStats>();
        shieldAbility = GetComponent<ShieldAbility>();

        // Spawned-prefab support: pull scene HUD refs not wired on the prefab
        if (HUDRefs.I != null)
        {
            if (hpSlider == null) hpSlider = HUDRefs.I.hpSlider;
            if (hpText == null) hpText = HUDRefs.I.hpText;
        }

        if (playerStats != null) maxHP = playerStats.maxHP;
        currentHP = maxHP;

        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        RefreshShieldVFX();
        UpdateHealthUI();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>
    /// Directional damage (enemy contact, projectiles). Blockable by the
    /// Warrior's ShieldAbility when the source is within the block arc.
    /// Non-directional hazards (spikes, lava, curse costs) use TakeDamage(int)
    /// and cannot be blocked.
    /// </summary>
    public void TakeDamage(int amount, Vector3 sourcePosition, GameObject source = null)
    {
        if (shieldAbility != null && shieldAbility.IsBlockingFrom(sourcePosition))
        {
            shieldAbility.OnBlockedHit(source);   // shield counter: damage + knockback
            return;   // blocked — no damage, no i-frames
        }

        TakeDamage(amount);
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || isDashInvincible) return;

        // Shield absorbs a hit
        if (shieldBlocks > 0)
        {
            shieldBlocks--;
            RefreshShieldVFX();
            StartCoroutine(InvincibilityTimer());
            return;
        }

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);
        playerStats?.SyncHPMirror(currentHP);

        playerStats?.RecordDamageTaken(amount);
        upgradeManager?.DamageTaken(amount);

        CameraFollow cam = PlayerRefs.CamFollow;   // cached — this runs on every hit
        if (currentHP > 0)
        {
            float mag = shakeMagnitudeBase + shakeMagnitudePer10Dmg * (amount / 10f);
            cam?.Shake(mag, shakeDurationOnHit);
        }
        else
        {
            cam?.Shake(shakeMagnitudeOnDeath, shakeDurationOnDeath);
        }

        UpdateHealthUI();
        StartCoroutine(InvincibilityTimer());

        if (playerRenderer != null)
            StartCoroutine(HitFlash());

        if (currentHP <= 0)
            Die();
    }

    public void AddShield(int blocks)
    {
        shieldBlocks += blocks;
        RefreshShieldVFX();
    }

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
        playerStats?.SyncHPMirror(currentHP);
        playerStats?.RecordHPRestored(amount);
        UpdateHealthUI();
    }

    public void SetHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        playerStats?.SyncHPMirror(currentHP);
        UpdateHealthUI();
    }

    public void IncreaseMaxHP(int amount)
    {
        maxHP += amount;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (playerStats != null) playerStats.maxHP = maxHP;
        UpdateHealthUI();
    }

    // ================================================================
    //  DEATH / EXTRA LIFE
    // ================================================================

    private void Die()
    {
        if (HasExtraLife)
        {
            HasExtraLife = false;
            StartCoroutine(ResurrectionSequence());
            return;
        }

        upgradeManager?.PlayerDied();
        Achievements.TriggerEvent("player_died");   // e.g. unlocks the Warrior class

        // Run summary (records the run + back-to-menu). Fallback: old fade-and-reload
        // for scenes without a RunSummaryUI (e.g. testing).
        if (RunSummaryUI.TryShow(false)) return;
        Object.FindFirstObjectByType<GameStartSequence>()?.PlayerDied();
    }

    private IEnumerator ResurrectionSequence()
    {
        // Disable player control during sequence
        GetComponent<PlayerStateController>()?.DisableControl();

        Camera mainCam = Camera.main;
        CameraFollow cam = mainCam?.GetComponent<CameraFollow>();
        float originalZ = mainCam != null ? mainCam.transform.position.z : 0f;

        // Ease time to near-zero
        float elapsed = 0f;
        while (elapsed < resurrectionSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / resurrectionSlowDuration);
            float curve = 1f - Mathf.Pow(1f - t, 3f);
            GameTime.SetScale(Mathf.Lerp(1f, resurrectionMinTimeScale, curve));

            // Ease camera Z in (zoom)
            if (mainCam != null)
            {
                float targetZ = originalZ + resurrectionCameraZoom;
                Vector3 pos = mainCam.transform.position;
                pos.z = Mathf.MoveTowards(pos.z, targetZ, resurrectionZoomSpeed * Time.unscaledDeltaTime);
                mainCam.transform.position = pos;
            }

            yield return null;
        }

        // Restore player HP
        currentHP = maxHP;
        UpdateHealthUI();

        // Play resurrection VFX
        if (resurrectionVFX != null)
            Instantiate(resurrectionVFX, transform.position, Quaternion.identity);

        // Brief hold at slow-mo
        yield return new WaitForSecondsRealtime(0.3f);

        // Ease time and zoom back to normal
        elapsed = 0f;
        while (elapsed < resurrectionSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / resurrectionSlowDuration);
            GameTime.SetScale(Mathf.Lerp(resurrectionMinTimeScale, 1f, t));

            if (mainCam != null)
            {
                Vector3 pos = mainCam.transform.position;
                pos.z = Mathf.MoveTowards(pos.z, originalZ, resurrectionZoomSpeed * Time.unscaledDeltaTime);
                mainCam.transform.position = pos;
            }

            yield return null;
        }

        GameTime.Resume();

        if (mainCam != null)
        {
            Vector3 pos = mainCam.transform.position;
            pos.z = originalZ;
            mainCam.transform.position = pos;
        }

        GetComponent<PlayerStateController>()?.EnableControl();
        StartCoroutine(InvincibilityTimer());
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private void RefreshShieldVFX()
    {
        if (shieldVFX != null)
            shieldVFX.SetActive(shieldBlocks > 0);
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
