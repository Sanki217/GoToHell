using UnityEngine;

/// <summary>
/// Hazard: damages the player on contact (non-directional — the Warrior's
/// shield can't block it) and fires the upgrade-bus SpikesTouched event.
/// </summary>
public class Spikes : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.TakeDamage(damage);
        FXManager.Play(ActionFX.PlayerSpikesTouched, other.transform.position);

        PlayerUpgradeManager mgr = PlayerRefs.I != null ? PlayerRefs.I.Upgrades : null;
        if (mgr != null) mgr.SpikesTouched();
    }
}
