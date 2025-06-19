using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;
    private int currentLives;

    [Header("UI Reference")]
    public TMP_Text healthTMPText;

    private void Start()
    {
        currentLives = maxLives;
        UpdateHealthUI();
    }

    public void TakeDamage(int amount)
    {
        currentLives -= amount;
        currentLives = Mathf.Max(currentLives, 0);
        UpdateHealthUI();

        if (currentLives <= 0)
        {
            Debug.Log("PLAYER DEAD!");
            // TODO: Add game over logic
        }
    }

    private void UpdateHealthUI()
    {
        if (healthTMPText != null)
            healthTMPText.text = "Lives: " + currentLives;
    }
}
