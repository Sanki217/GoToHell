using UnityEngine;

/// <summary>
/// Attach to the SlashCollider child GameObject (the one with BoxCollider Is Trigger = true).
/// Reports trigger hits back to PlayerSlash on the parent.
///
/// SETUP:
///   - Add to the SlashCollider child GameObject alongside the BoxCollider
///   - Make sure the BoxCollider Is Trigger = true
///   - The parent must have PlayerSlash.cs
/// </summary>
public class SlashDamageCollider : MonoBehaviour
{
    private PlayerSlash playerSlash;

    private void Awake()
    {
        playerSlash = GetComponentInParent<PlayerSlash>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerSlash == null) return;

        if (other.CompareTag("Enemy"))
        {
            playerSlash.OnSlashHitEnemy(other.gameObject);
            return;
        }

        // Destructibles
        if (other.GetComponent<Vase>() != null || other.GetComponent<ExplosiveBarrel>() != null)
        {
            playerSlash.OnSlashHitDestructible(other.gameObject);
        }
    }
}