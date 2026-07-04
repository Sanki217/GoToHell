using UnityEngine;

/// <summary>
/// Attach to any enemy child GameObject that has a Trigger Collider.
/// Deals damage to the player on contact.
/// The "damage" field is fully visible and editable in the Inspector.
///
/// Passes its own position as the damage source, so the Warrior's directional
/// shield (ShieldAbility) can block contact damage from the facing side.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Enemy Contact Damage")]
    public int damage = 10;
    public float hitCooldown = 0.5f;   // seconds between hits (prevents damage spam)

    private float lastHitTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastHitTime + hitCooldown) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage, transform.position);
        lastHitTime = Time.time;
    }
}
