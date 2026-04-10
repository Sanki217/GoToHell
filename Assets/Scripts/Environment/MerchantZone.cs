using UnityEngine;

/// <summary>
/// Merchant Zone — a BoxCollider trigger at the end of a level.
///
/// When the player enters the zone the camera smoothly travels to a
/// child anchor GameObject and stays locked there (no follow, no shake,
/// no charge-zoom). When the player exits, the camera smoothly resumes
/// following the player.
///
/// SETUP:
///   1. Create an empty GameObject at the entrance to the merchant area.
///   2. Add a BoxCollider — set Is Trigger = true. Size it to cover the zone.
///   3. Add this script.
///   4. Create a CHILD empty GameObject named e.g. "CameraAnchor".
///      Position it in the Scene where you want the camera to sit
///      (set the Z to your desired camera depth, e.g. −15).
///   5. Drag that child into the Camera Anchor field in the Inspector.
///   6. Tune Smooth Time in the Inspector.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class MerchantZone : MonoBehaviour
{
    [Header("Camera Anchor")]
    [Tooltip("Child GameObject positioned at the exact world position you want the camera " +
             "to travel to inside the zone (X, Y, and Z all count).")]
    public Transform cameraAnchor;

    [Header("Transition")]
    [Tooltip("SmoothDamp smooth time for entering and leaving the zone. " +
             "Lower = snappier, higher = more gradual.")]
    public float smoothTime = 0.5f;

    // ================================================================

    private CameraFollow cameraFollow;
    private bool playerInside = false;

    private void Start()
    {
        cameraFollow = Camera.main?.GetComponent<CameraFollow>();

        // Guarantee the collider is always a trigger
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || playerInside) return;
        playerInside = true;

        if (cameraFollow == null || cameraAnchor == null)
        {
            Debug.LogWarning("[MerchantZone] Missing CameraFollow or CameraAnchor reference.", this);
            return;
        }

        cameraFollow.LockToAnchor(cameraAnchor, smoothTime);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !playerInside) return;
        playerInside = false;

        cameraFollow?.Unlock();
    }

    // ================================================================
    //  EDITOR GIZMO
    // ================================================================

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw zone volume
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.85f, 0.6f, 0.1f, 0.18f);
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(0.85f, 0.6f, 0.1f, 0.75f);
            Gizmos.DrawWireCube(col.center, col.size);
            Gizmos.matrix = old;
        }

        // Draw line to anchor so you can see where the camera will travel
        if (cameraAnchor != null)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(cameraAnchor.position, 0.3f);
            Gizmos.DrawLine(transform.position, cameraAnchor.position);
        }
    }
#endif
}
