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

    // ================================================================
    //  STATE
    // ================================================================

    private Vector3 velocity = Vector3.zero;
    private Vector3 shakeOffset = Vector3.zero;
    private float trauma = 0f;
    private float seedX;
    private float seedY;

    // Zone anchor lock — set by MerchantZone
    private Transform lockedAnchor = null;
    private float lockedSmoothTime = 0.5f;
    private bool isLocked = false;

    /// <summary>True while the camera is locked to a zone anchor (e.g. merchant zone).</summary>
    public bool IsLocked => isLocked;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
    }

    // ================================================================
    //  LATE UPDATE
    // ================================================================

    void LateUpdate()
    {
        if (isLocked && lockedAnchor != null)
        {
            // Smoothly travel to the anchor — ignore player, shake, and zoom
            transform.position = Vector3.SmoothDamp(
                transform.position,
                lockedAnchor.position,
                ref velocity,
                lockedSmoothTime
            );
            return;
        }

        if (player == null) return;

        float targetX = player.position.x * -parallaxRatio;
        float targetY = player.position.y + yOffset;

        Vector3 targetPosition = new Vector3(targetX, targetY, transform.position.z);

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
            float magnitude = trauma * trauma;
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

    // ================================================================
    //  SHAKE — public API
    // ================================================================

    /// <summary>
    /// Add shake trauma (0–1). Multiple calls accumulate.
    /// duration param kept for backwards compatibility — decay controlled by traumaDecayRate.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (isLocked) return; // no shake while locked to zone anchor
        trauma = Mathf.Clamp01(trauma + magnitude);
    }

    // ================================================================
    //  ZONE ANCHOR LOCK — called by MerchantZone (or any trigger zone)
    // ================================================================

    /// <summary>
    /// Lock the camera to a fixed anchor Transform.
    /// The camera smoothly travels to the anchor's world position and stays there.
    /// While locked, player-follow, shake, and charge-zoom are all suppressed.
    /// </summary>
    public void LockToAnchor(Transform anchor, float smoothTime)
    {
        lockedAnchor = anchor;
        lockedSmoothTime = Mathf.Max(0.01f, smoothTime);
        isLocked = true;
        // Clear shake so it doesn't bleed in at the start
        trauma = 0f;
        shakeOffset = Vector3.zero;
    }

    /// <summary>Release the zone lock — camera resumes following the player.</summary>
    public void Unlock()
    {
        isLocked = false;
        lockedAnchor = null;
        // Reset velocity so the return-to-player motion starts smoothly
        velocity = Vector3.zero;
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private bool IsBlockedByUI()
    {
        if (ChestRewardUI.Instance != null && ChestRewardUI.Instance.IsOpen) return true;
        if (LevelUpUI.Instance != null && LevelUpUI.Instance.IsOpen) return true;
        return false;
    }
}
