using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;
    private int currentLives;

    [Header("UI Reference")]
    public TMP_Text healthTMPText;
    private DashAbility dashAbility;

    [Header("Damage Cooldown")]
    public float invincibilityTime = 1f;
    private bool isInvincible = false;


    private void Start()
    {
        currentLives = maxLives;
        UpdateHealthUI();
        dashAbility = GetComponent<DashAbility>();
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || dashAbility != null && dashAbility.isDashing)
            return; // INVINCIBLE while dashing

        currentLives -= amount;
        Camera.main.GetComponent<CameraFollow>()?.Shake(0.15f, 0.15f); // big shake

        currentLives = Mathf.Max(currentLives, 0);
        UpdateHealthUI();

        if (currentLives <= 0)
        {
            Debug.Log("PLAYER DEAD!");


            Object.FindFirstObjectByType<GameStartSequence>().PlayerDied();

            // TODO: Add game over logic
        }

        StartCoroutine(InvincibilityTimer());
    }

    private void UpdateHealthUI()
    {
        if (healthTMPText != null)
            healthTMPText.text = "Lives: " + currentLives;
    }

    private System.Collections.IEnumerator InvincibilityTimer()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }


}
