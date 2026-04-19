using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-logic helpers for the Mode 7 pseudo-3D world map view.
/// No Godot dependencies — fully NUnit-testable.
/// The shader-mirror methods replicate the GPU math so tests can
/// verify the perspective behaviour without a running Godot instance.
/// </summary>
public static class Mode7Logic
{
    // ── Defaults matching the shader uniforms ────────────────────────────
    public const float DefaultPitch = 0.35f;
    public const float CameraOffsetY = 60f;

    // ── World → shader offset ────────────────────────────────────────────

    /// <summary>
    /// Convert player world position to a camera offset for the shader (-0.5 to 0.5 range).
    /// </summary>
    public static (float x, float y) WorldToShaderOffset(
        float playerX, float playerY, float mapWidth, float mapHeight)
    {
        float u = mapWidth > 0 ? (playerX / mapWidth - 0.5f) : 0f;
        float v = mapHeight > 0 ? (playerY / mapHeight - 0.5f) : 0f;
        return (Clamp(u, -0.5f, 0.5f), Clamp(v, -0.5f, 0.5f));
    }

    /// <summary>
    /// Clamp Mode 7 shader parameters to their valid uniform ranges.
    /// </summary>
    public static (float horizon, float height, float fov) ClampParams(
        float horizon, float height, float fov)
        => (Clamp(horizon, 0.1f, 0.6f), Clamp(height, 0.5f, 3.0f), Clamp(fov, 0.5f, 2.0f));

    // ── Shader-mirror functions ──────────────────────────────────────────
    // These replicate the GPU fragment logic so NUnit tests can verify the
    // perspective curve, horizontal scale, and fog without Godot.

    /// <summary>
    /// Mirror of the shader's perspective warp.
    /// <paramref name="screenY"/> is 1.0 at the top of screen (close) and 0.0 at bottom (far).
    /// Returns the source texture Y coordinate (0 = top, 1 = bottom).
    /// </summary>
    public static float PerspectiveWarpY(float screenY, float pitch, float t)
    {
        float effectivePitch = Lerp(1f, pitch, t);
        float warped = MathF.Pow(Math.Clamp(screenY, 0f, 1f), effectivePitch);
        return 1f - warped;
    }

    /// <summary>
    /// Mirror of the shader's horizontal scale factor.
    /// <paramref name="screenY"/> is 1.0 at top (close), 0.0 at bottom (far).
    /// Returns the h_scale multiplier (≥ 1.0).
    /// </summary>
    public static float HorizontalScale(float screenY, float t)
    {
        float dist = 1f - Math.Clamp(screenY, 0f, 1f);
        return 1f + dist * dist * 2f * t;
    }

    /// <summary>
    /// Mirror of the shader's source X computation.
    /// <paramref name="uvX"/> is the original screen X (0–1).
    /// </summary>
    public static float SourceX(float uvX, float screenY, float t)
    {
        float hScale = HorizontalScale(screenY, t);
        return 0.5f + (uvX - 0.5f) / hScale;
    }

    /// <summary>
    /// Mirror of the shader's distance fog factor.
    /// Returns 0.0 at the very bottom (full fog), 1.0 when far from horizon (no fog).
    /// </summary>
    public static float FogFactor(float screenY)
    {
        return Smoothstep(0f, 0.25f, Math.Clamp(screenY, 0f, 1f));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static float Clamp(float v, float min, float max)
        => v < min ? min : v > max ? max : v;

    private static float Lerp(float a, float b, float t)
        => a + (b - a) * t;

    private static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
