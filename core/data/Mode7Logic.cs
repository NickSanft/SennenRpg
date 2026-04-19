namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-logic helpers for the Mode 7 pseudo-3D world map view.
/// No Godot dependencies — fully NUnit-testable.
/// </summary>
public static class Mode7Logic
{
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

    private static float Clamp(float v, float min, float max)
        => v < min ? min : v > max ? max : v;
}
