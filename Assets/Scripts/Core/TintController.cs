using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the colour of a character's renderers. Every system that wants to
/// recolour something (hit flash, status tints, invincibility pulse, fuse
/// blink) goes through here instead of writing renderer.material.color.
///
/// WHY:
///   • renderer.material silently clones the material per object (leaks, and
///     breaks batching once real art lands).
///   • Several writers used to fight: two hits inside one flash window saved
///     the FLASH colour as "original" and left enemies permanently white;
///     an expiring Burn reset the colour to white while Freeze was active.
///
/// LAYERS (highest wins):
///   1. Flash  — short override (hit flash).
///   2. Pulse  — timed ping-pong between two colours (dash i-frames).
///   3. Tints  — persistent, keyed by owner; the most recently set wins
///               (status effects, fuse glow). Removing one reveals the next.
///   4. Base   — the renderer's own colour, captured once.
///
/// Uses MaterialPropertyBlocks for mesh renderers (_Color and _BaseColor, so
/// it works on Built-in and URP shaders) and SpriteRenderer.color for sprites —
/// ready for the pixel-art pass.
///
/// Usage: TintController.For(gameObject).Flash(Color.white, 0.08f);
/// The component is added on demand and only Updates while a flash/pulse runs.
/// </summary>
[DisallowMultipleComponent]
public class TintController : MonoBehaviour
{
    private static readonly int ColorId     = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Renderer[] renderers = System.Array.Empty<Renderer>();
    private Color[] baseColors = System.Array.Empty<Color>();
    private MaterialPropertyBlock block;
    private bool configured;

    // Persistent tints — ordered by recency (last = active)
    private readonly List<KeyValuePair<object, Color>> tints = new List<KeyValuePair<object, Color>>();

    private float flashEnd = -1f;
    private Color flashColor;

    private float pulseEnd = -1f;
    private Color pulseA, pulseB;
    private float pulseFrequency;

    // ================================================================
    //  ACCESS
    // ================================================================

    /// <summary>Gets (or adds) the controller on <paramref name="go"/>, auto-targeting its child renderers.</summary>
    public static TintController For(GameObject go)
    {
        if (go == null) return null;
        if (!go.TryGetComponent(out TintController tc))
            tc = go.AddComponent<TintController>();
        return tc;
    }

    /// <summary>
    /// Restrict the controller to specific renderers (e.g. the body mesh only,
    /// not weapon trails). Call before the first colour change.
    /// </summary>
    public void SetTargets(params Renderer[] targets)
    {
        var list = new List<Renderer>();
        if (targets != null)
            foreach (var r in targets)
                if (r != null) list.Add(r);
        renderers = list.ToArray();
        CaptureBaseColors();
        configured = true;
        Apply();
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        enabled = false;   // Update only runs while a flash/pulse is active
    }

    private void EnsureConfigured()
    {
        if (configured) return;
        configured = true;

        var list = new List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            // Only body renderers — never particles, lines or trails
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            list.Add(r);
        }
        renderers = list.ToArray();
        CaptureBaseColors();
    }

    private void CaptureBaseColors()
    {
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r is SpriteRenderer sr) { baseColors[i] = sr.color; continue; }

            Material m = r.sharedMaterial;
            if (m != null && m.HasProperty(BaseColorId)) baseColors[i] = m.GetColor(BaseColorId);
            else if (m != null && m.HasProperty(ColorId)) baseColors[i] = m.GetColor(ColorId);
            else baseColors[i] = Color.white;
        }
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>The untinted colour of the first target renderer.</summary>
    public Color BaseColor
    {
        get
        {
            EnsureConfigured();
            return baseColors.Length > 0 ? baseColors[0] : Color.white;
        }
    }

    /// <summary>Brief full override (hit flash). Overlapping flashes extend, never corrupt the base colour.</summary>
    public void Flash(Color color, float duration)
    {
        EnsureConfigured();
        flashColor = color;
        flashEnd = Mathf.Max(flashEnd, Time.time + Mathf.Max(0f, duration));
        enabled = true;
        Apply();
    }

    /// <summary>Ping-pong between two colours for <paramref name="duration"/> seconds.</summary>
    public void Pulse(Color a, Color b, float frequency, float duration)
    {
        EnsureConfigured();
        pulseA = a;
        pulseB = b;
        pulseFrequency = frequency;
        pulseEnd = Time.time + Mathf.Max(0f, duration);
        enabled = true;
        Apply();
    }

    public void StopPulse()
    {
        pulseEnd = -1f;
        Apply();
    }

    /// <summary>Persistent tint owned by <paramref name="owner"/> (a status effect, a fuse…).</summary>
    public void SetTint(object owner, Color color)
    {
        if (owner == null) return;
        EnsureConfigured();
        RemoveTintInternal(owner);
        tints.Add(new KeyValuePair<object, Color>(owner, color));
        Apply();
    }

    public void ClearTint(object owner)
    {
        if (owner == null) return;
        if (RemoveTintInternal(owner)) Apply();
    }

    // ================================================================
    //  INTERNAL
    // ================================================================

    private bool RemoveTintInternal(object owner)
    {
        for (int i = tints.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(tints[i].Key, owner)) continue;
            tints.RemoveAt(i);
            return true;
        }
        return false;
    }

    private void Update()
    {
        bool flashing = Time.time < flashEnd;
        bool pulsing = Time.time < pulseEnd;
        Apply();
        if (!flashing && !pulsing) enabled = false;
    }

    private void Apply()
    {
        EnsureConfigured();
        if (renderers.Length == 0) return;
        if (block == null) block = new MaterialPropertyBlock();

        float now = Time.time;
        bool hasOverride = true;
        Color overrideColor = default;

        if (now < flashEnd) overrideColor = flashColor;
        else if (now < pulseEnd)
            overrideColor = Color.Lerp(pulseA, pulseB, Mathf.PingPong(now * pulseFrequency, 1f));
        else if (tints.Count > 0) overrideColor = tints[tints.Count - 1].Value;
        else hasOverride = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            if (r is SpriteRenderer sr)
            {
                sr.color = hasOverride ? overrideColor : baseColors[i];
                continue;
            }

            if (!hasOverride)
            {
                // Back to the material's own colours (all sub-materials intact)
                r.SetPropertyBlock(null);
                continue;
            }

            r.GetPropertyBlock(block);
            block.SetColor(ColorId, overrideColor);
            block.SetColor(BaseColorId, overrideColor);
            r.SetPropertyBlock(block);
        }
    }
}
