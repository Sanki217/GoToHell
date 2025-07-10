using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    public int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
            

        }
    }

    private void Die()
    {

        // Add death effects here later (particles, sounds)
        Camera.main.GetComponent<CameraFollow>()?.Shake(0.2f, 0.2f); // big shake
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
            if (energy != null)
            {
                energy.RestoreEnergy(10); // Adjust value as needed
            }
        }
        Destroy(gameObject);
    }
}
