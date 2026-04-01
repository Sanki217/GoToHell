using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Camera Projection Toggle — switches between Perspective and Orthographic.
/// Attach to a settings panel. Wire up the button and label in Inspector.
///
/// SETUP:
///   1. Add this script to your Settings Panel GameObject (or any persistent object)
///   2. Assign targetCamera (or leave empty to use Camera.main)
///   3. Create a Button in your Settings UI, assign it to toggleButton
///   4. Create a TMP_Text for the button label, assign to buttonLabel
///   5. Set orthographicSize to tune the ortho view (default 10)
///
/// The toggle saves the current choice to PlayerPrefs so it persists between sessions.
/// </summary>
public class CameraProjectionToggle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to use Camera.main")]
    public Camera targetCamera;
    public Button toggleButton;
    public TMP_Text buttonLabel;

    [Header("Perspective Settings")]
    public float perspectiveFOV = 76.449f;   // match your current camera FOV

    [Header("Orthographic Settings")]
    [Tooltip("Orthographic size — half the vertical height visible in world units")]
    public float orthographicSize = 10f;

    [Header("Transition")]
    [Tooltip("Duration of smooth blend between projection modes (seconds). 0 = instant.")]
    public float transitionDuration = 0.3f;

    // ================================================================
    //  STATE
    // ================================================================

    private bool isOrthographic = false;
    private Coroutine transitionCoroutine;

    private const string PrefKey = "CameraProjection";

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        // Restore saved preference
        isOrthographic = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        ApplyImmediate();
        UpdateLabel();
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    public void Toggle()
    {
        isOrthographic = !isOrthographic;
        PlayerPrefs.SetInt(PrefKey, isOrthographic ? 1 : 0);
        PlayerPrefs.Save();

        UpdateLabel();

        if (transitionDuration > 0f)
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionRoutine());
        }
        else
        {
            ApplyImmediate();
        }
    }

    // ================================================================
    //  APPLY
    // ================================================================

    private void ApplyImmediate()
    {
        if (targetCamera == null) return;

        if (isOrthographic)
        {
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = orthographicSize;
        }
        else
        {
            targetCamera.orthographic = false;
            targetCamera.fieldOfView = perspectiveFOV;
        }
    }

    private IEnumerator TransitionRoutine()
    {
        if (targetCamera == null) yield break;

        float elapsed = 0f;
        bool toOrtho = isOrthographic;

        // Capture starting values
        float startFOV = targetCamera.fieldOfView;
        float startOrtho = targetCamera.orthographicSize;

        // When going to ortho: animate FOV wider (zoom out feel), then switch
        // When going to persp: switch first, then animate FOV back
        if (toOrtho)
        {
            // Stay perspective while animating, switch at the end
            targetCamera.orthographic = false;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                // Animate FOV from current toward a wide value
                targetCamera.fieldOfView = Mathf.Lerp(startFOV, perspectiveFOV * 1.4f, t);
                yield return null;
            }

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = orthographicSize;
        }
        else
        {
            // Switch to perspective first, animate FOV in
            targetCamera.orthographic = false;
            targetCamera.fieldOfView = perspectiveFOV * 1.4f;  // start wide

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                targetCamera.fieldOfView = Mathf.Lerp(perspectiveFOV * 1.4f, perspectiveFOV, t);
                yield return null;
            }

            targetCamera.fieldOfView = perspectiveFOV;
        }

        transitionCoroutine = null;
    }

    private void UpdateLabel()
    {
        if (buttonLabel == null) return;
        buttonLabel.text = isOrthographic ? "Perspective" : "Orthographic";
    }
}