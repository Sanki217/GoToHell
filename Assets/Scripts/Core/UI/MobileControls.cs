using UnityEngine;

/// <summary>
/// Shows the on-screen touch controls only when they're needed: on phones /
/// tablets, in "Simulate Mobile" mode, or after the player touches the
/// screen. Hidden while the game is paused so menus get every tap.
///
/// The sticks and buttons under <see cref="controlsRoot"/> are Input System
/// OnScreenStick / OnScreenButton components bound to GAMEPAD paths
/// (left stick = move, right stick = aim, south = jump …). Gameplay already
/// reads those through GameInput, so touch support needs no gameplay code.
///
/// Build a starter layout: Tools → Go To Hell → Mobile → Build Mobile Controls
/// (in a gameplay scene), then restyle freely.
/// </summary>
public class MobileControls : MonoBehaviour
{
    [Tooltip("Parent of every on-screen stick/button. Toggled on/off.")]
    public GameObject controlsRoot;

    [Tooltip("Always show (layout work on desktop).")]
    public bool showOnDesktop;

    public bool hideWhilePaused = true;

    private bool visible = true;

    private void OnEnable()
    {
        GameInput.OnSchemeChanged += OnScheme;
        Refresh();
    }

    private void OnDisable() => GameInput.OnSchemeChanged -= OnScheme;

    private void OnScheme(InputScheme _) => Refresh();

    private void Update() => Refresh();

    private void Refresh()
    {
        bool want = showOnDesktop || Platform.IsTouchPrimary || GameInput.CurrentScheme == InputScheme.Touch;
        if (hideWhilePaused && GameTime.IsPaused) want = false;
        if (want == visible && controlsRoot != null && controlsRoot.activeSelf == want) return;

        visible = want;
        if (controlsRoot != null) controlsRoot.SetActive(want);
    }
}
