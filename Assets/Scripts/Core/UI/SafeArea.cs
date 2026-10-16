using UnityEngine;

/// <summary>
/// Keeps a UI panel inside the screen's safe area — away from phone notches,
/// camera holes, rounded corners and home-indicator bars. Put it on a
/// full-screen child of a HUD canvas and parent the HUD under it.
/// Backgrounds that SHOULD go edge-to-edge stay outside it.
///
/// Does nothing on desktop unless applyOnDesktop is on (the safe area is the
/// full screen there anyway). Test with Window → General → Device Simulator.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    [Tooltip("Also apply on desktop (safe area = full screen, so usually a no-op).")]
    public bool applyOnDesktop;

    public bool conformX = true;
    public bool conformY = true;

    private RectTransform rect;
    private Rect lastSafe;
    private Vector2Int lastScreen;

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        Refresh(force: true);
    }

    private void Update() => Refresh(force: false);

    private void Refresh(bool force)
    {
        if (rect == null) return;
        Rect safe = (Platform.IsMobile || applyOnDesktop || !Application.isPlaying)
            ? Screen.safeArea
            : new Rect(0f, 0f, Screen.width, Screen.height);

        var screen = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == lastSafe && screen == lastScreen) return;
        lastSafe = safe;
        lastScreen = screen;
        if (screen.x <= 0 || screen.y <= 0) return;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= screen.x; max.x /= screen.x;
        min.y /= screen.y; max.y /= screen.y;

        if (!conformX) { min.x = 0f; max.x = 1f; }
        if (!conformY) { min.y = 0f; max.y = 1f; }

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
