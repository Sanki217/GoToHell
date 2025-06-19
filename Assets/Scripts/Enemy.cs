using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    private int currentHealth;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().TakeDamage(1);
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
        Destroy(gameObject);
    }

}
