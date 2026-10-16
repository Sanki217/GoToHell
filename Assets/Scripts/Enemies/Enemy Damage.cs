using UnityEngine;

/// <summary>
/// Attach to any enemy child GameObject that has a Trigger Collider.
/// Deals damage to the player on contact — and keeps dealing it every
/// hitCooldown seconds while the player stays inside (standing in an enemy
/// used to be safe after the first hit). PlayerHealth's i-frames still apply.
///
/// Passes its own position as the damage source, so the Warrior's directional
/// shield (ShieldAbility) can block contact damage from the facing side.
///
/// Damage comes from the parent Enemy's EnemyDefinition when it has one
/// (contactDamage), otherwise from the field below — scaled by layer and
/// pact / meta modifiers either way (Enemy.ScaleContactDamage).
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Enemy Contact Damage")]
    public int damage = 10;
    public float hitCooldown = 0.5f;   // seconds between hits (prevents damage spam)

    private float lastHitTime = -999f;
    private Enemy owner;

    private void Awake() => owner = GetComponentInParent<Enemy>();

    /// <summary>Damage actually dealt per hit.</summary>
    public int EffectiveDamage => owner != null ? owner.ScaleContactDamage(damage) : damage;

    private void OnTriggerEnter(Collider other) => TryHit(other);
    private void OnTriggerStay(Collider other)  => TryHit(other);

    private void TryHit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastHitTime + hitCooldown) return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        int dmg = EffectiveDamage;
        if (dmg <= 0) return;
        playerHealth.TakeDamage(dmg, transform.position, gameObject);
        lastHitTime = Time.time;
    }
}
