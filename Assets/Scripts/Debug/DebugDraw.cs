using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One colour per KIND of information, everywhere in the debug overlay —
/// learn it once from the legend (top-left) and read any enemy at a glance.
/// </summary>
public static class DebugColors
{
    public static readonly Color Distance  = new Color(0.65f, 0.65f, 0.65f);   // line + metres to the player
    public static readonly Color Health    = new Color(0.35f, 1f, 0.35f);      // HP
    public static readonly Color Depth     = new Color(1f, 0.3f, 1f);          // Z depth
    public static readonly Color Attack    = new Color(1f, 0.25f, 0.25f);      // attack / shoot range
    public static readonly Color Melee     = new Color(1f, 0.6f, 0.15f);       // lunge / jump / melee trigger range
    public static readonly Color Keep      = new Color(0.25f, 0.95f, 1f);      // preferred distance kept from the player
    public static readonly Color Aggro     = new Color(1f, 0.95f, 0.2f);       // aggro / detection range
    public static readonly Color Velocity  = new Color(0.35f, 0.55f, 1f);      // movement direction + speed
    public static readonly Color Target    = new Color(0.7f, 0.4f, 1f);        // where it wants to go / spawn areas
    public static readonly Color State     = Color.white;                       // behaviour state, timers, path
    public static readonly Color Player    = new Color(1f, 0.55f, 0.75f);      // player skills / reach

    public static readonly (string name, Color color)[] Legend =
    {
        ("Distance to player", Distance), ("Health", Health), ("Depth (Z)", Depth),
        ("Attack range", Attack), ("Melee / lunge / jump range", Melee), ("Kept distance", Keep),
        ("Aggro range", Aggro), ("Velocity", Velocity), ("Target / spawn area", Target),
        ("State / timers / path", State), ("Player skills", Player),
    };
}

/// <summary>Anything that draws its own debug info (enemies, player skills, spawners).</summary>
public interface IDebugDrawable
{
    /// <summary>Called every frame while the overlay is on. Use DebugDraw.*.</summary>
    void DrawDebug();
}

/// <summary>
/// Immediate-mode drawing for the in-game debug overlay (` key). Call from
/// IDebugDrawable.DrawDebug — everything is drawn for one frame only, on top
/// of the world, in the Game view and in builds. Shapes lie on the plane at
/// the given point's Z.
/// </summary>
public static class DebugDraw
{
    public struct LineCmd { public Vector3 a, b; public Color c; }
    public struct LabelCmd { public Vector3 pos; public string text; public Color c; public int row; }

    internal static readonly List<LineCmd> Lines = new List<LineCmd>(2048);
    internal static readonly List<LabelCmd> Labels = new List<LabelCmd>(512);

    /// <summary>True while the overlay is visible — skip expensive debug work otherwise.</summary>
    public static bool On => DebugOverlay.Enabled;

    internal static void Clear() { Lines.Clear(); Labels.Clear(); }

    public static void Line(Vector3 a, Vector3 b, Color c)
    {
        if (On) Lines.Add(new LineCmd { a = a, b = b, c = c });
    }

    public static void Circle(Vector3 center, float radius, Color c, int segments = 48)
    {
        if (!On || radius <= 0f) return;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            Lines.Add(new LineCmd { a = prev, b = next, c = c });
            prev = next;
        }
    }

    /// <summary>Circle plus its radius written on the rim, e.g. "attack 14".</summary>
    public static void Range(Vector3 center, float radius, Color c, string name)
    {
        if (!On) return;
        Circle(center, radius, c);
        Label(center + new Vector3(radius, 0f, 0f), $"{name} {radius:0.#}", c);
    }

    public static void Box(Vector3 center, Vector3 size, Color c)
    {
        if (!On) return;
        Vector3 h = size * 0.5f;
        Vector3 a = center + new Vector3(-h.x, -h.y), b = center + new Vector3(h.x, -h.y);
        Vector3 d = center + new Vector3(-h.x, h.y),  e = center + new Vector3(h.x, h.y);
        Line(a, b, c); Line(b, e, c); Line(e, d, c); Line(d, a, c);
    }

    public static void Cross(Vector3 p, float size, Color c)
    {
        Line(p + new Vector3(-size, -size), p + new Vector3(size, size), c);
        Line(p + new Vector3(-size, size), p + new Vector3(size, -size), c);
    }

    public static void Arrow(Vector3 from, Vector3 to, Color c)
    {
        if (!On) return;
        Line(from, to, c);
        Vector3 d = to - from;
        if (d.sqrMagnitude < 0.0001f) return;
        Vector3 back = -d.normalized * Mathf.Min(0.4f, d.magnitude * 0.3f);
        Vector3 side = new Vector3(-back.y, back.x, 0f) * 0.6f;
        Line(to, to + back + side, c);
        Line(to, to + back - side, c);
    }

    /// <summary>Text at a world point. row = 0,1,2… stacks lines downward.</summary>
    public static void Label(Vector3 worldPos, string text, Color c, int row = 0)
    {
        if (On) Labels.Add(new LabelCmd { pos = worldPos, text = text, c = c, row = row });
    }
}
