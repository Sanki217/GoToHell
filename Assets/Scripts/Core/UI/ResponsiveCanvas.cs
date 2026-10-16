using UnityEngine;

/// <summary>
/// Optional per-canvas override for UIScaleManager. Without this component a
/// screen-space canvas gets the defaults (1920×1080, auto match).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class ResponsiveCanvas : MonoBehaviour
{
    [Tooltip("Leave this canvas completely alone (e.g. pixel-perfect debug overlays).")]
    public bool exempt;

    [Tooltip("Custom reference resolution. 0,0 = the global 1920×1080.")]
    public Vector2 referenceResolution;

    [Tooltip("Force a width/height match (0 = width, 1 = height). −1 = automatic by aspect ratio.")]
    [Range(-1f, 1f)] public float matchOverride = -1f;
}
