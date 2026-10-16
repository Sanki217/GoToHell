using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeholder AoE visuals (translucent spheres) until real VFX exist.
/// Materials are cached per colour — the old code built a new Material for
/// every explosion and never destroyed it (a slow leak over a long run).
/// </summary>
public static class DebugShapes
{
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    /// <summary>Spawns a translucent sphere that destroys itself after <paramref name="duration"/> seconds.</summary>
    public static void Sphere(Vector3 position, float radius, Color color, float duration)
    {
        if (duration <= 0f || radius <= 0f) return;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "[AoE Preview]";
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * radius * 2f;

        if (sphere.TryGetComponent(out Renderer r))
        {
            r.sharedMaterial = GetMaterial(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        Object.Destroy(sphere, duration);
    }

    /// <summary>Shared translucent material for a colour.</summary>
    public static Material GetMaterial(Color color)
    {
        if (materials.TryGetValue(color, out Material m) && m != null) return m;

        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        m = new Material(shader) { color = color, name = "AoE Preview " + color };
        materials[color] = m;
        return m;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => materials.Clear();
}
