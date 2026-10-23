using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// In-game debug overlay — press ` (backquote) to toggle, in the editor and
/// in builds. Put ONE "DebugOverlay" object in each scene (Tools → Go To Hell
/// → Debug → Add Debug Overlay).
///
/// Every enemy gets: a line to the player with the distance, HP, Z depth,
/// speed multiplier. On top of that, every component implementing
/// IDebugDrawable draws its own info (ranges, targets, states, timers) —
/// enemies, the stalker spawner, the player's kick and class skill.
/// One colour per kind of info (DebugColors) — see the legend top-left.
/// </summary>
[DefaultExecutionOrder(1000)]   // after gameplay has moved things this frame
public class DebugOverlay : MonoBehaviour
{
    /// <summary>Overlay visible (survives scene loads).</summary>
    public static bool Enabled { get; private set; }

    [Header("What to show")]
    public bool showEnemyBasics = true;
    public bool showComponentDebug = true;
    public bool showLegend = true;
    [Tooltip("Enemies further than this from the player are skipped (0 = all).")]
    public float maxDistance = 60f;

    [Header("Look")]
    public int fontSize = 13;
    [Tooltip("Rescan the scene for IDebugDrawable components this often (seconds).")]
    public float rescanInterval = 0.5f;

    private readonly List<IDebugDrawable> drawables = new List<IDebugDrawable>();
    private float nextScan;
    private Material lineMat;
    private GUIStyle style, shadow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => Enabled = false;

    private void Update()
    {
        if (GameInput.DebugTogglePressed)
        {
            Enabled = !Enabled;
            nextScan = 0f;
        }
    }

    private void LateUpdate()
    {
        DebugDraw.Clear();
        if (!Enabled) return;

        if (Time.unscaledTime >= nextScan) Rescan();

        if (showEnemyBasics) DrawEnemyBasics();

        if (showComponentDebug)
            foreach (IDebugDrawable d in drawables)
            {
                if (d is Behaviour b && (b == null || !b.isActiveAndEnabled)) continue;
                try { d.DrawDebug(); }
                catch (System.Exception e) { Debug.LogException(e); }
            }
    }

    private void Rescan()
    {
        nextScan = Time.unscaledTime + rescanInterval;
        drawables.Clear();
        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb is IDebugDrawable d) drawables.Add(d);
    }

    private void DrawEnemyBasics()
    {
        Transform player = PlayerRefs.PlayerTransform;
        foreach (Enemy e in EnemyRegistry.All)
        {
            if (e == null || e.IsDead) continue;
            Vector3 p = e.transform.position;

            float dist = player != null ? Vector2.Distance(p, player.position) : 0f;
            if (player != null && maxDistance > 0f && dist > maxDistance) continue;

            if (player != null)
            {
                DebugDraw.Line(p, player.position, DebugColors.Distance);
                DebugDraw.Label(Vector3.Lerp(p, player.position, 0.5f), $"{dist:0.0} m", DebugColors.Distance);
            }

            Vector3 head = p + Vector3.up * 1.2f;
            DebugDraw.Label(head, $"{e.name}", DebugColors.State, 0);
            DebugDraw.Label(head, $"HP {e.CurrentHealth}/{e.MaxHealth}", DebugColors.Health, 1);
            DebugDraw.Label(head, $"z {p.z:0.0}", DebugColors.Depth, 2);
            if (!Mathf.Approximately(e.SpeedMultiplier, 1f))
                DebugDraw.Label(head, $"speed x{e.SpeedMultiplier:0.00}", DebugColors.Velocity, 3);
        }

        // Player basics
        if (player != null)
        {
            Vector3 head = player.position + Vector3.up * 1.6f;
            DebugDraw.Label(head, $"z {player.position.z:0.0}", DebugColors.Depth, 0);
            if (player.TryGetComponent(out Rigidbody rb))
            {
                Vector3 v = rb.linearVelocity;
                DebugDraw.Arrow(player.position, player.position + new Vector3(v.x, v.y, 0f) * 0.2f, DebugColors.Velocity);
                DebugDraw.Label(head, $"v {new Vector2(v.x, v.y).magnitude:0.0}", DebugColors.Velocity, 1);
            }
            if (player.TryGetComponent(out DepthTracker depth))
                DebugDraw.Label(head, $"depth {depth.CurrentDepth:0} m", DebugColors.Depth, 2);
        }
    }

    // ================================================================
    //  RENDER — lines (GL, on top of everything) + labels (OnGUI)
    // ================================================================

    private void OnRenderObject()
    {
        if (!Enabled || DebugDraw.Lines.Count == 0) return;
        Camera cam = Camera.current;
        if (cam == null || cam != PlayerRefs.Cam) return;

        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            lineMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_Cull", (int)CullMode.Off);
            lineMat.SetInt("_ZWrite", 0);
            lineMat.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.LINES);
        foreach (DebugDraw.LineCmd l in DebugDraw.Lines)
        {
            GL.Color(l.c);
            GL.Vertex(l.a);
            GL.Vertex(l.b);
        }
        GL.End();
        GL.PopMatrix();
    }

    private void OnGUI()
    {
        if (!Enabled) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = false };
            shadow = new GUIStyle(style);
            shadow.normal.textColor = Color.black;
        }

        Camera cam = PlayerRefs.Cam;
        if (cam != null)
        {
            float lineH = fontSize + 3;
            foreach (DebugDraw.LabelCmd l in DebugDraw.Labels)
            {
                Vector3 s = cam.WorldToScreenPoint(l.pos);
                if (s.z <= 0f) continue;
                var r = new Rect(s.x, Screen.height - s.y + l.row * lineH, 400f, lineH + 4);
                Text(r, l.text, l.c);
            }
        }

        if (showLegend) DrawLegend();
    }

    private void DrawLegend()
    {
        float lineH = fontSize + 4;
        var box = new Rect(8, 8, 230, (DebugColors.Legend.Length + 1) * lineH + 8);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;
        Text(new Rect(14, 12, 220, lineH), $"DEBUG  (` to hide)   enemies {EnemyRegistry.Count}", Color.white);
        for (int i = 0; i < DebugColors.Legend.Length; i++)
        {
            var (name, color) = DebugColors.Legend[i];
            Text(new Rect(14, 12 + (i + 1) * lineH, 220, lineH), "■ " + name, color);
        }
    }

    private void Text(Rect r, string text, Color c)
    {
        GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, shadow);
        style.normal.textColor = c;
        GUI.Label(r, text, style);
    }

    private void OnDestroy()
    {
        if (lineMat != null) Destroy(lineMat);
    }
}
