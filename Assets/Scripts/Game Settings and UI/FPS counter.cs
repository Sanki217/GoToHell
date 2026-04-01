using UnityEngine;
using TMPro;

/// <summary>
/// FPS Counter — shows current and average FPS on screen.
///
/// SETUP:
///   1. Add this script to any GameObject in the scene
///   2. Create two TMP_Text elements in your Canvas (e.g. inside StatsPanel or a separate HUD panel)
///   3. Assign them to currentFpsLabel and averageFpsLabel
///   4. Tune updateInterval (default 0.2s) in Inspector
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Label that shows current FPS, updated every updateInterval seconds")]
    public TMP_Text currentFpsLabel;

    [Tooltip("Label that shows average FPS since the game started")]
    public TMP_Text averageFpsLabel;

    [Header("Settings")]
    [Tooltip("How often the current FPS display updates (seconds)")]
    public float updateInterval = 0.2f;

    [Tooltip("Color when FPS is good (above goodThreshold)")]
    public Color colorGood = new Color(0.2f, 1f, 0.2f);

    [Tooltip("Color when FPS is okay (above okayThreshold)")]
    public Color colorOkay = new Color(1f, 0.8f, 0.1f);

    [Tooltip("Color when FPS is bad (below okayThreshold)")]
    public Color colorBad = new Color(1f, 0.2f, 0.2f);

    [Tooltip("FPS threshold for 'good' color")]
    public float goodThreshold = 55f;

    [Tooltip("FPS threshold for 'okay' color")]
    public float okayThreshold = 30f;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private float intervalTimer = 0f;
    private float currentFps = 0f;

    private float totalFps = 0f;
    private int sampleCount = 0;

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        // Accumulate for rolling current FPS
        intervalTimer += Time.unscaledDeltaTime;

        // Accumulate for average
        if (Time.unscaledDeltaTime > 0f)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            totalFps += fps;
            sampleCount++;
        }

        // Update current display every interval
        if (intervalTimer >= updateInterval)
        {
            currentFps = 1f / Time.unscaledDeltaTime;
            intervalTimer = 0f;

            if (currentFpsLabel != null)
            {
                currentFpsLabel.text = $"FPS: {Mathf.RoundToInt(currentFps)}";
                currentFpsLabel.color = GetColor(currentFps);
            }

            // Update average display alongside current
            if (averageFpsLabel != null && sampleCount > 0)
            {
                float avg = totalFps / sampleCount;
                averageFpsLabel.text = $"AVG: {Mathf.RoundToInt(avg)}";
                averageFpsLabel.color = GetColor(avg);
            }
        }
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private Color GetColor(float fps)
    {
        if (fps >= goodThreshold) return colorGood;
        if (fps >= okayThreshold) return colorOkay;
        return colorBad;
    }

    /// <summary>Reset the running average (e.g. after a loading screen).</summary>
    public void ResetAverage()
    {
        totalFps = 0f;
        sampleCount = 0;
    }
}