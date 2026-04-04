using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;

    [Header("Offsets")]
    public float parallaxRatio = 0.2f;
    public float yOffset = 0f;

    [Header("Settings")]
    public bool shakeEnabled = true;

    private Vector3 velocity = Vector3.zero;
    private Vector3 shakeOffset = Vector3.zero;

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;

    void LateUpdate()
    {
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

        if (shakeEnabled && shakeDuration > 0f)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0f;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        transform.position = smoothedPosition + shakeOffset;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}