using UnityEngine;
using TMPro;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings — defaults, overridden by PlayerStats at runtime")]
    public float maxEnergy = 100f;
    public float currentEnergy = 0f;

    [Header("Gain Rates")]
    public float energyPerVelocityUnit = 2f;
    public float wallSlideEnergyMultiplier = 2f;

    [Header("UI Reference")]
    public TMP_Text energyTMPText;

    private PlayerMovement playerMovement;
    private PlayerStats playerStats;
    private LavaZone currentLavaZone;
    private Collider currentLavaCollider;
    private CapsuleCollider playerCapsule;

    // Always read max energy from PlayerStats when available
    private float MaxEnergy => playerStats != null ? playerStats.maxEnergy : maxEnergy;

    // Regen multiplier from PlayerStats — applied to ALL energy gained
    private float RegenMultiplier => playerStats != null ? playerStats.energyRegenMultiplier : 1f;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();
        playerCapsule = GetComponent<CapsuleCollider>();

        if (playerCapsule == null)
            Debug.LogWarning("PlayerEnergy: No CapsuleCollider found on player.");
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