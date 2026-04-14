using UnityEngine;

/// <summary>
/// DEBUG TOOL — Displays the distance from a reference point (usually the player
/// or shoot origin) to the cursor's world position, rendered as text near the cursor.
///
/// SETUP:
///   1. Add this component to any GameObject in the scene (e.g. the Player).
///   2. Assign Origin Transform — the point distances are measured FROM
///      (drag in the Player's ShootOrigin, or leave empty to use this GameObject).
///   3. Assign Main Camera (or leave empty — it auto-finds Camera.main).
///
/// The text appears near the cursor at all times in Play mode.
/// Disable the component (checkbox in Inspector) to hide it when not debugging.
/// </summary>
public class CursorRangeDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform measured FROM. Leave empty to use this GameObject's position.")]
    public Transform originTransform;

    [Tooltip("Camera used to convert cursor position to world space. Auto-assigns Camera.main.")]
    public Camera mainCamera;

    [Header("Display Settings")]
    [Tooltip("Pixel offset from the cursor tip so the text doesn't overlap the hotspot.")]
    public Vector2 cursorOffset = new Vector2(18f, 12f);

    [Tooltip("Font size of the range label.")]
    public int fontSize = 14;

    [Tooltip("Colour of the range text.")]
    public Color textColor = Color.white;

    [Tooltip("Whether to show a dark background box behind the text.")]
    public bool showBackground = true;

    // ================================================================

    private float currentRange;
    private GUIStyle labelStyle;
    private GUIStyle backgroundStyle;

    // ================================================================

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) return;

        // Project cursor onto the world plane at the origin's Z depth
        Vector3 origin = originTransform != null
            ? originTransform.position
            : transform.position;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, origin);
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 worldCursor = ray.GetPoint(dist);
            worldCursor.z = origin.z;   // flatten to same plane
            currentRange = Vector3.Distance(origin, worldCursor);
        }
    }

    private void OnGUI()
    {
        if (!enabled) return;
        if (mainCamera == null) return;

        // Build styles once (can't do it in Start since it must be inside OnGUI)
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = textColor;
        }

        if (backgroundStyle == null)
        {
            backgroundStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(4, 4, 2, 2)
            };
        }

        // Convert mouse to GUI space (Y axis flipped in GUI)
        Vector2 mousePos = Event.current.mousePosition;
        float x = mousePos.x + cursorOffset.x;
        float y = mousePos.y - cursorOffset.y;

        string text = currentRange.ToString("F2") + "m";
        Vector2 size = labelStyle.CalcSize(new GUIContent(text));
        Rect rect = new Rect(x, y, size.x + 8f, size.y + 4f);

        if (showBackground)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.Box(rect, GUIContent.none, backgroundStyle);
            GUI.color = old;
        }

        GUI.Label(rect, text, labelStyle);
    }
}
