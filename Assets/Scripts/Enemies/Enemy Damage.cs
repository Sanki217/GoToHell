using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 1;
    public float hitCooldown = 0.5f;

    private float lastHitTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (Time.time < lastHitTime + hitCooldown)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage);
        lastHitTime = Time.time;
    }
}
