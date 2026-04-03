using Godot;

namespace SennenRpg.Core.Extensions;

/// <summary>
/// Static helper for screen-shake effects. Applies a decaying random jitter
/// to a node's position or a Camera2D's offset, then resets to the original value.
/// </summary>
public static class CameraShake
{
    /// <summary>
    /// Shakes a Camera2D by jittering its Offset.
    /// </summary>
    public static void Shake(Camera2D camera, float intensity = 3f, float duration = 0.15f)
    {
        if (camera == null) return;
        ShakeProperty(camera, "offset", Vector2.Zero, intensity, duration);
    }

    /// <summary>
    /// Shakes any CanvasItem by jittering its position around its current value.
    /// Use this for scenes without a Camera2D (e.g., BattleScene).
    /// </summary>
    public static void ShakeNode(Node2D node, float intensity = 3f, float duration = 0.15f)
    {
        if (node == null) return;
        ShakeProperty(node, "position", node.Position, intensity, duration);
    }

    private static void ShakeProperty(Node node, string property, Vector2 restValue,
        float intensity, float duration)
    {
        var tween = node.CreateTween();
        int steps = Mathf.Max(3, (int)(duration / 0.03f));
        float stepTime = duration / steps;

        for (int i = 0; i < steps; i++)
        {
            float decay = 1f - (float)i / steps;
            float mag = intensity * decay;
            var offset = restValue + new Vector2(
                (float)GD.RandRange(-mag, mag),
                (float)GD.RandRange(-mag, mag));
            tween.TweenProperty(node, property, offset, stepTime);
        }

        tween.TweenProperty(node, property, restValue, stepTime);
    }
}
