using UnityEngine;

/// <summary>
/// Merchant Zone — a BoxCollider trigger at the end of a level.
///
/// When the player enters the zone the camera smoothly shifts to a new
/// Z position and Y offset (useful for a wider/pulled-back merchant view).
/// On exit the camera smoothly returns to its original values.
///
/// SETUP:
///   1. Create an empty GameObject at the entrance to the merchant area.
///   2. Add a BoxCollider. Set Is Trigger = true. Size it to cover the area.
///   3. Add this script.
///   4. Tune merchantCameraZ and merchantYOffset in the Inspector.
///      Leave as 0 to keep the camera's current default for that axis.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class MerchantZone : MonoBehaviour
{
    [Header("Camera Overrides")]
    [Tooltip("Z position the camera moves to inside the merchant zone. " +
             "More negative = zoomed out. Leave at 0 to use the camera's current Z.")]
    public float merchantCameraZ = 0f;

    [Tooltip("Y offset added to the camera's follow target inside the merchant zone.")]
    public float merchantYOffset = 2f;

    [Tooltip("If true, merchantCameraZ is treated as a delta from the camera's current Z " +
             "rather than an absolute value. e.g. -5 means 5 units further back.")]
    public bool deltaZ = false;

    // ================================================================

    private CameraFollow cameraFollow;
    private float originalZ;
    private float originalYOffset;
    private bool playerInside = false;

    private void Start()
    {
        cameraFollow = Camera.main?.GetComponent<CameraFollow>();

        if (cameraFollow != null)
        {
            originalZ = cameraFollow.transform.position.z;
            originalYOffset = cameraFollow.yOffset;

            // Default merchantCameraZ to the scene's existing camera Z if left at 0
            if (merchantCameraZ == 0f)
                merchantCameraZ = originalZ;
        }

        // Ensure BoxCollider is a trigger
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerInside) return;
        playerInside = true;

        if (cameraFollow == null) return;

        float targetZ = deltaZ ? originalZ + merchantCameraZ : merchantCameraZ;
        cameraFollow.SetTargetZ(targetZ);
        cameraFollow.SetTargetYOffset(merchantYOffset);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!playerInside) return;
        playerInside = false;

        if (cameraFollow == null) return;

        cameraFollow.SetTargetZ(originalZ);
        cameraFollow.SetTargetYOffset(originalYOffset);
    }

    // ================================================================
    //  EDITOR GIZMO
    // ================================================================

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0.8f, 0.6f, 0.1f, 0.25f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = new Color(0.8f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(col.center, col.size);
        Gizmos.matrix = old;
    }
#endif
}
