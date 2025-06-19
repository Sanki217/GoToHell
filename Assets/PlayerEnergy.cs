using UnityEngine;
using TMPro;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy = 0f;

    [Header("Gain Rates")]
    public float energyPerVelocityUnit = 2f;
    public float wallSlideEnergyMultiplier = 2f;

    [Header("UI Reference")]
    public TMP_Text energyTMPText;

    private PlayerMovement playerMovement;
    private LavaZone currentLavaZone;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        HandleEnergyGain();
        UpdateEnergyUI();
    }

    private void HandleEnergyGain()
    {
        float absYVelocity = Mathf.Abs(playerMovement.GetVelocity().y);
        float gain = absYVelocity * energyPerVelocityUnit;

        if (playerMovement.isWallSliding)
            gain *= wallSlideEnergyMultiplier;

        currentEnergy = Mathf.Clamp(currentEnergy + gain * Time.deltaTime, 0, maxEnergy);

        if (currentLavaZone != null)
        {
            float lavaGain = currentLavaZone.DrainEnergy(Time.deltaTime);
            currentEnergy = Mathf.Clamp(currentEnergy + lavaGain, 0, maxEnergy);
        }
    }

    private void UpdateEnergyUI()
    {
        if (energyTMPText != null)
            energyTMPText.text = Mathf.FloorToInt(currentEnergy).ToString();
    }

    public bool SpendEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out LavaZone lavaZone))
        {
            currentLavaZone = lavaZone;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out LavaZone lavaZone) && lavaZone == currentLavaZone)
        {
            currentLavaZone = null;
        }
    }
}
