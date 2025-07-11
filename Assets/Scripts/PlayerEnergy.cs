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
        float yVelocity = playerMovement.GetVelocity().y;
        if (yVelocity < 0f)
        {
            float gain = Mathf.Abs(yVelocity) * energyPerVelocityUnit;

            if (playerMovement.isWallSliding)
                gain *= wallSlideEnergyMultiplier;

            currentEnergy = Mathf.Clamp(currentEnergy + gain * Time.deltaTime, 0, maxEnergy);
        }

        if (currentLavaZone != null)
        {
            float lavaGain = currentLavaZone.DrainEnergy(Time.deltaTime);
            currentEnergy = Mathf.Clamp(currentEnergy + lavaGain, 0, maxEnergy);
        }
    }

    public void RestoreEnergy(float amount) //killing
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        UpdateEnergyUI(); // if you have one
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
    public void DrainEnergy(float amount) //hover
    {
        currentEnergy = Mathf.Max(currentEnergy - amount, 0f);
        UpdateEnergyUI(); // optional
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
