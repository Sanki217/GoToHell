using UnityEngine;

public class LavaZone : MonoBehaviour
{
    [Header("Lava Settings")]
    public float maxLavaEnergy = 50f;
    public float drainRate = 20f;

    private float currentLavaEnergy;

    void Start()
    {
        currentLavaEnergy = maxLavaEnergy;
    }

    public float DrainEnergy(float deltaTime)
    {
        if (currentLavaEnergy <= 0f)
            return 0f;

        float amountToGive = Mathf.Min(drainRate * deltaTime, currentLavaEnergy);
        currentLavaEnergy -= amountToGive;
        return amountToGive;
    }

    public void RefillLava(float amount)
    {
        currentLavaEnergy = Mathf.Clamp(currentLavaEnergy + amount, 0f, maxLavaEnergy);
    }

  
}
