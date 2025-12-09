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
    private Collider currentLavaCollider;        // collider of the lava zone we're in (for exit checks)
    private CapsuleCollider playerCapsule;       // player's capsule collider (root)

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCapsule = GetComponent<CapsuleCollider>();

        if (playerCapsule == null)
            Debug.LogWarning("PlayerEnergy: No CapsuleCollider found on player. Lava detection uses the player's CapsuleCollider bounds.");
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
        UpdateEnergyUI();
    }

    private void UpdateEnergyUI()
    {
        if (energyTMPText != null)
            energyTMPText.text = "Energy: " + Mathf.FloorToInt(currentEnergy).ToString();
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
        // If the collider we hit is a LavaZone, only register it if the player's capsule is actually overlapping that collider.
        if (other.TryGetComponent(out LavaZone lavaZone))
        {
            // If we don't have a capsule, fallback to the old behavior (best-effort).
            if (playerCapsule == null)
            {
                currentLavaZone = lavaZone;
                currentLavaCollider = other;
                return;
            }

            // Use bounds intersection to confirm the player's capsule overlaps the lava collider.
            // This prevents child triggers (like Looter) from falsely registering the player as "in lava".
            if (playerCapsule.bounds.Intersects(other.bounds))
            {
                currentLavaZone = lavaZone;
                currentLavaCollider = other;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If the collider leaving is the same lava collider we registered, clear it.
        if (other == currentLavaCollider)
        {
            currentLavaZone = null;
            currentLavaCollider = null;
            return;
        }

        // Safety: if some other lava zone exit fired, and the player's capsule no longer intersects that lava, clear anyway.
        if (other.TryGetComponent(out LavaZone lavaZone) && lavaZone == currentLavaZone)
        {
            // If we still intersect bounds, keep it; otherwise clear.
            if (playerCapsule == null || !playerCapsule.bounds.Intersects(other.bounds))
            {
                currentLavaZone = null;
                currentLavaCollider = null;
            }
        }
    }
}
