using UnityEngine;

/// <summary>
/// End-of-level keystone: the FIRST point of damage from the player opens the
/// assigned portal. Further hits do nothing — no health, no death, no drops.
///
/// Extends Enemy so every weapon (arrow, slash, dash, explosions, statuses)
/// hits it through the standard damage path.
///
/// SETUP:
///   1. Create the keystone object, TAG IT "Enemy", add a solid collider.
///   2. Add this component.
///   3. Create the portal (cylinder + trigger collider + LevelExit component),
///      DISABLE it, and assign it to "portal" here.
/// </summary>
public class PortalOpener : Enemy
{
    [Header("Portal")]
    [Tooltip("Disabled portal object (cylinder with a trigger collider + LevelExit). Activated on first hit.")]
    public GameObject portal;

    [Header("Feedback")]
    public float openShakeMagnitude = 0.25f;
    public string openText = "PORTAL OPENED";

    private bool opened;

    public override void TakeDamage(int amount, Vector3 hitPosition,
                                    Vector3 knockbackDir, float knockbackForce,
                                    bool isCrit, FloatingTextManager.HitType hitType)
    {
        if (opened) return;
        opened = true;

        if (portal != null)
            portal.SetActive(true);
        else
            Debug.LogWarning("[PortalOpener] No portal assigned.", this);

        FloatingTextManager.ShowText(openText, transform.position,
                                     FloatingTextManager.HitType.HolyDetonate);
        PlayerRefs.CamFollow?.Shake(openShakeMagnitude, 0.2f);
    }
}
