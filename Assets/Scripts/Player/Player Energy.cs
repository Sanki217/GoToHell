using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings � defaults, overridden by PlayerStats at runtime")]
    public float maxEnergy = 100f;
    public float currentEnergy = 0f;

    [Header("Gain Rates")]
    public float energyPerVelocityUnit = 2f;
    public float wallSlideEnergyMultiplier = 2f;

    [Header("UI Reference")]
    public TMP_Text energyTMPText;

    [Header("Insufficient Energy Flash")]
    [Tooltip("Color the energy text flashes when an action fails due to low energy.")]
    public Color insufficientEnergyColor = new Color(1f, 0.15f, 0.15f);
    [Tooltip("Duration of the flash in seconds.")]
    public float insufficientFlashDuration = 0.35f;

    private PlayerMovement playerMovement;
    private PlayerStats playerStats;
    private LavaZone currentLavaZone;
    private Collider currentLavaCollider;
    private CapsuleCollider playerCapsule;
    private Color energyTextOriginalColor;
    private Coroutine insufficientFlashRoutine;

    // Always read max energy from PlayerStats when available
    private float MaxEnergy => playerStats != null ? playerStats.maxEnergy : maxEnergy;

    // Regen multiplier from PlayerStats � applied to ALL energy gained
    private float RegenMultiplier => playerStats != null ? playerStats.energyRegenMultiplier : 1f;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();
        playerCapsule = GetComponent<CapsuleCollider>();

        if (playerCapsule == null)
            Debug.LogWarning("PlayerEnergy: No CapsuleCollider found on player.");

        if (energyTMPText != null)
            energyTextOriginalColor = energyTMPText.color;
    }

    void Update()
    {
        HandleEnergyGain();
        UpdateEnergyUI();
    }

    private void HandleEnergyGain()
    {
        float yVelocity = playerMovement.GetVelocity().y;

        if (yVelocity < 0f)
        {
            float gain = Mathf.Abs(yVelocity) * energyPerVelocityUnit * RegenMultiplier;

            EnergySource source = EnergySource.Falling;

            if (playerMovement.isWallSliding)
            {
                gain *= wallSlideEnergyMultiplier;
                source = EnergySource.WallSlide;
            }

            float actualGain = Mathf.Min(gain * Time.deltaTime, MaxEnergy - currentEnergy);
            if (actualGain > 0f)
            {
                currentEnergy += actualGain;
                playerStats?.RecordEnergyGained(actualGain, source);
            }
        }

        if (currentLavaZone != null)
        {
            float lavaGain = currentLavaZone.DrainEnergy(Time.deltaTime) * RegenMultiplier;
            float actualGain = Mathf.Min(lavaGain, MaxEnergy - currentEnergy);
            if (actualGain > 0f)
            {
                currentEnergy += actualGain;
                playerStats?.RecordEnergyGained(actualGain, EnergySource.Lava);
            }
        }

        // Clamp to current max (handles MaxEnergy being lowered mid-run)
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, MaxEnergy);
    }

    /// <summary>Restore energy from kills or other external sources.</summary>
    public void RestoreEnergy(float amount)
    {
        float actualGain = Mathf.Min(amount * RegenMultiplier, MaxEnergy - currentEnergy);
        if (actualGain > 0f)
        {
            currentEnergy += actualGain;
            playerStats?.RecordEnergyGained(actualGain, EnergySource.Kill);
        }
        UpdateEnergyUI();
    }

    /// <summary>Spend energy. Returns true if successful.</summary>
    public bool SpendEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
    }

    /// <summary>Drain energy (hover). Does not return a value.</summary>
    public void DrainEnergy(float amount)
    {
        currentEnergy = Mathf.Max(currentEnergy - amount, 0f);
        UpdateEnergyUI();
    }

    private void UpdateEnergyUI()
    {
        if (energyTMPText != null)
            energyTMPText.text = "Energy: " + Mathf.FloorToInt(currentEnergy).ToString();
    }

    // ================================================================
    //  INSUFFICIENT ENERGY FLASH
    // ================================================================

    /// <summary>
    /// Briefly flashes the energy UI text red to indicate an action
    /// failed due to insufficient energy.
    /// </summary>
    public void FlashInsufficient()
    {
        if (energyTMPText == null) return;
        if (insufficientFlashRoutine != null) StopCoroutine(insufficientFlashRoutine);
        insufficientFlashRoutine = StartCoroutine(InsufficientFlashCoroutine());
    }

    private IEnumerator InsufficientFlashCoroutine()
    {
        if (energyTMPText == null) yield break;

        energyTMPText.color = insufficientEnergyColor;
        float elapsed = 0f;
        while (elapsed < insufficientFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / insufficientFlashDuration);
            energyTMPText.color = Color.Lerp(insufficientEnergyColor, energyTextOriginalColor, t);
            yield return null;
        }
        energyTMPText.color = energyTextOriginalColor;
        insufficientFlashRoutine = null;
    }

    // ================================================================
    //  LAVA ZONE DETECTION  (unchanged from original)
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out LavaZone lavaZone))
        {
            if (playerCapsule == null)
            {
                currentLavaZone = lavaZone;
                currentLavaCollider = other;
                return;
            }

            if (playerCapsule.bounds.Intersects(other.bounds))
            {
                currentLavaZone = lavaZone;
                currentLavaCollider = other;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == currentLavaCollider)
        {
            currentLavaZone = null;
            currentLavaCollider = null;
            return;
        }

        if (other.TryGetComponent(out LavaZone lavaZone) && lavaZone == currentLavaZone)
        {
            if (playerCapsule == null || !playerCapsule.bounds.Intersects(other.bounds))
            {
                currentLavaZone = null;
                currentLavaCollider = null;
            }
        }
    }
}