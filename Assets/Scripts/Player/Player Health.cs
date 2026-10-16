using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Player HP, i-frames, shield charges, death and the extra-life resurrection.
///
/// MAX HP has ONE source of truth: PlayerStats.maxHP (derived from the Health
/// stat + flat bonuses). This component listens to PlayerStats.OnStatsChanged
/// and, when max HP rises, heals by the same amount — so Health upgrades,
/// class presets and shop items all behave the same way. (Previously a Health
/// stat upgrade was overwritten back to the old max.)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("HP Settings")]
    [Tooltip("Fallback max HP when there is no PlayerStats on the player.")]
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
    //  STATIC STATE  (persists across scenes — reset by RunManager)
    // ================================================================

    public static bool HasExtraLife = false;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private int currentHP;
    private bool initialized;
    private float invincibleUntil = -1f;
    private float dashInvincibleUntil = -1f;
    private int shieldBlocks = 0;
    private bool isDead;
    private bool isResurrecting;

    private PlayerUpgradeManager upgradeManager;
    private PlayerStats playerStats;
    private ShieldAbility shieldAbility;
    private PlayerStateController stateCtrl;
    private TintController tint;

    public int CurrentHP => currentHP;
    public int MaxHP => playerStats != null ? playerStats.maxHP : maxHP;
    public bool IsDead => isDead;
    public bool IsInvincible => isResurrecting || Time.time < invincibleUntil || Time.time < dashInvincibleUntil;

    // ================================================================
    //  INIT
    // ================================================================

    private void Awake()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        playerStats = GetComponent<PlayerStats>();
        shieldAbility = GetComponent<ShieldAbility>();
        stateCtrl = GetComponent<PlayerStateController>();

        if (playerStats != null) playerStats.OnStatsChanged += OnStatsChanged;
    }

    private void OnDestroy()
    {
        if (playerStats != null) playerStats.OnStatsChanged -= OnStatsChanged;
    }

    private void Start()
    {
        // Spawned-prefab support: pull scene HUD refs not wired on the prefab
        if (HUDRefs.I != null)
        {
            if (hpSlider == null) hpSlider = HUDRefs.I.hpSlider;
            if (hpText == null) hpText = HUDRefs.I.hpText;
        }

        tint = TintController.For(gameObject);
        if (playerRenderer != null) tint.SetTargets(playerRenderer);

        SyncMaxHP();
        currentHP = maxHP;
        initialized = true;
        if (playerStats != null) playerStats.SyncHPMirror(currentHP);

        RefreshShieldVFX();
        UpdateHealthUI();
    }

    /// <summary>Max HP changed (Health stat, class preset, items): heal by any increase.</summary>
    private void OnStatsChanged()
    {
        int oldMax = maxHP;
        SyncMaxHP();
        if (!initialized) return;

        int delta = maxHP - oldMax;
        currentHP = delta > 0 ? currentHP + delta : Mathf.Min(currentHP, maxHP);
        playerStats.SyncHPMirror(currentHP);
        UpdateHealthUI();
    }

    private void SyncMaxHP()
    {
        if (playerStats != null) maxHP = Mathf.Max(1, playerStats.maxHP);
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
        if (isDead || isResurrecting) return;

        if (shieldAbility != null && shieldAbility.isActiveAndEnabled
            && shieldAbility.IsBlockingFrom(sourcePosition))
        {
            shieldAbility.OnBlockedHit(source);   // shield counter: damage + knockback
            return;   // blocked — no damage, no i-frames
        }

        TakeDamage(amount);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || isDead || IsInvincible) return;

        // Pacts / meta upgrades (never rounds a real hit down to zero)
        amount = Mathf.Max(1, Mathf.RoundToInt(amount * RunModifiers.DamageTakenMult));

        // Shield absorbs a hit
        if (shieldBlocks > 0)
        {
            shieldBlocks--;
            RefreshShieldVFX();
            invincibleUntil = Time.time + invincibilityTime;
            return;
        }

        currentHP = Mathf.Max(currentHP - amount, 0);
        if (playerStats != null)
        {
            playerStats.SyncHPMirror(currentHP);
            playerStats.RecordDamageTaken(amount);
        }
        if (upgradeManager != null) upgradeManager.DamageTaken(amount);
        HitStopManager.NotifyPlayerHurt(amount, maxHP);

        CameraFollow cam = PlayerRefs.CamFollow;   // cached — this runs on every hit
        if (cam != null)
        {
            if (currentHP > 0)
                cam.Shake(shakeMagnitudeBase + shakeMagnitudePer10Dmg * (amount / 10f), shakeDurationOnHit);
            else
                cam.Shake(shakeMagnitudeOnDeath, shakeDurationOnDeath);
        }

        UpdateHealthUI();
        invincibleUntil = Time.time + invincibilityTime;
        if (tint != null && hitFlashDuration > 0f) tint.Flash(hitFlashColor, hitFlashDuration);

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
        dashInvincibleUntil = Mathf.Max(dashInvincibleUntil, Time.time + duration);
        if (tint != null) tint.Pulse(dashInvincColor, Color.white, 6f, duration);
    }

    public void RestoreHP(int amount)
    {
        if (isDead || amount <= 0) return;
        int before = currentHP;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (playerStats != null)
        {
            playerStats.SyncHPMirror(currentHP);
            playerStats.RecordHPRestored(currentHP - before);   // only what was actually healed
        }
        UpdateHealthUI();
    }

    public void SetHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        if (playerStats != null) playerStats.SyncHPMirror(currentHP);
        UpdateHealthUI();
    }

    /// <summary>
    /// Permanent flat max-HP bonus (heals by the same amount). Goes through
    /// PlayerStats so it survives every later stat recalculation.
    /// </summary>
    public void IncreaseMaxHP(int amount)
    {
        if (amount == 0) return;
        if (playerStats != null)
        {
            playerStats.AddBonusMaxHP(amount);   // OnStatsChanged heals + refreshes UI
            return;
        }

        maxHP += amount;
        currentHP = Mathf.Clamp(currentHP + Mathf.Max(0, amount), 0, maxHP);
        UpdateHealthUI();
    }

    // ================================================================
    //  DEATH / EXTRA LIFE
    // ================================================================

    private void Die()
    {
        if (isDead) return;

        if (HasExtraLife)
        {
            HasExtraLife = false;
            StartCoroutine(ResurrectionSequence());
            return;
        }

        isDead = true;
        if (upgradeManager != null) upgradeManager.PlayerDied();
        Achievements.TriggerEvent("player_died");   // e.g. unlocks the Warrior class

        // Run summary (records the run + back-to-menu). Fallback: record the run
        // and fade-and-reload for scenes without a RunSummaryUI (e.g. testing).
        if (RunSummaryUI.TryShow(false)) return;

        RunManager.CompleteRun(false);
        GameStartSequence seq = Object.FindFirstObjectByType<GameStartSequence>();
        if (seq != null) seq.PlayerDied();
    }

    private IEnumerator ResurrectionSequence()
    {
        isResurrecting = true;
        if (stateCtrl != null) stateCtrl.AddBlocker(this);

        Camera mainCam = PlayerRefs.Cam;
        float originalZ = mainCam != null ? mainCam.transform.position.z : 0f;

        // Ease time to near-zero
        float elapsed = 0f;
        while (elapsed < resurrectionSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / resurrectionSlowDuration);
            float curve = 1f - Mathf.Pow(1f - t, 3f);
            GameTime.SetSlowMo(this, Mathf.Lerp(1f, resurrectionMinTimeScale, curve));

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
        if (playerStats != null) playerStats.SyncHPMirror(currentHP);
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
            GameTime.SetSlowMo(this, Mathf.Lerp(resurrectionMinTimeScale, 1f, t));

            if (mainCam != null)
            {
                Vector3 pos = mainCam.transform.position;
                pos.z = Mathf.MoveTowards(pos.z, originalZ, resurrectionZoomSpeed * Time.unscaledDeltaTime);
                mainCam.transform.position = pos;
            }

            yield return null;
        }

        GameTime.ClearSlowMo(this);

        if (mainCam != null)
        {
            Vector3 pos = mainCam.transform.position;
            pos.z = originalZ;
            mainCam.transform.position = pos;
        }

        isResurrecting = false;
        if (stateCtrl != null) stateCtrl.RemoveBlocker(this);
        invincibleUntil = Time.time + invincibilityTime;
    }

    private void OnDisable()
    {
        // Interrupted resurrection must not leave time slowed
        GameTime.ClearSlowMo(this);
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
}
