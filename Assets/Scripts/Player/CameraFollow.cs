using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;

    [Header("Offsets")]
    public float parallaxRatio = 0.2f;
    public float yOffset = 0f;

    [Header("Shake Settings")]
    public bool shakeEnabled = true;
    [Tooltip("Global intensity scale applied to all shakes. 0 = off, 1 = full.")]
    [Range(0f, 2f)]
    public float shakeIntensity = 1f;

    [Header("Shake Decay")]
    [Tooltip("How fast trauma decays per second. Higher = shorter shakes.")]
    public float traumaDecayRate = 3f;

    [Header("Zone Override (set by MerchantZone)")]
    [Tooltip("How fast the camera Z and Y offset transition to zone-override values.")]
    public float zoneTransitionSpeed = 2f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 shakeOffset = Vector3.zero;

    // Trauma-based shake: 0–1 value, decays over time
    private float trauma = 0f;

    // Shake noise seed so offsets look random but are smooth
    private float seedX;
    private float seedY;

    // Zone override targets — set by MerchantZone
    private float targetZ;
    private float targetYOffset;
    private float zVelocity;
    private float yOffsetVelocity;

    private void Start()
    {
        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
        targetZ = transform.position.z;
        targetYOffset = yOffset;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Smoothly transition zone-overridden Z and Y offset
        float smoothZ = Mathf.SmoothDamp(transform.position.z, targetZ,
                                         ref zVelocity, 1f / zoneTransitionSpeed);
        yOffset = Mathf.SmoothDamp(yOffset, targetYOffset,
                                   ref yOffsetVelocity, 1f / zoneTransitionSpeed);

        float targetX = player.position.x * -parallaxRatio;
        float targetY = player.position.y + yOffset;

        Vector3 targetPosition = new Vector3(targetX, targetY, smoothZ);

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            1f / smoothSpeed
        );

        // Decay trauma every frame — framerate-independent
        trauma = Mathf.Max(0f, trauma - traumaDecayRate * Time.unscaledDeltaTime);

        if (shakeEnabled && shakeIntensity > 0f && trauma > 0f && !IsBlockedByUI())
        {
            float magnitude = trauma * trauma; // squaring gives more dramatic falloff

            // Perlin noise gives smooth but unpredictable shake — framerate-independent
            float t = Time.unscaledTime;
            float nx = Mathf.PerlinNoise(seedX, t * 10f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(seedY, t * 10f) * 2f - 1f;
            shakeOffset = new Vector3(nx, ny, 0f) * magnitude * shakeIntensity;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        transform.position = smoothedPosition + shakeOffset;
    }

    /// <summary>
    /// Add shake trauma (0–1). Multiple calls accumulate.
    /// duration param kept for backwards compatibility but is no longer used — 
    /// decay is controlled by traumaDecayRate.
    /// magnitude maps to trauma added.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        // Clamp trauma to 1 so it never over-accumulates
        trauma = Mathf.Clamp01(trauma + magnitude);
    }

    private bool IsBlockedByUI()
    {
        // Check both chest UI and level-up UI
        if (ChestRewardUI.Instance != null && ChestRewardUI.Instance.IsOpen) return true;
        if (LevelUpUI.Instance != null && LevelUpUI.Instance.IsOpen) return true;
        return false;
    }

    // ================================================================
    //  ZONE OVERRIDES — called by MerchantZone (or any trigger zone)
    // ================================================================

    /// <summary>Set the camera Z target. Camera smoothly lerps to this value.</summary>
    public void SetTargetZ(float z) => targetZ = z;

    /// <summary>Set the camera Y offset target. Camera smoothly lerps to this value.</summary>
    public void SetTargetYOffset(float offset) => targetYOffset = offset;

    /// <summary>Returns the current baseline Z (before any zone override).</summary>
    public float DefaultZ => targetZ;
}