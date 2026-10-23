using UnityEngine;

/// <summary>
/// Side-on camera for the town: follows the player left/right (and a little
/// up/down), clamped to the street's ends. Put on the town's Main Camera.
/// </summary>
public class TownCamera : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 2.5f, -16f);
    [Tooltip("Higher = snappier.")]
    public float smoothSpeed = 6f;
    [Tooltip("Camera X never goes outside this range.")]
    public float minX = -20f;
    public float maxX = 20f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        Transform p = PlayerRefs.PlayerTransform;
        if (p == null) return;

        Vector3 target = p.position + offset;
        target.x = Mathf.Clamp(target.x, minX, maxX);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 1f / Mathf.Max(0.01f, smoothSpeed));
    }
}
